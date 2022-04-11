using Dynaframe3.Client.Services;
using Dynaframe3.Shared;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Dynaframe3.Client
{
    public class StateContainer : IDisposable
    {
        private readonly DevicesService _service;
        private readonly DeviceSignalRService _signalRService;

        public event Action<AppSettings> OnUpdated;

        public int CurrentDeviceId { get; private set; }
        public AppSettings CurrentAppSettings { get; private set; }

        public StateContainer(DevicesService service, DeviceSignalRService signalRService)
        {
            _service = service;
            _signalRService = signalRService;

            _signalRService.OnAppSettingsChanged += OnAppSettingsChanged;
        }

        private void OnAppSettingsChanged(AppSettings newAppSettings)
        {
            CurrentAppSettings = newAppSettings;
            OnUpdated?.Invoke(newAppSettings);
        }

        public async Task SetCurrentDeviceAsync(int deviceId)
        {
            var appSettings = await _service.GetAppSettingsAsync(deviceId).ConfigureAwait(false);

            await _signalRService.DisconnectDeviceAsync(CurrentDeviceId).ConfigureAwait(false);
            await _signalRService.ConnectDeviceAsync(deviceId).ConfigureAwait(false);
            CurrentDeviceId = deviceId;
            OnAppSettingsChanged(appSettings);
        }

        public void Dispose()
        {
            _signalRService.OnAppSettingsChanged -= OnAppSettingsChanged;
        }
    }
}
