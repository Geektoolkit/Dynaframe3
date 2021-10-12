using Dynaframe3.Client.Services;
using Dynaframe3.Shared;
using System;
using System.Threading.Tasks;

namespace Dynaframe3.Client
{
    public class StateContainer
    {
        private readonly AppSettingsService _service;

        public event Action<AppSettings> OnUpdated;

        public AppSettings Current { get; set; }

        public StateContainer(AppSettingsService service)
            => _service = service;

        public async Task GetLatestAsync()
        {
            Current = await _service.GetAppSettingsAsync().ConfigureAwait(false);
            OnUpdated?.Invoke(Current);
        }
    }
}
