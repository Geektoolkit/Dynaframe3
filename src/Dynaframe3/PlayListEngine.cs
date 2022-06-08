using MetadataExtractor;
using MetadataExtractor.Formats.Exif;
using MetadataExtractor.Formats.Iptc;
using MetadataExtractor.Formats.Xmp;
using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Dynaframe3.Models;
using Dynaframe3.Shared;

namespace Dynaframe3
{
    class PlayListEngine
    {
        public MediaFile CurrentMediaFile { get; set; }
        public int MediaIndex = 0;
        public List<int> Playlist;

        public PlayListEngine()
        {
            Playlist = new List<int>();
        }

        public void GoToFirstFile()
        {
            MediaIndex = 0;
            GetCurrentFile();
        }
        public void RebuildPlaylist(AppSettings appsettings)
        {
            // TODO:
            // Add Filters
            // Add Sorting

            Stopwatch sw = new Stopwatch();
            sw.Start();
            Logger.LogComment("Rebuilding playlist called...rebuilding the playlist...");
            Playlist.Clear();

            // filter based on file paths
            foreach (string dir in appsettings.CurrentPlayList)
            {
                using (var db = new MediaDataContext())
                {
                    var files = db.MediaFiles.Where(d => d.Path.StartsWith(dir));
                    foreach (var file in files)
                    {
                        Playlist.Add(file.Id);
                    }
                }
            }

            // Add in toplevel directories 
            // TODO: This can be filtered out here.
            foreach (string dir in appsettings.SearchDirectories)
            {
                using (var db = new MediaDataContext())
                {
                    var files = db.MediaFiles.Where(d => d.Directory == dir);
                    foreach (var file in files)
                    {
                        Playlist.Add(file.Id);
                    }
                }
            }

            // Filter based on tags
            if (!String.IsNullOrEmpty(appsettings.InclusiveTagFilters))
            {
                string[] filters = appsettings.InclusiveTagFilters.Split(';');
                foreach (string filter in filters)
                {
                    using (var db = new MediaDataContext())
                    {
                        var files = db.MediaFiles.Where(d => d.Tags != null).Where(f => f.Tags.Contains(filter));
                        foreach (var file in files)
                        {
                            Playlist.Add(file.Id);
                        }
                    }
                }
            }

            Playlist = Playlist.Distinct().ToList();

            if (appsettings.Shuffle)
            {
                Random r = new Random((int)DateTime.Now.Ticks);
                Playlist = Helpers.Shuffle<int>(Playlist, r).ToList();
            }
            sw.Stop();
            Logger.LogComment("Rebuilding playlist took: " + sw.ElapsedMilliseconds + "ms.");
            Logger.LogComment("Playlist has: " + Playlist.Count + " items now.");

        } 
        public MediaFile GetCurrentFile()
        {
            using (var db = new MediaDataContext())
            {
                try
                {
                    if (MediaIndex >= Playlist.Count())
                    {
                        MediaIndex = 0;
                    }

                    Logger.LogComment("Skipping to item: " + Playlist[MediaIndex]);
                    MediaFile file = db.MediaFiles.Where(f => f.Id == Playlist[MediaIndex]).Single();
                    Logger.LogComment("Returning item# : " + MediaIndex + " File: " + file.Path);
                    CurrentMediaFile = file;
                    return file;
                }
                catch (Exception)
                {
                    MediaIndex++;
                    return GetCurrentFile();
                }
                
            }
        }

        public void DumpPlaylistToLog()
        {
            Logger.LogComment("--------------------- START DUMP --------------------------");
            Logger.LogComment("Dumping log of: " + Playlist.Count + " items.");
            using (var db = new MediaDataContext())
            {
                for (int i = 0; 0 < Playlist.Count; i++)
                {
                    try
                    {
                        MediaFile file = db.MediaFiles.Where(mf => mf.Id == Playlist[i]).Single();
                        Logger.LogComment(file.Id + " - File: " + file.Path);
                    }
                    catch (Exception)
                    {
                        // If the playlist is modified this will throw. Ignore for now.
                    }

                }
            }
            Logger.LogComment("Size: " + sizeof(int) * Playlist.Count);
            Logger.LogComment("--------------------- END DUMP --------------------------");
        }

