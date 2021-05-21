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
using Avalonia.Rendering;
using Avalonia.Skia;
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
using MetadataExtractor;
using Avalonia.Rendering.SceneGraph;
using Dynaframe3.TransitionTypes;

namespace Dynaframe3
{
    public class MainWindow : Window
    {
        CrossFadeTransition crossFadeTransition;
        TextBlock tb;
        Window mainWindow;
        Panel mainPanel;
        Process videoProcess; // handle to the video Player

        // Engines
        internal PlayListEngine playListEngine;
        internal SimpleHTTPServer server;

        /// <summary>
        /// This controls the time between 'slides'
        /// </summary>
        // Timer which controls 'slides'
        // set to a low number to force a quick 'first slide' to appear
        System.Timers.Timer slideTimer = new System.Timers.Timer { AutoReset = false, Interval = 500 };

        // Track state of the engine. Lastupdated we set back in time so that the first
        // frame fires as soon as it can.
        DateTime lastUpdated = DateTime.Now.Subtract(TimeSpan.FromDays(1976));
        DateTime timeStarted = DateTime.Now;
        public bool IsPaused = false;

        // used for fading in/out fronttext
        DoubleTransition fadeTransition;

        Transform rotationTransform;

        public MainWindow()
        {
            playListEngine = new PlayListEngine();
            InitializeComponent();
            SetupWebServer();
        }

        private void InitializeComponent()
        {
            CommandProcessor.GetMainWindowHandle(this);
            SyncedFrame.SyncEngine.Initialize(); // initialize the list of frames for syncing.

            AvaloniaXamlLoader.Load(this);
            this.KeyDown += MainWindow_KeyDown;
            this.Closed += MainWindow_Closed;

            // setup transitions and animations
            // For mainWindow fades
            DoubleTransition windowTransition = new DoubleTransition();
            windowTransition.Duration = TimeSpan.FromMilliseconds(2000);
            windowTransition.Property = Window.OpacityProperty;

            // For mainPanel fades
            DoubleTransition panelTransition = new DoubleTransition();
            panelTransition.Duration = TimeSpan.FromMilliseconds(1200);
            panelTransition.Property = Panel.OpacityProperty;

            crossFadeTransition = this.FindControl<CrossFadeTransition>("CrossFadeImage");

            fadeTransition = new DoubleTransition();
            fadeTransition.Easing = new QuadraticEaseIn();
            fadeTransition.Duration = TimeSpan.FromMilliseconds(AppSettings.Default.FadeTransitionTime);
            fadeTransition.Property = UserControl.OpacityProperty;

            mainWindow = this.FindControl<Window>("mainWindow");
            mainWindow.Transitions = new Transitions();
            mainWindow.Transitions.Add(windowTransition);

            mainWindow.SystemDecorations = SystemDecorations.None;
            mainWindow.WindowState = WindowState.FullScreen;
            mainWindow.WindowStartupLocation = WindowStartupLocation.CenterScreen;

            mainPanel = this.FindControl<Panel>("mainPanel");
            mainPanel.Transitions = new Transitions();
            mainPanel.Transitions.Add(panelTransition);
            if (AppSettings.Default.Rotation != 0)
            {
                RotateMainPanel();
            }

            tb = this.FindControl<TextBlock>("tb");
            tb.Foreground = Brushes.AliceBlue;
            tb.Text = "Loading images...";
            tb.FontFamily = new FontFamily("Terminal");
            tb.FontWeight = FontWeight.Bold;
            tb.FontSize = AppSettings.Default.InfoBarFontSize;
            tb.Transitions = new Transitions();
            tb.Transitions.Add(fadeTransition);
            tb.Padding = new Thickness(30);


            string intro;
            if ((AppSettings.Default.Rotation == 0) || AppSettings.Default.Rotation == 180)
            {
                intro = Environment.CurrentDirectory + "/images/background.jpg";
            }
            else
            {
                intro = Environment.CurrentDirectory + "/images/vertbackground.jpg";
            }

            crossFadeTransition.SetImage(intro,0);
            crossFadeTransition.SetImageStretch(AppSettings.Default.ImageStretch);

            slideTimer.Elapsed += Timer_Tick;

            AppSettings.Default.ReloadSettings = true;
            slideTimer.Start();
            Timer_Tick(null, null);
            Logger.LogComment("Dynaframe Initialized...");

        }

