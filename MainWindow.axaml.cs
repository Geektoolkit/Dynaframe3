using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Animators;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.LogicalTree;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using System;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.VisualBasic.FileIO;
using System.Collections.Generic;
using SkiaSharp;

namespace Dynaframe3
{
    public class MainWindow : Window
    {

        static private List<string> fileList;
        int index;
        Image frontImage;
        Image backImage;
        TextBlock tb;
        Bitmap bitmapNew;
        Window mainWindow;
        Panel mainPanel;


        // Transitions used for animating the fades
        DoubleTransition fadeTransition;

        string PlayingDirectory = "";
        SimpleHTTPServer server;

        enum InfoBar { Clock, FileInfo, DateTime, Error, IP}
        InfoBar infoBar = InfoBar.IP;

        /// <summary>
        /// Tracks number of times the system has gone through a loop. useful for
        /// settings such as the IP address 
        /// </summary>
        int numberOfTimes = 0;
        const int NumberOfSecondsToShowIP = 15;

        /// <summary>
        /// This controls the time between 'slides'
        /// </summary>
        // Timer which controls 'slides'
        // set to a low number to force a quick 'first slide' to appear
        System.Timers.Timer slideTimer = new System.Timers.Timer(500);

        /// <summary>
        /// This fires every second to update the clock.  
        /// // TODO: Investigate using synchronizedobject here to sync to UI thread
        /// </summary>
        System.Timers.Timer clock = new System.Timers.Timer(1000);


        public MainWindow()
        {
            InitializeComponent();
            SetupWebServer();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
            this.KeyDown += MainWindow_KeyDown;
            this.Closed += MainWindow_Closed;

            // setup transitions and animations
            DoubleTransition windowTransition = new DoubleTransition();
            windowTransition.Duration = TimeSpan.FromMilliseconds(1000);
            windowTransition.Property = Window.OpacityProperty;
           
            fadeTransition = new DoubleTransition();
            fadeTransition.Easing = new QuadraticEaseIn();
            fadeTransition.Duration = TimeSpan.FromMilliseconds(AppSettings.Default.FadeTransitionTime);
            fadeTransition.Property = Image.OpacityProperty;

            mainWindow = this.FindControl<Window>("mainWindow");
            mainWindow.Transitions = new Transitions();
            mainWindow.Transitions.Add(windowTransition);
            mainWindow.SystemDecorations = SystemDecorations.None;
            mainWindow.WindowState = WindowState.Maximized;
            mainWindow.WindowStartupLocation = WindowStartupLocation.CenterScreen;

            mainPanel = this.FindControl<Panel>("mainPanel");
            
            tb = this.FindControl<TextBlock>("tb");
            tb.Foreground = Brushes.AliceBlue;
            tb.Text = "Loaded";
            tb.FontFamily = new FontFamily("Terminal");
            tb.FontWeight = FontWeight.Bold;
            tb.FontSize = AppSettings.Default.InfoBarFontSize;
            tb.Transitions = new Transitions();
            tb.Transitions.Add(fadeTransition);

            frontImage = this.FindControl<Image>("Front");
            backImage = this.FindControl<Image>("Back");

            DoubleTransition transition2 = new DoubleTransition();
            transition2.Easing = new QuadraticEaseIn();
            transition2.Duration = TimeSpan.FromMilliseconds(1600);
            transition2.Property = Image.OpacityProperty;

            frontImage.Transitions = new Transitions();
            frontImage.Transitions.Add(fadeTransition);
            backImage.Transitions = new Transitions();
            backImage.Transitions.Add(transition2);

            slideTimer.Elapsed += Timer_Tick;
            clock.Elapsed += Clock_Elapsed;

            RefreshSettings();
            GetFiles();

            slideTimer.Start();
            clock.Start();

            RaspberryPiPrep();
        }