        public void InitializeDatabase(AppSettings appsettings)
        {
            Logger.LogComment("InitializeDatabase() - Building Database");
            AddItemsToDatabase(appsettings, appsettings.CurrentPlayList, SearchOption.AllDirectories);
            AddItemsToDatabase(appsettings, appsettings.SearchDirectories, SearchOption.TopDirectoryOnly);
        }

        public void AddItemsToDatabase(AppSettings appsettings, List<string> Folders, SearchOption searchOptions)
        {
            // Filter folders and issues out now..
            string[] folders = new string[] { "" };
            if (appsettings.IgnoreFolders.Length > 0)
            {
                folders = appsettings.IgnoreFolders.Split(",");
            }

            // First remove ignore folders from Folders array which is passed in
            foreach (string folderException in folders)
            {
                Folders = Folders.Except(Folders.Where(f => new DirectoryInfo(f).Name.StartsWith(folderException))).ToList();
            }

            // Go through remaining folders and get files

            foreach (string Folder in Folders)
            {
                if (!System.IO.Directory.Exists(Folder))
                {
                    Logger.LogComment("ERROR: Selected Playlist Folder Missing: " + Folder);
                    continue;
                }

                try
                {
                    IEnumerable<string> files = PlayListEngineHelper.GetFilesByExtensions(new DirectoryInfo(Folder), searchOptions, PlayListEngineHelper.GetSupportedExtensions());

                    // Filter here
                    // Filter 1: MAC Computers create files that end in .jpg that start with "._"
                    files = files.Except(files.Where(f => new FileInfo(f).Name.StartsWith("._")));
                    using (var db = new MediaDataContext())
                    {
                        foreach (string file in files)
                        {
                            // For each file, see if the path exists in the database
                            bool FileExists = true;
                            MediaFile mediaFile = null;
                            var mFile = db.MediaFiles
                                .Where(f => f.Path == file).ToList();

                            if ((mFile != null) && (mFile.Count() > 0))
                            {
                                mediaFile = mFile.FirstOrDefault();
                            }

                            // If not create a new blank one and enter the path
                            if (mediaFile == null)
                            {
                                mediaFile = new MediaFile();
                                mediaFile.Path = file;
                                mediaFile.Directory = new FileInfo(file).Directory.FullName;
                                FileExists = false; // The file didn't exist, so note that here
                            }

                            // Now we can fill out/update the rest
                            mediaFile.Type = PlayListEngineHelper.GetPlayListItemTypeFromPath(file).ToString();
                            if (mediaFile.Type == "Image")
                            {
                                GetMetaData(ref mediaFile);
                            }

                            // If it exists, do an update, else add new.
                            if (FileExists)
                            {
                                db.MediaFiles.Update(mediaFile);
                            }
                            else
                            {
                                db.MediaFiles.Add(mediaFile);
                            }
                            
                            db.SaveChanges();
                        }
                        Logger.LogComment("Count of files in the database: " + db.MediaFiles.Count());
                    }
                }
                catch (Exception exc)
                {
                    Logger.LogComment("WARNING: Unable to load file or files from Folder " + Folder + " Exception: " + exc.ToString());
                }

            }
        }

        /// <summary>
        /// Gets the exif data and fills out the object so that we can get that data into the database. Very time intensive operation.
        /// </summary>
        /// <param name="mediaFile"></param>
        public void GetMetaData(ref MediaFile mediaFile)
        {
            try
            {
                {
                    IEnumerable<MetadataExtractor.Directory> directories = ImageMetadataReader.ReadMetadata(mediaFile.Path);
                    MetadataExtractor.Directory dir = directories.Where(d => d.Name == "Exif IFD0").FirstOrDefault();
                    if (dir != null)
                    {
                        mediaFile.Title = GetTagByName("Windows XP Title", dir);
                        mediaFile.Author = GetTagByName("Windows XP Author", dir);
                        mediaFile.Comment = GetTagByName("Windows XP Comment", dir);
                        mediaFile.Tags = GetTagByName("Windows XP Keywords", dir);
                    }
                }
            }
            catch (Exception exc)
            {
                Logger.LogComment("Exception looking up tag data. File: " + mediaFile.Path + " Exception: " + exc.Message);
            }
        }

