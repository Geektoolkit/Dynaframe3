using Avalonia.Controls;
using Dynaframe3.Shared;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Dynaframe3
{


    static class VideoPlayer
    {
        static public bool IsPlaying;
        static private Process videoProcess; // handle to the video Player
        static public Window MainWindowHandle;
        static public Panel MainPanelHandle;

        public static void PlayVideo(string VideoPath, AppSettings appSettings)
        {
            IsPlaying = true;
            Logger.LogComment("Entering PlayVideoFile with Path: " + VideoPath);

            ProcessStartInfo pInfo = new ProcessStartInfo();
            pInfo.WindowStyle = ProcessWindowStyle.Maximized;
            
            // TODO: Parameterize omxplayer settings
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                Logger.LogComment("Linux Detected, setting up OMX Player");
                pInfo.FileName = "omxplayer";
                Logger.LogComment("Setting up Appsettings...");
                pInfo.Arguments = appSettings.OXMOrientnation + " --aspect-mode " + appSettings.VideoStretch + " ";

                // Append volume command argument
                if (!appSettings.VideoVolume)
                {
                    pInfo.Arguments += "--vol -6000 ";
                }

                pInfo.Arguments += "\"" + VideoPath + "\"";
                Logger.LogComment("DF Playing: " + VideoPath);
                Logger.LogComment("OMXPLayer args: " + pInfo.Arguments);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                pInfo.UseShellExecute = true;
                pInfo.FileName = "wmplayer.exe";
                pInfo.Arguments = "\"" + VideoPath + "\"";
                pInfo.Arguments += " /fullscreen";
                Logger.LogComment("Looking for media in: " + pInfo.Arguments);
            }


            videoProcess = new Process();
            videoProcess.StartInfo = pInfo;
            Logger.LogComment("PlayVideoFile: Starting player...");
            videoProcess.Start();
            Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
            {
                MainWindowHandle.Opacity = 0;
            });

            // Give video player time to start, then fade out to reveal it...
            System.Threading.Thread.Sleep(1100);
            Logger.LogComment("PlayVideoFile: Fading Foreground to reveal video player.");
            Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
            {
                MainWindowHandle.Opacity = 0;
            });
        }

        /// <summary>
        /// Returns true if the video is playing, false if it isn't...
        /// </summary>
        /// <returns></returns>
        public static bool CheckStatus(bool ForceTransition, AppSettings appSettings)
        {
            if ((videoProcess == null) || (videoProcess.HasExited))
            {
                IsPlaying = false;
                KillVideoPlayer();

                Logger.LogComment("VideoPlayer CheckStatus returning false..: Video has exited!");
                Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                {
                    MainPanelHandle.Opacity = 1;
                    MainWindowHandle.Opacity = 1;
                });
                return false;
            }
            else if((appSettings.PlaybackFullVideo == false) && (ForceTransition))
            {
                // We should interupt the video...check status gets called at the slide transition time.
                Logger.LogComment("VideoPlayer is closing playing video to transition to images..");
                Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                {
                    MainPanelHandle.Opacity = 1;
                    MainWindowHandle.Opacity = 1;
                });
                VideoPlayer.KillVideoPlayer();
                return true;

            }
            else
            {
                return true;
            }
        }

        public static void KillVideoPlayer()
        {
            Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
            {
                MainPanelHandle.Opacity = 1;
                MainWindowHandle.Opacity = 1;
            });
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

                    Helpers.RunProcessAsync("killall", "-q -9 omxplayer.bin");
                    videoProcess = null;

                }
                else
                {
                    videoProcess?.Close();
                    videoProcess?.Dispose();
                    videoProcess = null;
                }
            }
            catch (Exception)
            {
                // Swallow. This may no longer be there depending on what kills it (OMX player will exit if the video
                // completes for instance
            }
        }

    }
}