        /// <summary>
        /// Raspberry pi/linux commands to try to keep the screen on
        /// </summary>
        private void RaspberryPiPrep()
        {
            try
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    ProcessStartInfo pInfo = new ProcessStartInfo();
                    pInfo.FileName = "setterm";
                    pInfo.Arguments = "-blank 0 -powerdown 0";
                    Process p = new Process();
                    p.StartInfo = pInfo;
                    p.Start();
                }
            }
            catch (Exception) { }
            
        }

        private void MainWindow_Closed(object sender, EventArgs e)
        {
            ((ClassicDesktopStyleApplicationLifetime)Avalonia.Application.Current.ApplicationLifetime).Shutdown(0);
        }

        private void MainWindow_KeyDown(object sender, Avalonia.Input.KeyEventArgs e)
        {
            // Exit on Escape or Control X (Windows and Linux Friendly)
            if ((e.Key == Avalonia.Input.Key.Escape) || ((e.KeyModifiers == Avalonia.Input.KeyModifiers.Control) && (e.Key == Avalonia.Input.Key.X)))
            {
                slideTimer.Stop();
                clock.Stop();
                this.Close();
                server.Stop();
            }
        }

        /// <summary>
        /// The main processing loop. Will have to break this down at some point.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Timer_Tick(object sender, EventArgs e)
        {
            index++;
            if ((fileList == null) || (fileList.Count == 0))
            {
                Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                {
                    tb.Text = "Error! No files found! Path is: " + AppSettings.Default.SearchDirectories.ToString();
                    infoBar = InfoBar.Error;
                });
                
                return;
            }

            // Get drives that are available
            // TODO: Initial Work to support inserted USB drives for loading pictures
            // Need to maintain a list of locations media can be found
            // and gracefully handle when it disappears
            DriveInfo[] driveInfo = System.IO.DriveInfo.GetDrives();
            foreach (DriveInfo info in driveInfo)
            {
                if (info.DriveType == DriveType.Removable)
                {
                    //Console.WriteLine("info: " + info.Name);
                    //Console.WriteLine("type: " + info.DriveType);
                }
            }


            if (index == fileList.Count)
            {
                index = 0;
            }

            if ((fileList == null) || (fileList.Count == 0))
            {
                return;

            }

            IReadOnlyList<MetadataExtractor.Directory> data = MetadataExtractor.ImageMetadataReader.ReadMetadata(fileList[index]);
            foreach (var directory in data)
                foreach (var tag in directory.Tags)
                    Debug.WriteLine($"{directory.Name} - {tag.Name} = {tag.Description}");
            try
            {
                // ensure the file is there
                if (!File.Exists(fileList[index]))
                {
                    this.Timer_Tick(null, null); // Move on to next file in the list
                }


                // TODO: Try to 'peek' at next file, if video, then slow down more
                if ((fileList[index].ToUpper().EndsWith(".MOV"))
                    || (fileList[index].ToUpper().EndsWith(".MP4"))
                    || (fileList[index].ToUpper().EndsWith(".AVI"))
                    || (fileList[index].ToUpper().EndsWith(".MKV"))
                    || (fileList[index].ToUpper().EndsWith(".MPEG"))
                    )
                {
                    PlayVideoFile();
                }
                else
                {
                    ///
                    ///    Photo swap code
                    ///

                    // Step 1: Set the background image to the new one
                    // fade the top out, revealing the bottom
                    Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        try
                        {
                            RefreshSettings();
                            bitmapNew = new Bitmap(fileList[index]);
                            
                            backImage.Source = bitmapNew;
                            backImage.Opacity = 1;
                            frontImage.Opacity = 0;
                            mainWindow.WindowState = WindowState.FullScreen;
                        }
                        catch (Exception exc)
                        {
                            tb.Text = "broken " + exc.Message;
                        }
                    });

                    // We sleep on this thread to let the transition occur fully
                    System.Threading.Thread.Sleep(AppSettings.Default.FadeTransitionTime);


                    // At this point the 'bottom' image is opaque showing the new image
                    // We set the top to that image, and fade it in
                    // we temporarily clear the transiton out so it's instant
                    Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        try
                        {
                            frontImage.Transitions.Clear();
                            frontImage.Source = backImage.Source;
                            frontImage.Opacity = 1;
                            frontImage.Transitions.Add(fadeTransition);
                        }
                        catch (Exception exc)
                        {
                            tb.Text = "Error: " + exc.Message;
                        }
                    });

                    // We then fade the background back out to keep it out of the way (in case we 
                    Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        backImage.Opacity = 0;
                    });
                }
            }
            catch (Exception)
            {
                // Another thread messed with the filelist...ignore for now
                // I'll refactor this later.
            }
        }

        private void PlayVideoFile()
        {
            this.slideTimer.Stop(); // stop processing images...
            ProcessStartInfo pInfo = new ProcessStartInfo();
            pInfo.WindowStyle = ProcessWindowStyle.Maximized;

            // TODO: Parameterize omxplayer settings
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                pInfo.FileName = "omxplayer";
                pInfo.Arguments = AppSettings.Default.OXMOrientnation + " --aspect-mode stretch " + fileList[index];
                Console.WriteLine("DF Playing: " + fileList[index]);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                pInfo.UseShellExecute = true;
                pInfo.FileName = "wmplayer.exe";
                pInfo.Arguments = fileList[index];
                pInfo.Arguments += " /fullscreen";
                Console.WriteLine("Looking for media in: " + pInfo.Arguments);
            }


            Process p = new Process();
            p.StartInfo = pInfo;
            p.Start();
            System.Threading.Thread.Sleep(500);

            int timer = 0;
            while (!p.HasExited)
            {
                timer += 1;
                System.Threading.Thread.Sleep(100);
                if (timer > 400)
                {
                    // timeout to not 'hang'
                    // TODO: Add a setting for this
                    break;
                }
                Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                {
                    mainWindow.Opacity = .1;
                });
            }
            Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
            {
                mainWindow.Opacity = 1;
            });
            p.Close();
            p.Dispose();
            System.Threading.Thread.Sleep(500);
            this.slideTimer.Start(); // resume
            Timer_Tick(null, null); // force next tick
        }

        /// <summary>
        /// Secondary 'clock' loop. Mainly controls the secondary UI for now.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Clock_Elapsed(object sender, ElapsedEventArgs e)
        {
            // Case where files don't match
            if (AppSettings.Default.ReloadSettings)
            {
                AppSettings.Default.ReloadSettings = false;
                Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                {
                    GetFiles();
                    this.Timer_Tick(null, null);
                });
            }

            Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
            {
                switch (infoBar)
                {
                    case (InfoBar.Clock):
                        {
                            tb.Text = DateTime.Now.ToString(AppSettings.Default.DateTimeFormat);
                            break;
                        }
                    case (InfoBar.Error):
                        {
                            tb.Text = "Error! Please check your appsettings.json!";
                            tb.FontSize = 75;
                            tb.Foreground = Brushes.Red;
                            break;
                        }
                    case (InfoBar.FileInfo):
                        { 
                            tb.Text = AppSettings.Default.Shuffle.ToString();
                            break;
                        }
                    case (InfoBar.IP):
                    {
                        numberOfTimes++;

                        tb.Text = Helpers.GetIPString();
                        if (numberOfTimes > NumberOfSecondsToShowIP)
                        {
                            infoBar = InfoBar.Clock;
                        }
                        break;
                    }
                    default:
                        {
                            tb.Text = "";
                            break;
                        }
                }

            });

            // NOTE: We update this here in case a bad value gets put into the app settings
            // We only try to update it if it's changed. Setting that to a bad value can be catastrophic.
            // May consider limiting the values in the future.
            if (slideTimer.Interval != AppSettings.Default.SlideshowTransitionTime)
            {
                slideTimer.Interval = AppSettings.Default.SlideshowTransitionTime;
                Timer_Tick(null, null);
            }

        }

        private bool GetFiles()
        {
            index = 0;
            fileList = new List<string>(); // get a list of files to go through
            slideTimer.Stop();
            string directory = AppSettings.Default.CurrentDirectory;
            try
            {

                if (Directory.Exists(directory))
                {
                    DirectoryInfo dirInfo = new DirectoryInfo(directory);

                    var mediafiles = Helpers.GetFilesByExtensions(dirInfo, ".jpg", ".jpeg", ".png",
                        ".bmp", ".mov", ".mpg", ".avi", ".mkv", ".mpeg", ".mp4");

                    if (mediafiles != null)
                        fileList.AddRange(mediafiles.ToList());

                    if (AppSettings.Default.Shuffle)
                    {
                        Random r = new Random((int)DateTime.Now.Ticks);
                        fileList = Helpers.Shuffle<string>(fileList.ToList(), r).ToList();
                    }
                }
            }
            catch (Exception exc)
            {
                Console.WriteLine("Exception when reading directory: " + directory + " Exception: " + exc.ToString());
            }
            slideTimer.Start();

            if (fileList.Count > 0)
            {
                return true;
            }
            else
            {
                // 0 files found in all given directories
                return false;
            }
        }   

        public void SetupWebServer()
        {
            string current = Directory.GetCurrentDirectory();
            server = new SimpleHTTPServer(current + "//web", 8000);
        }

        /// <summary>
        /// This gets called each clock cycle, and is responsible for 'refreshing' settings, fixing up rotation rendering, etc.
        /// </summary>
        private void RefreshSettings()
        {
            if (AppSettings.Default.Clock)
            {
                tb.Opacity = 1;
            }
            else
            {
                if (infoBar != InfoBar.IP)
                {
                    tb.Opacity = 0;
                }
            }

            // Infobar is the text at the bottom (default 100)
            tb.FontSize = AppSettings.Default.InfoBarFontSize;

            // Fix up rotations. When rotating, we must redo the layout
            // to get everything to resize correctly
            int degrees = AppSettings.Default.Rotation;
            mainWindow.InvalidateVisual();

          

            Transform t = new RotateTransform(degrees);
            double w = mainWindow.Width;
            double h = mainWindow.Height;

            

            if ((degrees == 90) || (degrees == 270))
            {
                mainPanel.Width = h;
                mainPanel.Height = w;
            }
            else
            {
                mainPanel.Width = w;
                mainPanel.Height = h;
            }
            mainPanel.RenderTransform = t;

            // update any fade settings
            fadeTransition.Duration = TimeSpan.FromMilliseconds(AppSettings.Default.FadeTransitionTime);

            AppSettings.Default.OXMOrientnation = "--orientation " + degrees.ToString();
        }

    }
}
