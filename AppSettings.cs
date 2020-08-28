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
            // marked as private to prevent outside classes from creating new.
            Paths = "";
            Shuffle = false;
            Rotation = 0;
            OXMOrientnation = "--orientation 0";
            CurrentDirectory = SpecialDirectories.MyPictures;
            //TimeFormat = "dd MMMM yyyy H:mm tt";
            TimeFormat = "H:mm tt";
            Width = 1920;
            Height = 1080;
            Clock = false;
            InfoBarFontSize = 50;

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

                    _jsonSource = $"{Directory.GetCurrentDirectory()}{Path.DirectorySeparatorChar}appsettings.json";



                    var config = builder.Build();
                    _appSettings = new AppSettings();
                    config.Bind(_appSettings);
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

        public string Paths { get; set; }
        /// <summary>
        /// Should the pictures in the folder shuffle?
        /// </summary>
        public bool Shuffle { get; set; }

        /// <summary>
        /// Show the current time
        /// </summary>
        public bool Clock { get; set; }
        /// <summary>
        /// Rotation of the images and text
        /// </summary>
        public int Rotation { get; set; }
        /// <summary>
        /// keeps track of orientation for OMXPlayer
        /// </summary>
        public string OXMOrientnation { get; set; }

        /// <summary>
        /// Keeps track of the current directory that is being used
        /// </summary>
        public string CurrentDirectory { get; set; }

        public string TimeFormat { get; set; }

        /// <summary>
        /// How many pixels wide should the window be?
        /// </summary>
        public int Width { get; set; }

        /// <summary>
        /// How many pixels High should the window be?
        /// </summary>
        public int Height { get; set; }

        /// <summary>
        /// The size of the font for the info bar
        /// </summary>
        public int InfoBarFontSize { get; set; }
    }
}