using Avalonia;
using Avalonia.Media;
using Dynaframe3.Shared;
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
    public class ServerAppSettings : AppSettings
    {
        // marked as private to prevent outside classes from creating new.
        private ServerAppSettings()
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
            FadeTransitionTime = 10000;            // milliseconds for fades
            ImageStretch = Stretch.UniformToFill; // Default image stretch

            // Video Settings
            VideoStretch = "Fill";                // Used for OMXPlayer
            PlaybackFullVideo = false;            // This means videos will obey transition time by default

            ExpandDirectoriesByDefault = false;   // WebUI setting to expand the trees
            ListenerPort = 8000;                  // Default port for the WebUI to listen on
            IsSyncEnabled = false;                // Sync with other frames off by default

            IgnoreFolders = ".lrlibrary,.photoslibrary"; // TODO: Expose this in the Advanced UI

            ScreenStatus = true;                  // Default for Screen On / Off function
            ShowInfoDateTime = false;             // Show Date Time in Infobar On / Off function
            ShowInfoFileName = false;             // Show Filename in Infobar On / Off function
            ShowEXIFData = false;                 // Show Exif Data in Infobar On / Off function
            ShowInfoIP = "false";                 // Show IP in Infobar On / Off function
            HideInfoBar = false;                  // Hide Infobar On / Off function
            DynaframeIP = Helpers.GetIP();        // Dynaframe IP Address
            SlideShowPaused = false;              // Pause Slideshow on / off
            EnableLogging = true;                 // Enables logging...should be set to false by default at some point.

            // Tag settings
            InclusiveTagFilters = "";             // Semicolon delimited list of images to include. Blank string means 'all'
            // Blurbox Settings
            BlurBoxSigmaX = 30;                   // See BlurboxImage.axaml.cs for usage
            BlurBoxSigmaY = 30;                   // Used in line: blurPaint.ImageFilter = SKImageFilter.CreateBlur(50, 50);
            BlurBoxMargin = -400;                 // Set to negative to scale the background image
        }

        private static string _jsonSource;
        private static object _sync = new();
        private static ServerAppSettings _appSettings = null;
        public static ServerAppSettings Default
        {
            get
            {
                lock (_sync)
                {
                    if (_appSettings == null)
                    {
                        var builder = new ConfigurationBuilder()
                            .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

                        _jsonSource = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");

                        var config = builder.Build();
                        _appSettings = new ServerAppSettings();
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
                            string dirPath = AppDomain.CurrentDomain.BaseDirectory + "wwwroot/uploads/";
                            _appSettings.SearchDirectories.Add(dirPath);
                            foreach (string dir in Directory.GetDirectories(dirPath))
                            {
                                _appSettings.CurrentPlayList.Add(dir);
                            }
                        }
                    }
                }

                return _appSettings;
            }
        }

        public void Save()
        {
            // We don't want to save the subdirectories but we do want to return it from the API so we can't simply use a JsonIgnoreAttribute.
            var currentSubDirs = SearchSubDirectories;
            SearchSubDirectories = null;

            // open config file
            string json = JsonConvert.SerializeObject(_appSettings);

            SearchSubDirectories = currentSubDirs;

            //write string to file
            System.IO.File.WriteAllText(_jsonSource, json);
        }
    }
}