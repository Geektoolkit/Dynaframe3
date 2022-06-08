using Avalonia.Media;
using System.Collections.Generic;

namespace Dynaframe3.Shared
{
    public class AppSettings
    {
        public int Id { get; set; }

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
        public List<string> SearchDirectories { get; set; }

        public Dictionary<string, List<string>> SearchSubDirectories { get; set; }

        public List<string> CurrentPlayList { get; set; }

        /// <summary>
        /// The size of the font for the info bar
        /// </summary>
        public int InfoBarFontSize { get; set; }

        public enum InfoBar { Clock, FileInfo, DateTime, Error, IP, OFF, InitialIP, ExifData }
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
        /// This controls how video playback is handled. If true the full video is always played, else transition time is obeyed
        /// </summary>

        public bool PlaybackFullVideo { get; set; }

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
        /// Show Exif Data On / Off function for buttons
        /// </summary>
        public bool ShowEXIFData { get; set; }
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

        public string InclusiveTagFilters { get; set; }

        /// <summary>
        /// Allows the enabling or disabling of logging. Defaulting to disabled, this can be turned on
        /// to help troubleshoot issues.
        /// </summary>
        public bool EnableLogging { get; set; }

        // BLURBOX Settings

        /// <summary>
        /// SigmaX amount of blur applied for an SKPaint blur effect
        /// </summary>
        public float BlurBoxSigmaX { get; set; }

        /// <summary>
        /// SigmaY amount of blur applied for an SKPaint blur effect
        /// </summary>
        public float BlurBoxSigmaY { get; set; }

        /// <summary>
        /// The margin the blurbox should use when displayed. Setting this to negative can scale the background to hide text/edges.
        /// </summary>
        public double BlurBoxMargin { get; set; }

        public AppSettings()
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
            // TODO: Remove this
            //DynaframeIP = Helpers.GetIP();        // Dynaframe IP Address
            SlideShowPaused = false;              // Pause Slideshow on / off
            EnableLogging = true;                 // Enables logging...should be set to false by default at some point.

            // Tag settings
            InclusiveTagFilters = "";             // Semicolon delimited list of images to include. Blank string means 'all'
            // Blurbox Settings
            BlurBoxSigmaX = 30;                   // See BlurboxImage.axaml.cs for usage
            BlurBoxSigmaY = 30;                   // Used in line: blurPaint.ImageFilter = SKImageFilter.CreateBlur(50, 50);
            BlurBoxMargin = -400;                 // Set to negative to scale the background image
        }
    }
}