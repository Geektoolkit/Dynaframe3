using Avalonia;
using Avalonia.Media;
using Microsoft.Extensions.Configuration;
using Microsoft.VisualBasic.FileIO;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;

namespace Dynaframe3
{
    /// <summary>
    /// Source: https://stackoverflow.com/questions/51351464/user-configuration-settings-in-net-core
    /// </summary>
    public class AppSettings
    {
        // marked as private to prevent outside classes from creating new.
        private AppSettings()
        {
            ReloadSettings = true;
            RefreshDirctories = true;

            Shuffle = false;
            VideoVolume = false;

            Rotation = 0;
            NumberOfSecondsToShowIP = 10;

            OXMOrientnation = "--orientation 0";
            SearchDirectories = new List<string>() { };
            CurrentPlayList = new List<string>() { };
            RemoteClients = new List<string>() { };

            CurrentDirectory = SpecialDirectories.MyPictures;
            DateTimeFormat = "H:mm tt";
            DateTimeFontFamily = "Terminal";

            InfoBarFontSize = 50;
            SlideshowTransitionTime = 30000;      // milliseconds between slides
            FadeTransitionTime = 1600;            // milliseconds for fades
            ImageStretch = Stretch.UniformToFill; // Default image stretch
            VideoStretch = "Fill";                // Used for OMXPlayer
            ExpandDirectoriesByDefault = false;   // WebUI setting to expand the trees
            ListenerPort = 8000;                  // Default port for the WebUI to listen on
            IsSyncEnabled = false;                // Sync with other frames off by default

            IgnoreFolders = ".lrlibrary,.photoslibrary"; // TODO: Expose this in the Advanced UI

            ScreenStatus = true;                  // Default for Screen On / Off function
            ShowInfoDateTime = false;             // Show Date Time in Infobar On / Off function
            ShowInfoFileName = false;             // Show Filename in Infobar On / Off function
            ShowInfoIP = "false";                 // Show IP in Infobar On / Off function
            HideInfoBar = false;                  // Hide Infobar On / Off function
            DynaframeIP = Helpers.GetIP();        // Dynaframe IP Address
            SlideShowPaused = false;              // Pause Slideshow on / off
            EnableLogging = true;                 // Enables logging...should be set to false by default at some point.
        }

        private static string _jsonSource;
        private static AppSettings _appSettings = null;
        public static AppSettings Default
        {
            get
            {
                if (_appSettings == null)
                {
                    var builder = new ConfigurationBuilder()
                        .SetBasePath(Directory.GetCurrentDirectory())
                        .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

                    _jsonSource = $"{AppDomain.CurrentDomain.BaseDirectory}{Path.DirectorySeparatorChar}appsettings.json";

                    var config = builder.Build();
                    _appSettings = new AppSettings();
                    config.Bind(_appSettings);
                    if (_appSettings.SearchDirectories.Count == 0)
                    {
                        // Firstrun...if there are no search directories, add in mypictures and subfolders
                        _appSettings.SearchDirectories.Add(SpecialDirectories.MyPictures);
                        _appSettings.CurrentPlayList.Clear();
                        foreach (string dir in Directory.GetDirectories(SpecialDirectories.MyPictures))
                        {
                            _appSettings.CurrentPlayList.Add(dir);
                        }
                        string dirPath = AppDomain.CurrentDomain.BaseDirectory + "web/uploads/";
                        _appSettings.SearchDirectories.Add(dirPath);
                        foreach (string dir in Directory.GetDirectories(dirPath))
                        {
                            _appSettings.CurrentPlayList.Add(dir);
                        }

                    }
                }

                return _appSettings;
            }
        }

        public void Save()
        {
            // open config file
            string json = JsonConvert.SerializeObject(_appSettings);

            //write string to file
            System.IO.File.WriteAllText(_jsonSource, json);
        }

        /// <summary>
        /// Should the pictures in the folder shuffle?
        /// </summary>
        public bool Shuffle { get; set; }

