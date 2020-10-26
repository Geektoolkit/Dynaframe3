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
        int index;
        Image frontImage;
        Image backImage;
        TextBlock tb;
        Bitmap bitmapNew;
        Window mainWindow;
        Panel mainPanel;
        Process videoProcess; // handle to the video Player

        // Engines
        PlayListEngine playListEngine;

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
        int NumberOfSecondsToShowIP = AppSettings.Default.NumberOfSecondsToShowIP;

        /// <summary>
        /// This controls the time between 'slides'
        /// </summary>
        // Timer which controls 'slides'
        // set to a low number to force a quick 'first slide' to appear
        System.Timers.Timer slideTimer = new System.Timers.Timer(500);

        DateTime lastUpdated = DateTime.Now;

        Transform rotationTransform;

        public MainWindow()
        {
            playListEngine = new PlayListEngine();
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

            frontImage.Stretch = AppSettings.Default.ImageStretch;
            backImage.Stretch = AppSettings.Default.ImageStretch;



            DoubleTransition transition2 = new DoubleTransition();
            transition2.Easing = new QuadraticEaseIn();
            transition2.Duration = TimeSpan.FromMilliseconds(1600);
            transition2.Property = Image.OpacityProperty;

            frontImage.Transitions = new Transitions();
            frontImage.Transitions.Add(fadeTransition);
            backImage.Transitions = new Transitions();
            backImage.Transitions.Add(transition2);

            slideTimer.Elapsed += Timer_Tick;
       
            GetFiles();
            slideTimer.Start();

            Logger.LogComment("Initialized");
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
                this.Close();
                server.Stop();
            }

            if (e.Key == Avalonia.Input.Key.F)
            {
                tb.Opacity = 1;
                infoBar = InfoBar.FileInfo;
            }
            if (e.Key == Avalonia.Input.Key.I)
            {
                tb.Opacity = 1;
                infoBar = InfoBar.IP;
            }
            if (e.Key == Avalonia.Input.Key.C)
            {
                tb.Opacity = 1;
                infoBar = InfoBar.Clock;
            }
            if (e.Key == Avalonia.Input.Key.Right)
            {
                playListEngine.GoToNext();
                PlayImageFile();
                lastUpdated = DateTime.Now;

            }
            if (e.Key == Avalonia.Input.Key.Left)
            {
                playListEngine.GoToPrevious();
                PlayImageFile();
                lastUpdated = DateTime.Now;
            }
            if (e.Key == Avalonia.Input.Key.H)
            {
                tb.Opacity = 0;
            }

        }

        /// <summary>
        /// The main processing loop. Will have to break this down at some point.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Timer_Tick(object sender, EventArgs e)
        {
            if (AppSettings.Default.ReloadSettings)
            {
                Logger.LogComment("Reload settings was true");
                AppSettings.Default.ReloadSettings = false;
                Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                {
                    GetFiles();
                });
            }
            
            UpdateInfoBar();

            if (DateTime.Now.Subtract(lastUpdated).TotalMilliseconds > AppSettings.Default.SlideshowTransitionTime)
            {
                lastUpdated = DateTime.Now;
                playListEngine.GoToNext();
                Logger.LogComment("Next file is: " + playListEngine.CurrentPlayListItem.Path);
                try
                {
                    // TODO: Try to 'peek' at next file, if video, then slow down more
                    if (playListEngine.CurrentPlayListItem.ItemType == PlayListItemType.Video)
                    {
                        KillVideoPlayer();
                        PlayVideoFile();
                    }
                    else
                    {
                        PlayImageFile();
                        KillVideoPlayer(); // if a video is playing, get rid of it now that we've swapped images
                    }
                }
                catch (InvalidOperationException)
                { 
                    // We expect this if a process is no longer around
                }
                catch (Exception exc)
                {
                    Debug.WriteLine("ERROR: Exception processing file.." + exc.ToString());
                    Logger.LogComment("ERROR: Exception processing file.." + exc.ToString());
                }
            }
        }

        private void UpdateInfoBar()
        {
            Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
            {
                tb.FontFamily = AppSettings.Default.DateTimeFontFamily;
                switch (infoBar)
                {
                    case (InfoBar.Clock):
                        {
                            tb.Text = DateTime.Now.ToString(AppSettings.Default.DateTimeFormat);
                            break;
                        }
                    case (InfoBar.FileInfo):
                        {
                            tb.Text = playListEngine.CurrentPlayListItem.Path;
                            break;
                        }
                    case (InfoBar.IP):
                        {
                            numberOfTimes++;

                            tb.Text = Helpers.GetIPString();
                            // TODO: Get rid of magic number 2 (2 * 500ms clock timer = 1 second)
                            if (numberOfTimes > (NumberOfSecondsToShowIP * 2))
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
        }
        private void PlayImageFile()
        {
            Logger.LogComment("PlayImageFile() called");
           
           // Step 1: Set the background image to the new one
           // fade the top out, revealing the bottom
           Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
            {
                try
                {
                    Logger.LogComment("Beginning to load next file: " + playListEngine.CurrentPlayListItem.Path);
                    
                    bitmapNew = new Bitmap(playListEngine.CurrentPlayListItem.Path);

                    backImage.Source = bitmapNew;
                    backImage.Opacity = 1;
                    frontImage.Opacity = 0;
                    mainWindow.WindowState = WindowState.FullScreen;
                    RefreshSettings();
                }
                catch (Exception exc)
                {
                    Logger.LogComment("ERROR: Exception: " + exc.ToString());
                }
            });

            // We sleep on this thread to let the transition occur fully
            Thread.Sleep(AppSettings.Default.FadeTransitionTime);


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
                backImage.Opacity = 0;
            });
        }
        private void PlayVideoFile()
        {
            Logger.LogComment("Entering PlayVideoFile");
            ProcessStartInfo pInfo = new ProcessStartInfo();
            pInfo.WindowStyle = ProcessWindowStyle.Maximized;

            // TODO: Parameterize omxplayer settings
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                Logger.LogComment("Linux Detected, setting up OMX Player");
                pInfo.FileName = "omxplayer";
                Logger.LogComment("Setting up Appsettings...");
                pInfo.Arguments = AppSettings.Default.OXMOrientnation + " --aspect-mode stretch ";
                pInfo.Arguments += "\"" + playListEngine.CurrentPlayListItem.Path + "\""; 
                Logger.LogComment("DF Playing: " + playListEngine.CurrentPlayListItem.Path);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                pInfo.UseShellExecute = true;
                pInfo.FileName = "wmplayer.exe";
                pInfo.Arguments = "\"" + playListEngine.CurrentPlayListItem.Path + "\"";
                pInfo.Arguments += " /fullscreen";
                Console.WriteLine("Looking for media in: " + pInfo.Arguments);
            }


            videoProcess = new Process();
            videoProcess.StartInfo = pInfo;
            Logger.LogComment("Starting player...");
            videoProcess.Start();
            System.Threading.Thread.Sleep(1000);

            int timer = 0;
            Logger.LogComment("Entering Timerloop");
            while ((videoProcess != null) && (!videoProcess.HasExited))
            {
                timer += 1;
                System.Threading.Thread.Sleep(300);
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
            Logger.LogComment("Video has exited!");
            Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
            {
                mainWindow.Opacity = 1;
            });
            try
            {
               
                
            }
            catch (Exception)
            { 
                // swallow. 
            }
        }

        private void KillVideoPlayer()
        {
            try
            {
                if (videoProcess != null)
                {
                    try
                    {
                        videoProcess.CloseMainWindow();
                        videoProcess = null;
                    }
                    catch (InvalidOperationException)
                    {
                        // expected if the process isn't there.
                    }
                    catch (Exception exc)
                    {
                        Debug.WriteLine("Tried and failed to kill video process..." + exc.ToString());
                        Logger.LogComment("Tried and failed to kill video process. Excpetion: " + exc.ToString());
                    }
                }

                if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    // OMXPlayer processes can be a bit tricky. to kill them we use
                    // killall - 9 omxplayer.bin
                    Helpers.RunProcess("killall", "-9 omxplayer.bin");
                    videoProcess = null;
                }
                else
                {
                    videoProcess.Close();
                    videoProcess.Dispose();
                    videoProcess = null;
                }
            }
            catch (Exception)
            { 
                // Swallow. This may no longer be there depending on what kills it (OMX player will exit if the video
                // completes for instance
            }
        }
        private bool GetFiles()
        {
            Logger.LogComment("GetFiles called!");
            playListEngine.GetPlayListItems();
            RefreshSettings();
            return true;
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
            Logger.LogComment("Refresh settings was called");
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

            // update stretch if changed
            frontImage.Stretch = AppSettings.Default.ImageStretch;
            backImage.Stretch = AppSettings.Default.ImageStretch;


            int degrees = AppSettings.Default.Rotation;
            mainWindow.InvalidateVisual();

            double w = mainWindow.Width;
            double h = mainWindow.Height;

            rotationTransform = new RotateTransform(degrees);

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
            mainPanel.RenderTransform = rotationTransform;

            // update any fade settings
            fadeTransition.Duration = TimeSpan.FromMilliseconds(AppSettings.Default.FadeTransitionTime);

            AppSettings.Default.OXMOrientnation = "--orientation " + degrees.ToString();
        }

    }
}