        public string GetTagByName(string tagName, MetadataExtractor.Directory dir)
        {
           Tag val =  dir.Tags.Where(t => t.Name == tagName).FirstOrDefault();
            if (val != null)
            {
                return val.Description;
            }
            return "";
        }
        /// <summary>
        /// Goes to Next Playlist Item. Will rebuild Playlist if nothing is found.
        /// </summary>
        public void GoToNext()
        {
            MediaIndex++;
            GetCurrentFile();
        }

        /// <summary>
        /// Goes to the previous point in the list
        /// </summary>
        public void GoToPrevious()
        {
            MediaIndex--;
            GetCurrentFile();
        }


        /// <summary>
        /// This is for playlist syncing. This gets called when a playlist is handed in on the
        /// command via setfile. It' checks to find a file that is as close as possible on this unit.
        /// </summary>
        /// <param name="path">Remote machine path which was passed in</param>
        /// <returns>a full path to a file</returns>
        public string ConvertFileNameToLocal(string path)
        {
            // TODO: Disabled for now. Need to rethink syncing.

            /* Logger.LogComment("ConvertFileNameToLocal (For frame syncing) Entering. Path passed in was: " + path);
             // Note: Path is from a remote machine
             // Formula is:
             // 1) If an exact match is found..return that.
             // 2) If the root folder and file name is found, return that (probably common case)
             // 3) If the file name is found...return that
             // 4) If folder is found, return random file from that folder
             // 5) If nothing is found, return a random file (Not sure what to do here)
             // Sample for below uses:
             //          passed in: c:\geektoolkit\pictures\scifi\mandalorian.jpg
             //          

             // Test 1: Full path found on this machine: c:\geektoolkit\pictures\scifi\mandalorian.jpg
             var testpath = CurrentPlayListItems.Where(p => p.Path.ToUpper() == path.ToUpper()).FirstOrDefault();
             if (testpath != null)
             {
                 Logger.LogComment("SYNC: (case 1) Full path found! Returning: " + testpath);
                 return testpath.Path;
             }

             // case 2: Match from just the directory name, and a matching image was found: scifi\mandalorian.jpg
             // Note: this is 'golden path' and how I expect this to be used.
             testpath = CurrentPlayListItems.Where(p => p.Path.ToUpper().Contains(new FileInfo(path).Directory.Name.ToUpper()) && new FileInfo(p.Path).Name.ToUpper() == new FileInfo(path).Name.ToUpper()).FirstOrDefault();
             if (testpath != null)
             {
                 Logger.LogComment("SYNC: (case 2) Folder/Filename found! Returning: " + testpath.Path);
                 return testpath.Path;
             }

             // case 3: Match any file in the playlist against mandalorian.jpg
             testpath = CurrentPlayListItems.Where(p => new FileInfo(p.Path).Name.ToUpper() == new FileInfo(path).Name.ToUpper()).FirstOrDefault();
             if (testpath != null)
             {
                 Logger.LogComment("SYNC: (case 3) Filename only found! Returning: " + testpath.Path);
                 return testpath.Path;
             }

             // case 4: Match a folder with the same name as the subfolder/playlist:  scifi
             testpath = CurrentPlayListItems.Where(p => new FileInfo(p.Path).Directory.Name.ToUpper() == new FileInfo(path).Directory.Name.ToUpper()).FirstOrDefault();
             if (testpath != null)
             {
                 // note: in this case, the testpath is likely just a foldername.  We have to get a file out of it.
                 Logger.LogComment("SYNC: (case 4) - Found folder!");
                 string file = PlayListEngineHelper.GetRandomFileFromFolder(testpath.Path);
                 Logger.LogComment("SYNC: (case 4) Folder only found! Returning: " + file);
                 return file;
             }

             // case 5: just return something to show
             Logger.LogComment("SYNC: (case 5) - NO matches found for path, DirectoryName, Filename, continuing on with current playlist..returning" + CurrentPlayListItem.Path);
             return CurrentPlayListItem.Path;
            */
            return "";
        }

    }
}
