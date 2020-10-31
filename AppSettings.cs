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
        private AppSettings()
        {
            ReloadSettings = true;
            // marked as private to prevent outside classes from creating new.
            Shuffle = false;
            VideoVolume = false;

            Rotation = 0;
            NumberOfSecondsToShowIP = 15;

            OXMOrientnation = "--orientation 0";
            SearchDirectories = new List<string>() { };
            CurrentPlayList = new List<string>() { };

            CurrentDirectory = SpecialDirectories.MyPictures;
            DateTimeFormat = "H:mm tt";
            DateTimeFontFamily = "Terminal";

            Clock = false;
            InfoBarFontSize = 50;
            SlideshowTransitionTime = 30000;      // milliseconds between slides
            FadeTransitionTime = 1600;            // milliseconds for fades
            ImageStretch = Stretch.UniformToFill; // Default image stretch
            VideoStretch = "Fill";                // Used for OMXPlayer
            ExpandDirectoriesByDefault = false;   // WebUI setting to expand the trees

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
                        _appSettings.SearchDirectories.Add(SpecialDirectories.MyPictures);
                        _appSettings.CurrentPlayList.Clear();
                        foreach (string dir in Directory.GetDirectories(SpecialDirectories.MyPictures))
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
        /// Show the current time
        /// </summary>
        public bool Clock { get; set; }

        /// <summary>
        /// Should videos play with volume? Default off.
        /// </summary>
        public bool VideoVolume { get; set; }

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

        public bool ReloadSettings { get; set; }

        // This is the currently selected 'playlist'.  
        // This will need to be expanded to an array in the future to support
        // multiple folders being selected at once
        public string CurrentDirectory { get; set; }

        public int NumberOfSecondsToShowIP { get; set; }

        public Stretch ImageStretch { get; set; }

        public string VideoStretch { get; set; }



    }
}