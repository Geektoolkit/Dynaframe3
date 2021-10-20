using Dynaframe3.Client.Services;
using Dynaframe3.Shared;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Dynaframe3.Client
{
    public class StateContainer
    {
        private readonly AppSettingsService _service;
        private readonly SemaphoreSlim _sync = new SemaphoreSlim(1);

        public event Action<AppSettings> OnUpdated;

        private AppSettings _current;

        public StateContainer(AppSettingsService service)
            => _service = service;

        public async Task<AppSettings> SettingsUpdatedAsync()
        {
            _current = await _service.GetAppSettingsAsync().ConfigureAwait(false);
            OnUpdated?.Invoke(_current);
            return _current;
        }

        public async ValueTask<AppSettings> GetCurrentSettingsAsync()
        {
            if (_current is not null)
            {
                return _current;
            }

            await _sync.WaitAsync().ConfigureAwait(false);

            try
            {
                _current = await _service.GetAppSettingsAsync().ConfigureAwait(false);
            }
            finally
            {
                _sync.Release();
            }
            return _current;
        }
    }
}
