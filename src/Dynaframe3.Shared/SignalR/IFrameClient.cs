using System.Threading.Tasks;

namespace Dynaframe3.Shared.SignalR
{
    public interface IFrameClient
    {
        public Task TurnOffScreenAsync();

        public Task TurnOnScreenAsync();

        // Controls
        public Task FirstAsync();
        public Task BackAsync();
        public Task ForwardAsync();
        public Task TogglePauseAsync();

        public Task RebootAsync();
        public Task ShutdownAsync();
        public Task ExitAsync();

        public Task ProcessSetFileAsync(string filename);

        public Task SyncAppSettings(AppSettings appSettings);
    }
}
