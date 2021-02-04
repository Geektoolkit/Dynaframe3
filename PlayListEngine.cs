using System;
using System.Collections.Generic;
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
        List<PlayListItem> playListItems;

        public PlayListEngine()
        {
            CurrentPlayListItems = new List<PlayListItem>();
            playListItems = new List<PlayListItem>();
        }
        public List<PlayListItem> GetPlayListItems(List<string> Folders, SearchOption searchOptions)
        {
            playListItems.Clear();

            // Filter folders here. The IgnoreFolders appsetting allows folders to be entered to be ignored/added

            string[] folders = new string[] { ""};
            if (AppSettings.Default.IgnoreFolders.Length > 0)
            {
                folders = AppSettings.Default.IgnoreFolders.Split(",");
            }

            foreach (string folderException in folders)
            {
                Folders = Folders.Except(Folders.Where(f => new DirectoryInfo(f).Name.StartsWith(folderException))).ToList();
            }


            foreach (string Folder in Folders)
            {
                if (!Directory.Exists(Folder))
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

            if (CurrentPlayListItems.Count > 0)
            {
                CurrentPlayListItem = CurrentPlayListItems.First();
            }

            return playListItems;
        }

        /// <summary>
        /// Overload to simplify the API, uses the default 'current playlist' from settings
        /// </summary>
        /// <returns></returns>
        public List<PlayListItem> GetPlayListItems()
        {
            // Logic notes:
            // 1) SearchDirectories tracks the top level directories
            // 2) Appsettings.default.currentplaylist tracks subdirectories under top level
            CurrentPlayListItems.Clear();
            List<PlayListItem> items = GetPlayListItems(AppSettings.Default.CurrentPlayList, SearchOption.AllDirectories)
                .Concat(GetPlayListItems(AppSettings.Default.SearchDirectories, SearchOption.TopDirectoryOnly)).ToList();

            if (AppSettings.Default.Shuffle)
            {
                Random r = new Random((int)DateTime.Now.Ticks);
                CurrentPlayListItems = Helpers.Shuffle<PlayListItem>(CurrentPlayListItems, r).ToList();
            }
            else
            {
                // Sort the list here:
                CurrentPlayListItems.Sort((y, z) => new FileInfo(y.Path).Name.CompareTo(new FileInfo(z.Path).Name));

                // Sort by date
                // CurrentPlayListItems.Sort((y, z) => new FileInfo(y.Path).CreationTime.CompareTo(new FileInfo(z.Path).CreationTime));


            }

            Logger.LogComment("New List generated! Contains: " + CurrentPlayListItems.Count + " items. Shuffle setting is: " + AppSettings.Default.Shuffle);
            // extra logging for now 
            Logger.LogComment("----------------------Begin Playlist Dump----------------");
            try
            {
                for (int i = 0; i < CurrentPlayListItems.Count; i++)
                {
                    Logger.LogComment(CurrentPlayListItems[i].ItemType + ":" + CurrentPlayListItems[i].Path);
                }
            }
            catch (Exception)
            {
                // Collection was modified, basically timing issue. ignore.
            }

            Logger.LogComment("------------------- END PLAYLIST DUMP-------------------");

            return items;

        }

        /// <summary>
        /// Goes to Next Playlist Item. Will rebuild Playlist if nothing is found.
        /// </summary>
        public void GoToNext()
        {
            int index = CurrentPlayListItems.IndexOf(CurrentPlayListItem);
            PlayListItem instructions = new PlayListItem() { ItemType = PlayListItemType.Image, Path = Environment.CurrentDirectory + "/images/Instructions.png" };


            if (index == -1)
            {
                Logger.LogComment("ERROR: GoToNext was unable to find current item in playlist!");
                GetPlayListItems();  // TODO: Investigate if this is a terrible idea...
            }
            // If at the end of the list...return the first one
            if (index == CurrentPlayListItems.Count - 1)
            {
                Logger.LogComment("Reached end of the list! (or no items found)");
                GetPlayListItems(); // refresh the list. This will help with shuffled lists possibly. May make this a setting.
                if (playListItems.Count == 0)
                {
                    CurrentPlayListItems.Add(instructions);
                }
                CurrentPlayListItem = CurrentPlayListItems.First();
            }
            else
            {
                if (CurrentPlayListItems.Contains(instructions))
                {
                    CurrentPlayListItems.RemoveAll(f=> f.Path == instructions.Path);
                }
                CurrentPlayListItem = CurrentPlayListItems[index + 1];
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
            testpath = CurrentPlayListItems.Where(p => p.Path.Contains(new FileInfo(path).Directory.Name.ToUpper()) && new FileInfo(p.Path).Name.ToUpper() == new FileInfo(path).Name.ToUpper()).FirstOrDefault();
            if (testpath != null)
            {
                Logger.LogComment("SYNC: (case 2) Folder/Filename found! Returning: " + testpath);
                return testpath.Path;
            }

            // case 3: Match any file in the playlist against mandalorian.jpg
            testpath = CurrentPlayListItems.Where(p => new FileInfo(p.Path).Name.ToUpper() == new FileInfo(path).Name.ToUpper()).FirstOrDefault();
            if (testpath != null)
            {
                Logger.LogComment("SYNC: (case 3) Filename only found! Returning: " + testpath);
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
