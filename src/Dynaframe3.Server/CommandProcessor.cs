using Dynaframe3.Server.Data;
using Dynaframe3.Server.SignalR;
using Dynaframe3.Shared;
using Dynaframe3.Shared.SignalR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace Dynaframe3.Server
{
    /// <summary>
    /// This class is responsible for processing commands from various sources (Webpage, get requests, mqtt, GPIO, etc)
    /// </summary>
    public class CommandProcessor
    {
        static readonly string _uploadsDirectory = AppDomain.CurrentDomain.BaseDirectory + "/wwwroot/uploads/";

        private readonly ServerDbContext _db;
        private readonly IHubContext<DynaframeHub, IFrameClient> _hub;
        private readonly ILogger<CommandProcessor> _logger;

        public CommandProcessor(ServerDbContext db, IHubContext<DynaframeHub, IFrameClient> hub, ILogger<CommandProcessor> logger)
        {
            _db = db;
            _hub = hub;
            _logger = logger;
        }


        /// <summary>
        /// Turns the screen off using vcgencmd (Linux only, doesn't work on all screens)
        /// </summary>
        public Task TurnOffScreenAsync(int deviceId)
        {
            return GetDevice(deviceId).TurnOffScreenAsync();
        }

        private IFrameClient GetDevice(int deviceId)
        {
            return _hub.Clients.Group(deviceId.ToString());
        }

        /// <summary>
        /// Turns the screen on using vcgencmd (Linux only, doesn't work on all screens)
        /// </summary>
        public Task TurnOnScreenAsync(int deviceId)
        {
            return _hub.Clients.Group(deviceId.ToString()).TurnOnScreenAsync();
        }

        private void SetInfoBar(string InfobarValue, AppSettings appSettings)
        {
            switch (InfobarValue.ToUpper())
            {
                case "INFOBAR_DATETIME_ON":
                    {
                        appSettings.InfoBarState = AppSettings.InfoBar.DateTime;
                        break;
                    }
                case "INFOBAR_FILENAME_ON":
                    {
                        appSettings.InfoBarState = AppSettings.InfoBar.FileInfo;
                        break;
                    }
                case "INFOBAR_EXIF_OFF":
                    {
                        appSettings.InfoBarState = AppSettings.InfoBar.ExifData;
                        break;
                    }
                case "INFOBAR_DATETIME_OFF":
                case "INFOBAR_FILENAME_OFF":
                case "INFOBAR_IP_OFF":
                case "INFOBAR_EXIF_ON":
                case "INFOBAR_HIDDEN":
                    {
                        appSettings.InfoBarState = AppSettings.InfoBar.OFF;
                        break;
                    }
                case "INFOBAR_IP_ON":
                    {
                        appSettings.InfoBarState = AppSettings.InfoBar.IP;
                        break;
                    }
                default:
                    {
                        break;
                    }
            }
        }

        /// <summary>
        /// These commands are for controling the slide show such as skipping,
        /// going back, pausing, etc.
        /// </summary>
        /// <param name="ControlCommand"></param>
        public Task ControlSlideshowAsync(string ControlCommand, int deviceId)
        {
            switch (ControlCommand)
            {
                case "CONTROL_FIRST":
                    {
                        return GetDevice(deviceId).FirstAsync();
                    }
                case "CONTROL_BACKWARD":
                    {
                        return GetDevice(deviceId).BackAsync();
                    }
                case "CONTROL_PAUSE_On":
                    {
                        return GetDevice(deviceId).TogglePauseAsync();
                    }

                case "CONTROL_PAUSE_Off":
                    {
                        return GetDevice(deviceId).TogglePauseAsync();
                    }



                case "CONTROL_FORWARD":
                    {
                        return GetDevice(deviceId).ForwardAsync();
                    }
                default:
                    {
                        return Task.CompletedTask;
                    }
            }
        }

        /// <summary>
        /// This method handles incoming transmissions from other frames. 
        /// </summary>
        /// <param name="filename"></param>
        /// <returns>True if appSettings exists and updated, else false</returns>
        public Task ProcessSetFile(string filename, int deviceId)
        {
            return GetDevice(deviceId).ProcessSetFileAsync(filename);
        }

        public async Task<bool> ProcessCommandAsync(string command, int deviceId)
        {
            var appSettings = await _db.Devices.Where(d => d.Id == deviceId)
                                            .Select(d => d.AppSettings)
                                            .FirstOrDefaultAsync()
                                            .ConfigureAwait(false);

            if (appSettings is null)
            {
                return false;
            }

            _logger.LogInformation("Command recieved: " + command);
            var commandFound = true;
            switch (command.ToUpper())
            {
                case "SCREENOFF":
                    {
                        await TurnOffScreenAsync(deviceId).ConfigureAwait(false);
                        appSettings.ScreenStatus = false;
                        break;
                    }
                case "SCREENON":
                    {
                        await TurnOnScreenAsync(deviceId).ConfigureAwait(false);
                        appSettings.ScreenStatus = true;
                        break;
                    }
                case "INFOBAR_DATETIME_OFF":
                    {
                        SetInfoBar(command, appSettings);
                        appSettings.ShowInfoDateTime = false;
                        break;
                    }
                case "INFOBAR_DATETIME_ON":
                    {
                        SetInfoBar(command, appSettings);
                        appSettings.ShowInfoDateTime = true;
                        appSettings.ShowInfoFileName = false;
                        appSettings.ShowInfoIP = "false";
                        break;
                    }
                case "INFOBAR_FILENAME_OFF":
                    {
                        SetInfoBar(command, appSettings);
                        appSettings.ShowInfoFileName = false;
                        break;
                    }
                case "INFOBAR_FILENAME_ON":
                    {
                        SetInfoBar(command, appSettings);
                        appSettings.ShowInfoDateTime = false;
                        appSettings.ShowInfoFileName = true;
                        appSettings.ShowInfoIP = "false";
                        break;
                    }
                case "INFOBAR_HIDDEN":
                    {
                        SetInfoBar(command, appSettings);
                        break;
                    }
                case "INFOBAR_EXIF_ON":
                    {
                        SetInfoBar(command, appSettings);
                        appSettings.ShowEXIFData = true;
                        break;
                    }
                case "INFOBAR_EXIF_OFF":
                    {
                        SetInfoBar(command, appSettings);
                        appSettings.ShowEXIFData = false;
                        break;
                    }
                case "INFOBAR_IP_OFF":
                    {
                        SetInfoBar(command, appSettings);
                        appSettings.ShowInfoIP = "false";
                        break;
                    }
                case "INFOBAR_IP_ON":
                    {
                        SetInfoBar(command, appSettings);
                        appSettings.ShowInfoDateTime = false;
                        appSettings.ShowInfoFileName = false;
                        appSettings.ShowInfoIP = "true";
                        break;
                    }
                case "CONTROL_FIRST":
                case "CONTROL_BACKWARD":
                case "CONTROL_FORWARD":
                    {
                        await ControlSlideshowAsync(command, deviceId).ConfigureAwait(false);
                        break;
                    }
                case "CONTROL_PAUSE_ON":
                    {
                        await ControlSlideshowAsync(command, deviceId).ConfigureAwait(false);
                        appSettings.SlideShowPaused = true;
                        break;
                    }
                case "CONTROL_PAUSE_OFF":
                    {
                        await ControlSlideshowAsync(command, deviceId).ConfigureAwait(false);
                        appSettings.SlideShowPaused = false;
                        break;
                    }
                case "REBOOT":
                    {
                        await GetDevice(deviceId).RebootAsync().ConfigureAwait(false);
                        break;
                    }
                case "SHUTDOWN":
                    {
                        await GetDevice(deviceId).ShutdownAsync().ConfigureAwait(false);
                        break;
                    }
                case "EXITAPP":
                    {
                        await GetDevice(deviceId).ExitAsync().ConfigureAwait(false);
                        break;
                    }
                case "UTILITY_UPDATEFILELIST":
                    {
                        appSettings.RefreshDirctories = true;
                        break;
                    }
                case "SHUFFLE_OFF":
                    {
                        appSettings.Shuffle = false;
                        break;
                    }
                case "SHUFFLE_ON":
                    {
                        appSettings.Shuffle = true;
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
                appSettings.ReloadSettings = true;

                await GetDevice(deviceId).SyncAppSettings(appSettings).ConfigureAwait(false);
            }

            await _db.SaveChangesAsync().ConfigureAwait(false);
            return true;
        }
    }
}
