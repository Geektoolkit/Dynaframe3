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

            if (AppSettings.Default.Shuffle)
            {
                Random r = new Random((int)DateTime.Now.Ticks);
                playListItems = Helpers.Shuffle<PlayListItem> (playListItems, r).ToList();
            }

            CurrentPlayListItems.AddRange(playListItems);
            Logger.LogComment("New List generated! Contains: " + CurrentPlayListItems.Count + " items.");
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
            // Return it in case we want to use this for the UI for future work.

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

            return GetPlayListItems(AppSettings.Default.CurrentPlayList, SearchOption.AllDirectories)
                .Concat(GetPlayListItems(AppSettings.Default.SearchDirectories, SearchOption.TopDirectoryOnly)).ToList();
        }

        /// <summary>
        /// Goes to Next Playlist Item. Will rebuild Playlist if nothing is found.
        /// </summary>
        public void GoToNext()
        {
            int index = CurrentPlayListItems.IndexOf(CurrentPlayListItem);
            if (index == -1)
            {
                Logger.LogComment("ERROR: GoToNext was unable to find current item in playlist!");
                GetPlayListItems();  // TODO: Investigate if this is a terrible idea...
            }
            // If at the end of the list...return the first one
            if (index == CurrentPlayListItems.Count - 1)
            {
                Logger.LogComment("Reached end of the list!");
                GetPlayListItems(); // refresh the list. This will help with shuffled lists possibly. May make this a setting.
                CurrentPlayListItem = CurrentPlayListItems.First();
            }
            else
            {
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

    }
}
