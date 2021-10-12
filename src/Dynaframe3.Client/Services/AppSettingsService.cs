using Dynaframe3.Shared;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace Dynaframe3.Client.Services
{
    public class AppSettingsService
    {
        private readonly HttpClient _client;

        public AppSettingsService(HttpClient client)
        {
            _client = client;
        }

        public Task<AppSettings> GetAppSettingsAsync()
            => _client.GetFromJsonAsync<AppSettings>("AppSettings");

        public async Task ExecuteCommand(string command)
        {
            var url = $"commands/{command}";
            var resp = await _client.SendAsync(new HttpRequestMessage(HttpMethod.Post, url));

            resp.EnsureSuccessStatusCode();
        }
    }
}