        private void MainWindow_Closed(object sender, EventArgs e)
        {
            ((ClassicDesktopStyleApplicationLifetime)Avalonia.Application.Current.ApplicationLifetime).Shutdown(0);
        }

        /// <summary>
        /// Goes to the next image immediately. We add a default 500ms delay to try to keep the fade aesthetic
        /// </summary>
        public void GoToNextImage()
        {
            tb.Transitions.Clear();
            playListEngine.GoToNext();
            PlayImageFile(500, playListEngine.CurrentPlayListItem.Path);
            lastUpdated = DateTime.Now;
            tb.Transitions.Add(fadeTransition);
        }
        /// <summary>
        /// Goes back to the previous image. used for keyboard shortcuts or API calls. Keeps a small delay
        /// </summary>
        public void GoToPreviousImage()
        {
            tb.Transitions.Clear();
            playListEngine.GoToPrevious();
            PlayImageFile(500, playListEngine.CurrentPlayListItem.Path);
            lastUpdated = DateTime.Now;
            tb.Transitions.Add(fadeTransition);
        }
        public void GoToFirstImage()
        {
            tb.Transitions.Clear();
            playListEngine.GetPlayListItems();
            AppSettings.Default.RefreshDirctories = false;
            PlayImageFile(500, playListEngine.CurrentPlayListItem.Path);
            lastUpdated = DateTime.Now;
            tb.Transitions.Add(fadeTransition);
        }
        public void Pause()
        {
            if (IsPaused)
            {
                IsPaused = false;
            }
            else
            {
                IsPaused = true;
            }
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

            if (e.Key == Avalonia.Input.Key.T)
            {
                if (mainWindow.Opacity != 1)
                {
                    mainWindow.Opacity = 1;
                }
                else
                {
                    mainWindow.Opacity = 0;
                }
            }
            if (e.Key == Avalonia.Input.Key.U)
            {
                if (mainPanel.Opacity != 1)
                {
                    mainPanel.Opacity = 1;
                }
                else
                {
                    mainPanel.Opacity = 0;
                }
            }


            if (e.Key == Avalonia.Input.Key.F)
            {
                AppSettings.Default.InfoBarState = AppSettings.InfoBar.FileInfo;
            }
            if (e.Key == Avalonia.Input.Key.I)
            {
                AppSettings.Default.InfoBarState = AppSettings.InfoBar.IP;
            }
            if (e.Key == Avalonia.Input.Key.C)
            {
                AppSettings.Default.InfoBarState = AppSettings.InfoBar.DateTime;
            }
            if (e.Key == Avalonia.Input.Key.H)
            {
                tb.Opacity = 0;
                AppSettings.Default.InfoBarState = AppSettings.InfoBar.OFF;
            }

            if (e.Key == Avalonia.Input.Key.Right)
            {
                GoToNextImage();
            }
            if (e.Key == Avalonia.Input.Key.Left)
            {
                GoToPreviousImage();
            }

            if (e.Key == Avalonia.Input.Key.P)
            {
                Pause();
            }


            UpdateInfoBar();

        }

        /// <summary>
        /// The main processing loop. Will have to break this down at some point.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Timer_Tick(object sender, EventArgs e)
        {
            if (IsPaused)
            {
                Logger.LogComment("Timer_Tick: Currently paused..Updating the infobar...");
                UpdateInfoBar();
                // Note: Do not stop the timer...we need it to 'recheck'
                slideTimer.Start();
                return;
            }

            if (AppSettings.Default.ReloadSettings)
            {
                try
                {
                    Logger.LogComment("Timer_Tick: Reload settings was true... loading settings");
                    AppSettings.Default.ReloadSettings = false;
                    Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        RefreshSettings();
                    });
                }
                catch (Exception exc)
                {
                    Logger.LogComment("Timer_Tick: Exception reloading settings..." + exc.ToString());
                }

            }

