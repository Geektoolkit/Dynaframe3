using Avalonia.Controls.ApplicationLifetimes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Dynaframe3
{
    /// <summary>
    /// This class is responsible for processing commands from various sources (Webpage, get requests, mqtt, GPIO, etc)
    /// </summary>
    public static class CommandProcessor
    {
        static MainWindow handleMainWindow = null;
        static readonly string _uploadsDirectory = AppDomain.CurrentDomain.BaseDirectory + "/wwwroot/uploads/";

        public static void GetMainWindowHandle(MainWindow mainWindow)
        {
            handleMainWindow = mainWindow;
        }
        /// <summary>
        /// Turns the screen off using vcgencmd (Linux only, doesn't work on all screens)
        /// </summary>
        public static void TurnOffScreen()
        {
            Helpers.RunProcess("vcgencmd", "display_power 0");
        }

        /// <summary>
        /// Turns the screen on using vcgencmd (Linux only, doesn't work on all screens)
        /// </summary>
        public static void TurnOnScreen()
        {
            Helpers.RunProcess("vcgencmd", "display_power 1");
        }

        public static void SetInfoBar(string InfobarValue)
        {
            switch (InfobarValue.ToUpper())
            {
                case "INFOBAR_DATETIME_ON":
                    {
                        ServerAppSettings.Default.InfoBarState = ServerAppSettings.InfoBar.DateTime;
                        break;
                    }
                case "INFOBAR_FILENAME_ON":
                    {
                        ServerAppSettings.Default.InfoBarState = ServerAppSettings.InfoBar.FileInfo;
                        break;
                    }
                case "INFOBAR_EXIF_OFF":
                    {
                        ServerAppSettings.Default.InfoBarState = ServerAppSettings.InfoBar.ExifData;
                        break;
                    }
                case "INFOBAR_DATETIME_OFF":
                case "INFOBAR_FILENAME_OFF":
                case "INFOBAR_IP_OFF":
                case "INFOBAR_EXIF_ON":
                case "INFOBAR_HIDDEN":
                    {
                        ServerAppSettings.Default.InfoBarState = ServerAppSettings.InfoBar.OFF;
                        break;
                    }
                case "INFOBAR_IP_ON":
                    {
                        ServerAppSettings.Default.InfoBarState = ServerAppSettings.InfoBar.IP;
                        break;
                    }
                default:
                    {
                        break;
                    }
            }

            ServerAppSettings.Default.ReloadSettings = true;
            ServerAppSettings.Default.Save();
        }

        /// <summary>
        /// These commands are for controling the slide show such as skipping,
        /// going back, pausing, etc.
        /// </summary>
        /// <param name="ControlCommand"></param>
        public static void ControlSlideshow(string ControlCommand)
        {
            switch (ControlCommand)
            {
                case "CONTROL_FIRST":
                    {
                        Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                        {
                            handleMainWindow.GoToFirstImage();
                        });
                        break;
                    }
                case "CONTROL_BACKWARD":
                    {
                        Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                        {
                            handleMainWindow.GoToPreviousImage();
                        });
                        break;
                    }
                case "CONTROL_PAUSE_On":
                    {
                        Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                        {
                            handleMainWindow.Pause();
                        });
                        break;
                    }

                case "CONTROL_PAUSE_Off":
                    {
                        Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                        {
                            handleMainWindow.Pause();
                        });
                        break;
                    }



                case "CONTROL_FORWARD":
                    {
                        Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                        {
                            handleMainWindow.GoToNextImage();
                        });
                        break;
                    }
                default:
                    {
                        break;
                    }
            }
        }

        /// <summary>
        /// This method handles incoming transmissions from other frames. 
        /// </summary>
        /// <param name="filename"></param>
        public static void ProcessSetFile(string filename)
        {
            Logger.LogComment("SYNC: SetFile recieved: " + filename);

            // We have to do a bit of logic to figure out what is the 'closest' we can come to this file on this frame
            // This next call is where the magic happens for going from another frames file to matching on this frame
            string localFile = handleMainWindow.playListEngine.ConvertFileNameToLocal(filename);

            Logger.LogComment("SYNC: Converted it to: " + filename);
            handleMainWindow.PlayFile(localFile);
        }
        public static void ProcessCommand(string command)
        {
            Logger.LogComment("Command recieved: " + command);
            var commandFound = true;
            switch (command.ToUpper())
            {
                case "SCREENOFF":
                    {
                        TurnOffScreen();
                        ServerAppSettings.Default.ScreenStatus = false;
                        break;
                    }
                case "SCREENON":
                    {
                        TurnOnScreen();
                        ServerAppSettings.Default.ScreenStatus = true;
                        break;
                    }
                case "INFOBAR_DATETIME_OFF":
                    {
                        SetInfoBar(command);
                        ServerAppSettings.Default.ShowInfoDateTime = false;
                        break;
                    }
                case "INFOBAR_DATETIME_ON":
                    {
                        SetInfoBar(command);
                        ServerAppSettings.Default.ShowInfoDateTime = true;
                        ServerAppSettings.Default.ShowInfoFileName = false;
                        ServerAppSettings.Default.ShowInfoIP = "false";
                        break;
                    }
                case "INFOBAR_FILENAME_OFF":
                    {
                        SetInfoBar(command);
                        ServerAppSettings.Default.ShowInfoFileName = false;
                        break;
                    }
                case "INFOBAR_FILENAME_ON":
                    {
                        SetInfoBar(command);
                        ServerAppSettings.Default.ShowInfoDateTime = false;
                        ServerAppSettings.Default.ShowInfoFileName = true;
                        ServerAppSettings.Default.ShowInfoIP = "false";
                        break;
                    }
                case "INFOBAR_HIDDEN":
                    {
                        SetInfoBar(command);
                        break;
                    }
                case "INFOBAR_EXIF_ON":
                    {
                        SetInfoBar(command);
                        ServerAppSettings.Default.ShowEXIFData = true;
                        break;
                    }
                case "INFOBAR_EXIF_OFF":
                    {
                        SetInfoBar(command);
                        ServerAppSettings.Default.ShowEXIFData = false;
                        break;
                    }
                case "INFOBAR_IP_OFF":
                    {
                        SetInfoBar(command);
                        ServerAppSettings.Default.ShowInfoIP = "false";
                        break;
                    }
                case "INFOBAR_IP_ON":
                    {
                        SetInfoBar(command);
                        ServerAppSettings.Default.ShowInfoDateTime = false;
                        ServerAppSettings.Default.ShowInfoFileName = false;
                        ServerAppSettings.Default.ShowInfoIP = "true";
                        break;
                    }
                case "CONTROL_FIRST":
                case "CONTROL_BACKWARD":
                case "CONTROL_FORWARD":
                    {
                        ControlSlideshow(command);
                        break;
                    }
                case "CONTROL_PAUSE_ON":
                    {
                        ControlSlideshow(command);
                        ServerAppSettings.Default.SlideShowPaused = true;
                        break;
                    }
                case "CONTROL_PAUSE_OFF":
                    {
                        ControlSlideshow(command);
                        ServerAppSettings.Default.SlideShowPaused = false;
                        break;
                    }
                case "REBOOT":
                    {
                        Helpers.RunProcess("reboot", "");
                        break;
                    }
                case "SHUTDOWN":
                    {
                        Helpers.RunProcess("shutdown", "now");
                        break;
                    }
                case "EXITAPP":
                    {
                        // Close up shop we're going home
                        Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                        {
                            handleMainWindow.Close();
                        });
                        throw new Exception("EXIT Called...exiting app!");
                    }
                case "UTILITY_UPDATEFILELIST":
                    {
                        ServerAppSettings.Default.RefreshDirctories = true;
                        break;
                    }
                case "SHUFFLE_OFF":
                    {
                        ServerAppSettings.Default.Shuffle = false;
                        break;
                    }
                case "SHUFFLE_ON":
                    {
                        ServerAppSettings.Default.Shuffle = true;
                        break;
                    }


                default:
                    {
                        commandFound = false;
                        break;
                    }
            }

            if (commandFound)
            {
                ServerAppSettings.Default.ReloadSettings = true;
            }

            ServerAppSettings.Default.Save();
        }

        // File Upload 

        public static async Task<string> SaveFileAsync(Stream input, string fileExtension)
        {
            //var dirPath= AppSettings.Default.CurrentDirectory + "/uploads/image_" + DateTime.Now.ToString("ddMMyyhhmmss") + "." + fileExtension;
            var fileName = "image_" + DateTime.Now.ToString("ddMMyyhhmmss") + fileExtension;
            var dirPath = _uploadsDirectory + fileName;
            //var dirPath = "/Users/rnewberger/Web/image4.jpg";

            using (var output = new FileStream(dirPath, FileMode.Create, FileAccess.Write))
            {
                await input.CopyToAsync(output).ConfigureAwait(false);
            }
            ControlSlideshow("CONTROL_FORWARD");
            return fileName;
        }

        public static IEnumerable<string> GetFiles()
        {
            var files = Directory.GetFiles(_uploadsDirectory);
            foreach (var file in files)
            {
                yield return Path.GetFileName(file);
            }
        }

        public static Stream GetFile(string fileName)
        {
            var dirPath = _uploadsDirectory + fileName;
            return File.OpenRead(dirPath);
        }

        public static void DeleteFile(string fileName)
        {
            var dirPath = _uploadsDirectory + fileName;
            File.Delete(dirPath);
            ControlSlideshow("CONTROL_FORWARD");
        }
    }
}
