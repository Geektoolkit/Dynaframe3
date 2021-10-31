using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Dynaframe3.TransitionTypes;
using MetadataExtractor;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Dynaframe3
{
    public class MainWindow : Window
    {
        CrossFadeTransition crossFadeTransition;
        TextBlock tb;
        Window mainWindow;
        Panel mainPanel;

        // Engines
        internal PlayListEngine playListEngine;

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

        // An empty constructor is required for the axaml to compile, but it is not actually used.
        public MainWindow()
        {
            throw new NotImplementedException();
        }

        public MainWindow(string[] args)
        {
            playListEngine = new PlayListEngine();
            InitializeComponent();
            SetupWebServer(args);
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
            fadeTransition.Duration = TimeSpan.FromMilliseconds(ServerAppSettings.Default.FadeTransitionTime);
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
            if (ServerAppSettings.Default.Rotation != 0)
            {
                RotateMainPanel();
            }

            tb = this.FindControl<TextBlock>("tb");
            tb.Foreground = Brushes.AliceBlue;
            tb.Text = "Loading images...";
            tb.FontFamily = new FontFamily("Terminal");
            tb.FontWeight = FontWeight.Bold;
            tb.FontSize = ServerAppSettings.Default.InfoBarFontSize;
            tb.Transitions = new Transitions();
            tb.Transitions.Add(fadeTransition);
            tb.Padding = new Thickness(30);

            VideoPlayer.MainPanelHandle = this.mainPanel;
            VideoPlayer.MainWindowHandle = this.mainWindow;

            string intro;
            if ((ServerAppSettings.Default.Rotation == 0) || ServerAppSettings.Default.Rotation == 180)
            {
                intro = Environment.CurrentDirectory + "/images/background.jpg";
            }
            else
            {
                intro = Environment.CurrentDirectory + "/images/vertbackground.jpg";
            }

            crossFadeTransition.SetImage(intro, 0);
            crossFadeTransition.SetImageStretch(ServerAppSettings.Default.ImageStretch);

            slideTimer.Elapsed += Timer_Tick;

            Stopwatch sw = new Stopwatch();

            Logger.LogComment("Initializing database..");
            sw.Start();
            playListEngine.InitializeDatabase();
            playListEngine.RebuildPlaylist();

            sw.Stop();
            Logger.LogComment("Database initialized. Took: " + sw.ElapsedMilliseconds + " ms.");

            ServerAppSettings.Default.ReloadSettings = true;
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
            PlayFile(playListEngine.CurrentMediaFile.Path, 500);
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
            PlayFile(playListEngine.CurrentMediaFile.Path, 500);
            lastUpdated = DateTime.Now;
            tb.Transitions.Add(fadeTransition);
        }
        public void GoToFirstImage()
        {
            tb.Transitions.Clear();
            playListEngine.GoToFirstFile();
            PlayFile(playListEngine.GetCurrentFile().Path, 500);

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

            if (e.Key == Avalonia.Input.Key.D)
            {
                playListEngine.DumpPlaylistToLog();
            }

            if (e.Key == Avalonia.Input.Key.F)
            {
                ServerAppSettings.Default.InfoBarState = ServerAppSettings.InfoBar.FileInfo;
            }
            if (e.Key == Avalonia.Input.Key.I)
            {
                ServerAppSettings.Default.InfoBarState = ServerAppSettings.InfoBar.IP;
            }
            if (e.Key == Avalonia.Input.Key.E)
            {
                ServerAppSettings.Default.InfoBarState = ServerAppSettings.InfoBar.ExifData;
            }
            if (e.Key == Avalonia.Input.Key.C)
            {
                ServerAppSettings.Default.InfoBarState = ServerAppSettings.InfoBar.DateTime;
            }
            if (e.Key == Avalonia.Input.Key.H)
            {
                tb.Opacity = 0;
                ServerAppSettings.Default.InfoBarState = ServerAppSettings.InfoBar.OFF;
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

            if (ServerAppSettings.Default.ReloadSettings)
            {
                try
                {
                    Logger.LogComment("Timer_Tick: Reload settings was true... loading settings");
                    ServerAppSettings.Default.ReloadSettings = false;
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
            if (ServerAppSettings.Default.RefreshDirctories)
            {
                ServerAppSettings.Default.RefreshDirctories = false;
                playListEngine.InitializeDatabase();
                playListEngine.RebuildPlaylist();
            }

            UpdateInfoBar();
            bool GoToNext = false;
            if ((playListEngine.CurrentMediaFile != null) && (playListEngine.CurrentMediaFile.Type == "Video"))
            {
                if (!VideoPlayer.CheckStatus(false))
                {
                    // Video exited before we expected it to! Recover gracefully
                    GoToNext = true;
                }
            }


            // check transpired time against transition time...
            if ((DateTime.Now.Subtract(lastUpdated).TotalMilliseconds > ServerAppSettings.Default.SlideshowTransitionTime) || GoToNext == true)
            {
                // See if a video is playing...
                if (VideoPlayer.CheckStatus(true))
                {
                    // Video is still playing, tick off the next timer interval for now.
                    slideTimer.Start();
                    return;
                }
                lastUpdated = DateTime.Now;
                playListEngine.GoToNext();
                Logger.LogComment("Next file is: " + playListEngine.CurrentMediaFile.Path);

                // sync frame call
                if ((ServerAppSettings.Default.IsSyncEnabled) && (SyncedFrame.SyncEngine.syncedFrames.Count > 0))
                {
                    SyncedFrame.SyncEngine.SyncFrames(playListEngine.CurrentMediaFile.Path);
                }
                PlayFile(playListEngine.CurrentMediaFile.Path);
            }
            slideTimer.Start(); // start next iterations...this prevents reentry...

        }
        public void PlayFile(string path)
        {
            PlayFile(path, ServerAppSettings.Default.FadeTransitionTime);
        }

        public void PlayFile(string path, int transitionTime)
        {
            Logger.LogComment("PlayFile() called with TransitionTime=" + transitionTime + " and path: " + path);
            PlayListItemType type = PlayListEngineHelper.GetPlayListItemTypeFromPath(path);
            VideoPlayer.KillVideoPlayer(); // Kill this...if this is called from gotonext / gotoprevious it can cause bad effects.
            try
            {
                // TODO: Try to 'peek' at next file, if video, then slow down more
                if (playListEngine.CurrentMediaFile.Type == "Video")
                {
                    VideoPlayer.PlayVideo(path);
                }
                else
                {
                    crossFadeTransition.SetImage(path, transitionTime);
                }
                Logger.LogComment("Media is now set.");
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
                tb.FontFamily = ServerAppSettings.Default.DateTimeFontFamily;
                if (DateTime.Now.Subtract(timeStarted).TotalSeconds < ServerAppSettings.Default.NumberOfSecondsToShowIP)
                {
                    tb.Text = Helpers.GetIPString();
                    tb.Opacity = 1;
                }
                else
                {
                    switch (ServerAppSettings.Default.InfoBarState)
                    {
                        case (ServerAppSettings.InfoBar.DateTime):
                            {
                                tb.Opacity = 1;
                                tb.Text = DateTime.Now.ToString(ServerAppSettings.Default.DateTimeFormat);
                                break;
                            }
                        case (ServerAppSettings.InfoBar.FileInfo):
                            {
                                tb.Opacity = 1;
                                FileInfo f = new FileInfo(playListEngine.CurrentMediaFile.Path);
                                string fData = f.Name;
                                IEnumerable<MetadataExtractor.Directory> directories = ImageMetadataReader.ReadMetadata(f.FullName);

                                tb.Text = f.Name;
                                break;
                            }
                        case (ServerAppSettings.InfoBar.IP):
                            {
                                tb.Opacity = 1;
                                tb.Text = Helpers.GetIPString();
                                break;
                            }
                        case (ServerAppSettings.InfoBar.OFF):
                            {
                                tb.Opacity = 0;
                                break;
                            }
                        case (ServerAppSettings.InfoBar.ExifData):
                            {
                                tb.Opacity = 1;
                                tb.Text = playListEngine.CurrentMediaFile.Title + "\r\n" + playListEngine.CurrentMediaFile.Author;
                                break;
                            }
                        default:
                            {
                                tb.Text = "";
                                break;
                            }
                    } // end switch

                    if ((IsPaused) && (ServerAppSettings.Default.InfoBarState != ServerAppSettings.InfoBar.OFF))
                    {
                        tb.Text += " (Paused)";
                    }

                } // end if
            });
        }


        public void SetupWebServer(string[] args)
        {
            var cts = new CancellationTokenSource();
            var host = HttpHost.CreateHostBuilder(args).Build();
            var task = host.RunAsync(cts.Token);

            Closing += (object sender, CancelEventArgs e) =>
            {
                cts.Cancel();

                try
                {
                    task.ConfigureAwait(false).GetAwaiter().GetResult();
                }
                finally
                {
                    host.Dispose();
                    cts.Dispose();
                }
            };
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
            tb.FontSize = ServerAppSettings.Default.InfoBarFontSize;

            // update stretch if changed
            crossFadeTransition.SetImageStretch(ServerAppSettings.Default.ImageStretch);

            RotateMainPanel();

            // update any fade settings
            fadeTransition.Duration = TimeSpan.FromMilliseconds(ServerAppSettings.Default.FadeTransitionTime);
            crossFadeTransition.SetTransitions();
        }
        /// <summary>
        /// Rotates the Main Panel to match the orientation specified in appsettings
        /// </summary>
        private void RotateMainPanel()
        {
            int degrees = ServerAppSettings.Default.Rotation;
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
            ServerAppSettings.Default.OXMOrientnation = "--orientation " + degrees.ToString();

        }
    }
}