        /// <summary>
        /// Should videos play with volume? Default off.
        /// </summary>
        public bool VideoVolume { get; set; }

        /// <summary>
        /// WebUI setting around if the directories should be expanded.
        /// </summary>
        public bool ExpandDirectoriesByDefault { get; set; }

        /// <summary>
        /// Rotation of the images and text
        /// </summary>
        public int Rotation { get; set; }
        /// <summary>
        /// keeps track of orientation for OMXPlayer
        /// </summary>
        public string OXMOrientnation { get; set; }


        /// <summary>
        /// List of directories which should be scanned for pictures
        /// </summary>
        public List<String> SearchDirectories { get; set; }

        public List<string> CurrentPlayList { get; set; }

        /// <summary>
        /// The size of the font for the info bar
        /// </summary>
        public int InfoBarFontSize { get; set; }

        public enum InfoBar { Clock, FileInfo, DateTime, Error, IP, OFF, InitialIP }
        public InfoBar InfoBarState { get; set; }

        /// <summary>
        /// Time to crossfade between images
        /// </summary>
        public int FadeTransitionTime { get; set; }

        /// <summary>
        /// Time to show each slide
        /// </summary>
        public int SlideshowTransitionTime { get; set; }

        public string DateTimeFormat { get; set; }
        // Font used for the clock/infobar
        public string DateTimeFontFamily { get; set; }

        // This is the currently selected 'playlist'.  
        // This will need to be expanded to an array in the future to support
        // multiple folders being selected at once
        public string CurrentDirectory { get; set; }

        public int NumberOfSecondsToShowIP { get; set; }

        /// <summary>
        /// This alters how images are shown..stretch / aspect ratio settings are part
        /// of this. Stretch is an Avalonia property.
        /// </summary>
        public Stretch ImageStretch { get; set; }

        /// <summary>
        /// Tracks the video fill setting for omxplayer. This is what states if it should
        /// zoom, stretch, show original size, etc.  Ignored for Windows currently.
        /// </summary>
        public string VideoStretch { get; set; }


        /// <summary>
        ///  Control if we should reload settings on the page (for layout/rendering)
        /// </summary>
        public bool ReloadSettings { get; set; }

        /// <summary>
        /// Track if we should Refresh the Directories or not (for changes such as added files)
        /// </summary>
        public bool RefreshDirctories { get; set; }

        /// <summary>
        /// The public port the webUI should be running on. This setting will require a restart
        /// to take effect, and the plan is to only expose it via the appsettings file.
        /// </summary>
        public int ListenerPort { get; set; }

                // Sync Settings
        public bool IsSyncEnabled { get; set; }

        /// <summary>
        /// Default for Screen On / Off function for buttons
        /// </summary>
        public bool ScreenStatus { get; set; }

        /// <summary>
        /// Show Date Time On / Off function for buttons
        /// </summary>
        public bool ShowInfoDateTime { get; set; }

        /// <summary>
        /// Show Infobar Filename On / Off function for buttons
        /// </summary>
        public bool ShowInfoFileName { get; set; }

        /// <summary>
        /// Show Infobar IP Address On / Off function for buttons
        /// </summary>
        public string ShowInfoIP { get; set; }

        /// <summary>
        /// Hide Infobar On / Off function for buttons
        /// </summary>
        public bool HideInfoBar { get; set; }

        /// <summary>
        /// Dynaframe Current IP
        /// </summary>
        public string DynaframeIP { get; set; }

        /// <summary>
        /// On / Off for slideshow paused
        /// </summary>
        public bool SlideShowPaused { get; set; }

        /// <summary>
        /// List of ip addresses which are used to send remote commands to control clients
        /// </summary>
        public List<string> RemoteClients { get; set; }

        /// <summary>
        /// List of folders, comma seperated, to ignore looking in
        /// </summary>
        public string IgnoreFolders { get; set; }

        /// <summary>
        /// Allows the enabling or disabling of logging. Defaulting to disabled, this can be turned on
        /// to help troubleshoot issues.
        /// </summary>
        public bool EnableLogging { get; set; }
    }
}