using MetadataExtractor;
using MetadataExtractor.Formats.Exif;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace Dynaframe3
{
    class PlayListEngine
    {
        /// <summary>
        /// Future use: This is the 'Name' of the playlist, which will allow for it to be saved off in the future.
        /// </summary>
        public string CurrentPlayListName { get; set; }
        /// <summary>
        /// This is the current item in the playlist which is being shown
        /// </summary>
        public PlayListItem CurrentPlayListItem { get; set; }

        /// <summary>
        /// This is the current playlist which is being shown/used
        /// </summary>
        public List<PlayListItem> CurrentPlayListItems { get; set; }
        //List<PlayListItem> playListItems;

        public PlayListEngine()
        {
            CurrentPlayListItems = new List<PlayListItem>();
        }
        public void GetPlayListItems(List<string> Folders, SearchOption searchOptions)
        {
            // Temp list to hold items from this call that are retrieved from folders...
            List<PlayListItem> playListItems = new List<PlayListItem>();

            // Filter folders here. The IgnoreFolders appsetting allows folders to be entered to be ignored/added

            string[] folders = new string[] { ""};
            if (AppSettings.Default.IgnoreFolders.Length > 0)
            {
                folders = AppSettings.Default.IgnoreFolders.Split(",");
            }

            // First remove ignore folders from Folders array which is passed in
            foreach (string folderException in folders)
            {
                Folders = Folders.Except(Folders.Where(f => new DirectoryInfo(f).Name.StartsWith(folderException))).ToList();
            }

            // Now go through each folder and fill playlist items with items from them
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
                   
                    files = files.Except(files.Where(f => new FileInfo(f).Name.StartsWith("._")));
                    foreach (string file in files)
                    {
                        PlayListItem item = new PlayListItem();
                        item.Path = file;
                        item.ItemType = PlayListEngineHelper.GetPlayListItemTypeFromPath(file);

                        playListItems.Add(item);
                    }
                }
                catch (Exception exc)
                {
                    Logger.LogComment("WARNING: Unable to load file or files from Folder " + Folder + " Exception: " + exc.ToString());
                }

            }

            CurrentPlayListItems.AddRange(playListItems);

        }

        /// <summary>
        /// Overload to simplify the API, uses the default 'current playlist' from settings
        /// </summary>
        /// <returns></returns>
        public void GetPlayListItems()
        {
            // Logic notes:
            // 1) SearchDirectories tracks the top level directories
            // 2) Appsettings.default.currentplaylist tracks subdirectories under top level
            // This calls GetPlaylistItems(overloads) which
            //  - Creates a temp list of items
            //  - goes through a passed in folder list
            //  - adds files from each folder to the temp playlist
            //  - adds the temp playlist to the main playlist called "CurrentplaylistItems"

            Logger.LogComment("GetPlayListItems: Clearing item list..");
            CurrentPlayListItems.Clear();

            Logger.LogComment("GetPlayListItems: Getting playlist Items...");
            GetPlayListItems(AppSettings.Default.CurrentPlayList, SearchOption.AllDirectories);
            GetPlayListItems(AppSettings.Default.SearchDirectories, SearchOption.TopDirectoryOnly);

            if (AppSettings.Default.Shuffle)
            {
                Random r = new Random((int)DateTime.Now.Ticks);
                Logger.LogComment("GetPlayListItems: Shuffle is on...shuffling items....");
                CurrentPlayListItems = Helpers.Shuffle<PlayListItem>(CurrentPlayListItems, r).ToList();
            }
            else
            {
                Logger.LogComment("GetPlayListItems: Shuffle is off. Sorting items...");
                // Sort the list here:
                CurrentPlayListItems.Sort((y, z) => new FileInfo(y.Path).Name.CompareTo(new FileInfo(z.Path).Name));

                // Sort by date
                // CurrentPlayListItems.Sort((y, z) => new FileInfo(y.Path).CreationTime.CompareTo(new FileInfo(z.Path).CreationTime));


            }

            // extra logging for now 
            Logger.LogComment("----------------------Begin Playlist Dump----------------");
            Stopwatch sw = new Stopwatch();
            sw.Start();
            try
            {
                for (int i = 0; i < CurrentPlayListItems.Count; i++)
                {
                    Logger.LogComment(CurrentPlayListItems[i].ItemType + ":" + CurrentPlayListItems[i].Path);
                    if (CurrentPlayListItem.ItemType == PlayListItemType.Image)
                    {
                        IEnumerable<MetadataExtractor.Directory> directories = ImageMetadataReader.ReadMetadata(CurrentPlayListItem.Path);
                        var subIfdDirectory = directories.OfType<ExifSubIfdDirectory>().FirstOrDefault();
                        var dateTime = subIfdDirectory?.GetDescription(ExifDirectoryBase.TagDateTimeOriginal);
                        if (dateTime != null)
                        {
                            Logger.LogComment("Datetime: Datetime: " + dateTime);
                        }

                    }
                }
            }
            catch (Exception)
            {
                // Collection was modified, basically timing issue. ignore.
                Logger.LogComment("GetPlayListItems: timing issue caught...list modified while enumerating through it...");
            }
            sw.Stop();
           
            Logger.LogComment("------------------- END PLAYLIST DUMP  Took: " + sw.ElapsedMilliseconds + " ms.  -----------------");
            
        }

        /// <summary>
        /// Goes to Next Playlist Item. Will rebuild Playlist if nothing is found.
        /// </summary>
        public void GoToNext()
        {
            // Note: This method has given me alot of grief, so there is extensive logging
            // to assist in debugging.  If we ever 'hang' hopefully this method can tell us why since
            // it is so critical to the frame moving forward to the next image.

            Logger.LogComment("GoToNext: called");

            int index = CurrentPlayListItems.IndexOf(CurrentPlayListItem);

            PlayListItem instructions = new PlayListItem() { ItemType = PlayListItemType.Image, Path = Environment.CurrentDirectory + "/images/Instructions.png" };


            if (index == -1)
            {
                Logger.LogComment("ERROR: GoToNext was unable to find current item in playlist!");
                GetPlayListItems();  // TODO: Investigate if this is a terrible idea...
                index = 0;
            }
            
            Logger.LogComment("GoToNext: Current Index Value is: " + index);
            Logger.LogComment("CurrentPlayListItem Count is: " + CurrentPlayListItems.Count);

            if (CurrentPlayListItems.Count == 0)
            {
                Logger.LogComment("GoToNext: No images found...so going to show instructions...");
                CurrentPlayListItems.Add(instructions);
                CurrentPlayListItem = CurrentPlayListItems.First();
                index = 0;
            }
            else if (index == CurrentPlayListItems.Count - 1) 
            {
                // If at the end of the list...return the first one
                Logger.LogComment("Reached end of the list! (or no items found)");
                GetPlayListItems(); // refresh the list. This will help with shuffled lists possibly. May make this a setting.
                CurrentPlayListItem = CurrentPlayListItems.First();
                Logger.LogComment("GoToNext: CurrentPlayListItem is: " + CurrentPlayListItem.Path);
            }
            else
            {
                if (CurrentPlayListItems.Contains(instructions))
                {
                    CurrentPlayListItems.RemoveAll(f=> f.Path == instructions.Path);
                }
                index++;
                Logger.LogComment("GoToNext: Valid Playlist seems to be detected...Loading next item at index: " + index);

                CurrentPlayListItem = CurrentPlayListItems[index];
                Logger.LogComment("GoToNext: Next Playlist Item will be: " + CurrentPlayListItem.Path);
            }
        }

        /// <summary>
        /// Goes to the previous point in the list
        /// </summary>
        public void GoToPrevious()
        {
            int index = CurrentPlayListItems.IndexOf(CurrentPlayListItem);
            if (index == -1)
            {
                Logger.LogComment("ERROR: GoToPrevious was unable to find current item in playlist!");
                GetPlayListItems();  // TODO: Investigate if this is a terrible idea...
            }
            // If at the end of the list...return the first one
            if (index == 0)
            {
                Logger.LogComment("Reached end of the list!");
                CurrentPlayListItem = CurrentPlayListItems.Last();
            }
            else
            {
                CurrentPlayListItem = CurrentPlayListItems[index - 1];
            }
        }


        /// <summary>
        /// This is for playlist syncing. This gets called when a playlist is handed in on the
        /// command via setfile. It' checks to find a file that is as close as possible on this unit.
        /// </summary>
        /// <param name="path">Remote machine path which was passed in</param>
        /// <returns>a full path to a file</returns>
        public string ConvertFileNameToLocal(string path)
        {
            Logger.LogComment("ConvertFileNameToLocal (For frame syncing) Entering. Path passed in was: " + path);
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
        }

    }
}
