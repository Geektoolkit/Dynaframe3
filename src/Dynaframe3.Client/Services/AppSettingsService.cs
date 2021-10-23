using Dynaframe3.Shared;
using Microsoft.AspNetCore.JsonPatch;
using Newtonsoft.Json;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
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

        public async Task ExecuteCommandAsync(string command)
        {
            var url = $"commands/{command}";
            var resp = await _client.SendAsync(new HttpRequestMessage(HttpMethod.Post, url)).ConfigureAwait(false);

            resp.EnsureSuccessStatusCode();
        }

        public async Task<HttpResponseMessage> UpdateAppSettingsAsync(JsonPatchDocument<AppSettings> jsonPatch, bool errorOnBadStatus = true)
        {
            var content = new StringContent(JsonConvert.SerializeObject(jsonPatch), Encoding.UTF8, "application/json");
            var resp = await _client.PatchAsync("appsettings", content).ConfigureAwait(false);

            if (errorOnBadStatus)
            {
                resp.EnsureSuccessStatusCode();
            }

            return resp;
        }
    }
}
