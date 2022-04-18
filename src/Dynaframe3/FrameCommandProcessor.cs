using Dynaframe3.Shared;
using Dynaframe3.Shared.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Configuration;
using Splat;
using System;
using System.Threading.Tasks;

namespace Dynaframe3
{
    internal class FrameCommandProcessor : IFrameClient, IAsyncDisposable
    {
        private readonly MainWindow _window;
        private readonly HubConnection _connection;

        public FrameCommandProcessor(MainWindow window)
        {
            _window = window;

            var config = Locator.Current.GetService<IConfiguration>();

            _connection = new HubConnectionBuilder()
                .WithAutomaticReconnect(new RetryPolicy())
                .WithUrl($"{config.GetValue<string>("DYNAFRAME_SERVER")}/Hub")
                .Build();

            _connection.On(nameof(IFrameClient.TurnOnScreenAsync), ((IFrameClient)this).TurnOnScreenAsync);
            _connection.On(nameof(IFrameClient.TurnOffScreenAsync), ((IFrameClient)this).TurnOffScreenAsync);


            _connection.On(nameof(IFrameClient.BackAsync), ((IFrameClient)this).BackAsync);
            _connection.On(nameof(IFrameClient.ForwardAsync), ((IFrameClient)this).ForwardAsync);
            _connection.On(nameof(IFrameClient.FirstAsync), ((IFrameClient)this).FirstAsync);
            _connection.On(nameof(IFrameClient.TogglePauseAsync), ((IFrameClient)this).TogglePauseAsync);

            _connection.On(nameof(IFrameClient.RebootAsync), ((IFrameClient)this).RebootAsync);
            _connection.On(nameof(IFrameClient.ShutdownAsync), ((IFrameClient)this).ShutdownAsync);
            _connection.On(nameof(IFrameClient.ExitAsync), ((IFrameClient)this).ExitAsync);

            _connection.On<string>(nameof(IFrameClient.ProcessSetFileAsync), ((IFrameClient)this).ProcessSetFileAsync);
            _connection.On<AppSettings>(nameof(IFrameClient.SyncAppSettings), ((IFrameClient)this).SyncAppSettings);
        }

        public async Task StartAsync(int deviceId)
        {
            await _connection.StartAsync().ConfigureAwait(false);
            await _connection.InvokeAsync("ConnectDeviceAsync", deviceId).ConfigureAwait(false);
        }

        public async ValueTask DisposeAsync()
        {
            await _connection.DisposeAsync().ConfigureAwait(false);
        }

        Task IFrameClient.BackAsync()
        {
            return Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() => _window.GoToPreviousImage());
        }

        Task IFrameClient.ExitAsync()
        {
            return Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
            {
                _window.Close();
            });
        }

        Task IFrameClient.FirstAsync()
        {
            return Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() => _window.GoToFirstImage());
        }

        Task IFrameClient.ForwardAsync()
        {
            return Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() => _window.GoToNextImage());
        }

        Task IFrameClient.ProcessSetFileAsync(string filename)
        {
            Logger.LogComment("SYNC: SetFile recieved: " + filename);

            // We have to do a bit of logic to figure out what is the 'closest' we can come to this file on this frame
            // This next call is where the magic happens for going from another frames file to matching on this frame
            string localFile = _window.playListEngine.ConvertFileNameToLocal(filename);

            Logger.LogComment("SYNC: Converted it to: " + filename);
            _window.PlayFile(localFile);
            return Task.CompletedTask;
        }

        Task IFrameClient.RebootAsync()
        {
            return Helpers.RunProcessAsync("reboot", "");
        }

        Task IFrameClient.ShutdownAsync()
        {
            return Helpers.RunProcessAsync("shutdown", "now");
        }

        Task IFrameClient.SyncAppSettings(AppSettings appSettings)
        {
            SyncedFrame.SyncEngine.SyncSettiings(appSettings);
            var appsettingsManager = Locator.Current.GetService<DeviceCache>();
            appsettingsManager.UpdateAppSettings(appSettings);
            return Task.CompletedTask;
        }

        Task IFrameClient.TogglePauseAsync()
        {
            return Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
            {
                _window.Pause();
            });
        }

        Task IFrameClient.TurnOffScreenAsync()
        {
            return Helpers.RunProcessAsync("vcgencmd", "display_power 0");
        }

        Task IFrameClient.TurnOnScreenAsync()
        {
            return Helpers.RunProcessAsync("vcgencmd", "display_power 1");
        }
    }
}