            // Check to see if the directories flag was modified
            if (AppSettings.Default.RefreshDirctories)
            {
                try
                {

                    Logger.LogComment("Timer_Tick: Refresh Directories was true. Updating..");
                    //
                    // HACKHACK: We have to wait here while the filelist updates..if we move on we end up in a bad
                    // timing mess...the UI thread will try to continue with the file list being incorrect.
                    // 
                    //
                    AppSettings.Default.RefreshDirctories = false;
                    playListEngine.GetPlayListItems();
                    Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        Logger.LogComment("Timer_Tick: GetFiles is called. Should update next image...");
                        
                    });
                }
                catch (Exception exc)
                {
                    Logger.LogComment("Exception refreshing directories! " + exc.ToString());
                }
                lastUpdated = lastUpdated.Subtract(TimeSpan.FromMilliseconds(AppSettings.Default.SlideshowTransitionTime));
                Logger.LogComment("Timer_Tick: Exiting Refreshing Directories");

            }

            UpdateInfoBar();

            //
            //  This is the main check for if we need to switch frames. First we check to see if the 
            //  amount of time that has transpired is over our tranistion time. If so, we check:
            //  1) Are we syncing to other frames? if so send those signals
            //  2) Is it an audio or video? Call appropriate play method based on that.
            //
            if ((DateTime.Now.Subtract(lastUpdated).TotalMilliseconds > AppSettings.Default.SlideshowTransitionTime))
            {
                lastUpdated = DateTime.Now;
                playListEngine.GoToNext();
                Logger.LogComment("Next file is: " + playListEngine.CurrentPlayListItem.Path);

                // sync frame call
                if ((AppSettings.Default.IsSyncEnabled) &&(SyncedFrame.SyncEngine.syncedFrames.Count > 0))
                {
                    Logger.LogComment("SyncFrames enabled...sending sync signals..");
                    // We have frames to sync! Send this off to them:
                    try
                    {
                        SyncedFrame.SyncEngine.SyncFrames(playListEngine.CurrentPlayListItem.Path);
                    }
                    catch(Exception exc)
                    {
                        // This is a 'backstop' to catch any nastiness from networking. The whole network blind call to
                        // an IP thing is risky, and I've seen an instance where the try/catch in syncframes failed.
                        // Adding this to try to catch that if it happens and to understand why. Also to not 
                        // bring down the entire app due to network flakiness.
                        Logger.LogComment("ERROR: Excpetion trying to sync frames, caught in mainWindow. Excpetion: " + exc.ToString());
                    }
                }

                try
                {
                    // TODO: Try to 'peek' at next file, if video, then slow down more
                    if (playListEngine.CurrentPlayListItem.ItemType == PlayListItemType.Video)
                    {
                        KillVideoPlayer();
                        PlayVideoFile(playListEngine.CurrentPlayListItem.Path);
                    }
                    else
                    {
                        PlayImageFile(AppSettings.Default.FadeTransitionTime, playListEngine.CurrentPlayListItem.Path);
                        KillVideoPlayer(); // if a video is playing, get rid of it now that we've swapped images
                    }
                }
                catch (InvalidOperationException exc)
                {
                    Logger.LogComment("IOE Exception: " + exc.ToString());
                    // We expect this if a process is no longer around
                }
                catch (Exception exc)
                {
                    Logger.LogComment("ERROR: Exception processing file.." + exc.ToString());
                }
            }
            slideTimer.Start(); // start next iterations...this prevents reentry...

        }
        public void PlayFile(string path)
        {
            // Externally exposed API to allow for frame syncing or automation scenarios.
            // note: The file that we try to play may not exist, if not, we should look at the folder and try to
            // find it, if that doesn't exist then show randome file from playlist
            //
            // Future use from Avalonia Gitter: How to load an image from network path..
            // var response = await httpClient.GetAsync(bitmapPath, HttpCompletionOption.ResponseContentRead);
            // var stream = await response.Content.ReadAsStreamAsync();
            // bitmap = new Bitmap(stream);
           
            PlayListItemType type = PlayListEngineHelper.GetPlayListItemTypeFromPath(path);
            try
            {
                // TODO: Try to 'peek' at next file, if video, then slow down more
                if (type == PlayListItemType.Video)
                {
                    KillVideoPlayer();
                    PlayVideoFile(path);
                }
                else
                {
                    PlayImageFile(AppSettings.Default.FadeTransitionTime, path);
                    KillVideoPlayer(); // if a video is playing, get rid of it now that we've swapped images
                }
            }
            catch (InvalidOperationException)
            {
                // We expect this if a process is no longer around
            }
            catch (Exception exc)
            {
                Logger.LogComment("ERROR: PlayFile: Exception processing file.." + exc.ToString());
            }

        }
        private void UpdateInfoBar()
        {
            Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
            { 
                tb.FontFamily = AppSettings.Default.DateTimeFontFamily;
                if (DateTime.Now.Subtract(timeStarted).TotalSeconds < AppSettings.Default.NumberOfSecondsToShowIP)
                {
                    tb.Text = Helpers.GetIPString();
                    tb.Opacity = 1;
                }
                else
                {
                    switch (AppSettings.Default.InfoBarState)
                    {
                        case (AppSettings.InfoBar.DateTime):
                            {
                                tb.Opacity = 1;
                                tb.Text = DateTime.Now.ToString(AppSettings.Default.DateTimeFormat);
                                break;
                            }
                        case (AppSettings.InfoBar.FileInfo):
                            {
                                tb.Opacity = 1;
                                FileInfo f = new FileInfo(playListEngine.CurrentPlayListItem.Path);
                                string fData = f.Name;
                                IEnumerable<MetadataExtractor.Directory> directories = ImageMetadataReader.ReadMetadata(f.FullName);

                                tb.Text = f.Name;
                                break;
                            }
                        case (AppSettings.InfoBar.IP):
                            {
                                tb.Opacity = 1;
                                tb.Text = Helpers.GetIPString();
                                break;
                            }
                        case (AppSettings.InfoBar.OFF):
                            {
                                tb.Opacity = 0;
                                break;
                            }
                        default:
                            {
                                tb.Text = "";
                                break;
                            }
                    } // end switch

                    if ((IsPaused) && (AppSettings.Default.InfoBarState != AppSettings.InfoBar.OFF))
                    {
                        tb.Text += " (Paused)";
                    }

                } // end if
            });
        }
        public  void PlayImageFile(int millisecondsDelay, string path)
        {
            Logger.LogComment("PlayImageFile() called with Delay=" + millisecondsDelay + " and path: " + path);
            crossFadeTransition.SetImage(path, AppSettings.Default.FadeTransitionTime);
            Logger.LogComment("PlayImageFile: New Image now set");
        }
        private void PlayVideoFile(string path)
        {
            Logger.LogComment("Entering PlayVideoFile with Path: " + path);
            ProcessStartInfo pInfo = new ProcessStartInfo();
            pInfo.WindowStyle = ProcessWindowStyle.Maximized;

            // TODO: Parameterize omxplayer settings
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                Logger.LogComment("Linux Detected, setting up OMX Player");
                pInfo.FileName = "omxplayer";
                Logger.LogComment("Setting up Appsettings...");
                pInfo.Arguments = AppSettings.Default.OXMOrientnation + " --aspect-mode " + AppSettings.Default.VideoStretch + " ";

                // Append volume command argument
                if (!AppSettings.Default.VideoVolume)
                {
                    pInfo.Arguments += "--vol -6000 ";
                }

                pInfo.Arguments += "\"" + path + "\""; 
                Logger.LogComment("DF Playing: " + path);
                Logger.LogComment("OMXPLayer args: " + pInfo.Arguments);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                pInfo.UseShellExecute = true;
                pInfo.FileName = "wmplayer.exe";
                pInfo.Arguments = "\"" + path + "\"";
                pInfo.Arguments += " /fullscreen";
                Logger.LogComment("Looking for media in: " + pInfo.Arguments);
            }


            videoProcess = new Process();
            videoProcess.StartInfo = pInfo;
            Logger.LogComment("PlayVideoFile: Starting player...");
            videoProcess.Start();
            Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
            {
                mainPanel.Opacity = 0;
            });
           
            // Give video player time to start, then fade out to reveal it...
            System.Threading.Thread.Sleep(1100);
            Logger.LogComment("PlayVideoFile: Fading Foreground to reveal video player.");
            Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
            {
                mainWindow.Opacity = 0;
            });

            int timer = 0;
            Logger.LogComment("PlayVideoFile: Entering Timerloop");
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
               
            }
            Logger.LogComment("PlayVideoFile: Video has exited!");
            Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
            {
                mainPanel.Opacity = 1;
                mainWindow.Opacity = 1;
            });
        }

        private void KillVideoPlayer()
        {
            Logger.LogComment("KillVideoPlayer - Entering Method.");
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
                        Logger.LogComment("Tried and failed to kill video process. Exception: " + exc.ToString());
                    }
                }

                if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    // OMXPlayer processes can be a bit tricky. to kill them we use
                    // killall - 9 omxplayer.bin
                    // -q quiets this down in case omxplayer isn't running

                    Helpers.RunProcess("killall", "-q -9 omxplayer.bin");
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

        public void SetupWebServer()
        {
            string current = System.IO.Directory.GetCurrentDirectory();
            try
            {
                server = new SimpleHTTPServer(current + "//web", AppSettings.Default.ListenerPort);
                Logger.LogComment("SetupWebServer: Successfully setup webserver on default port :" + AppSettings.Default.ListenerPort);
            }
            catch (Exception)
            {
                // This can happen if the port is in use. Wittypi for instance uses 8000.  If this happens, message the user and try 5053 to try to reduce
                // the chances of a collision....and to give them a chance to pick one of thier own.
                Logger.LogComment("Error trying to start on default port!  Please set a new port in appsettings.json. Going to try again using Port 5053");
                AppSettings.Default.ListenerPort = 5053;
                server = new SimpleHTTPServer(current + "//web", AppSettings.Default.ListenerPort);
            }
        }

        /// <summary>
        /// This gets called each clock cycle, and is responsible for 'refreshing' settings, fixing up rotation rendering, etc.
        /// </summary>
        private void RefreshSettings()
        {
            Logger.LogComment("Refresh settings was called");
            Helpers.DumpAppSettingsToLogger();
            Logger.LogComment("Current opacity: " + mainWindow.Opacity);
           
            // Infobar is the text at the bottom (default 100)
            tb.FontSize = AppSettings.Default.InfoBarFontSize;

            // update stretch if changed
            crossFadeTransition.SetImageStretch(AppSettings.Default.ImageStretch);

            RotateMainPanel();

            // update any fade settings
            fadeTransition.Duration = TimeSpan.FromMilliseconds(AppSettings.Default.FadeTransitionTime);
            crossFadeTransition.SetTransitions();
        }
        /// <summary>
        /// Rotates the Main Panel to match the orientation specified in appsettings
        /// </summary>
        private void RotateMainPanel()
        {
            int degrees = AppSettings.Default.Rotation;
            double w = mainWindow.Width;
            double h = mainWindow.Height;

            // if the screen is rotated, and we haven't rendered anything yet, then we'll get back NaN when trying to 
            // calculate for rotation. Look for this and set some default guesses to get us by the first rendering.
            if (((Double.IsNaN(w) && (degrees == 90))) || ((Double.IsNaN(w) && (degrees == 270))))
            {
                mainWindow.Width = 1920;
                mainWindow.Height = 1080;
                // Screen hasn't rendered yet...force a resolution
                w = 1080;
                h = 1920;
                mainWindow.InvalidateMeasure();
                Logger.LogComment("Tried to fix rendering");
            }

            rotationTransform = new RotateTransform(degrees);

            if ((degrees == 90) || (degrees == 270))
            {
                mainPanel.Width = h;
                mainPanel.Height = w;
                Logger.LogComment("Rotating to Portrait. calculated  W: " + w + " H: " + h);
                Logger.LogComment("Rotation to Portrait. Panel  Width: " + mainPanel.Width + " Height: " + mainPanel.Height);
                Logger.LogComment("Rotation to Portrait. Window Width: " + mainWindow.Width + " Height: " + mainWindow.Height);
            }
            else
            {
                mainPanel.Width = w;
                mainPanel.Height = h;
            }
            mainPanel.RenderTransform = rotationTransform;
            mainWindow.InvalidateMeasure();
            AppSettings.Default.OXMOrientnation = "--orientation " + degrees.ToString();

        }
    }
}
