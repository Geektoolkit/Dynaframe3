using Dynaframe3.Shared;
using Dynaframe3.Shared.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using System;
using System.Threading.Tasks;

namespace Dynaframe3.Client.Services
{
    public class DeviceSignalRService : IAsyncDisposable
    {
        private readonly HubConnection _hubConnection;

        public event Action<AppSettings> OnAppSettingsChanged;

        public DeviceSignalRService(HubConnection hubConnection)
        {
            _hubConnection = hubConnection;

            _hubConnection.On<AppSettings>(nameof(IFrameClient.SyncAppSettings), SyncAppSettings);
        }

        public Task SyncAppSettings(AppSettings appSettings)
        {
            OnAppSettingsChanged?.Invoke(appSettings);

            return Task.CompletedTask;
        }

        public async Task ConnectDeviceAsync(int deviceId)
        {
            await EnsureConnected().ConfigureAwait(false);
            await _hubConnection.InvokeAsync("ConnectDeviceAsync", deviceId).ConfigureAwait(false);
        }

        public async Task DisconnectDeviceAsync(int deviceId)
        {
            await EnsureConnected().ConfigureAwait(false);
            await _hubConnection.InvokeAsync("DisconnectDeviceAsync", deviceId).ConfigureAwait(false);
        }

        private async ValueTask EnsureConnected()
        {
            if (_hubConnection.State != HubConnectionState.Connected)
            {
                await _hubConnection.StartAsync().ConfigureAwait(false);
            }
        }

        public ValueTask DisposeAsync()
        {
            _hubConnection.Remove(nameof(IFrameClient.SyncAppSettings));
            return _hubConnection.DisposeAsync();
        }
    }
}
