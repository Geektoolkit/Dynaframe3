using Dynaframe3.Shared;
using Dynaframe3.Shared.SignalR;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.SignalR.Client;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;

namespace Dynaframe3.Client.Services
{
    public class DevicesService
    {
        private readonly HttpClient _client;

        public DevicesService(HttpClient client)
        {
            _client = client;
        }

        public Task<List<Device>> GetDevicesAsync()
            => _client.GetFromJsonAsync<List<Device>>($"{ApiVersion.Version}/Devices");

        public Task<AppSettings> GetAppSettingsAsync(int deviceId)
            => _client.GetFromJsonAsync<AppSettings>($"{ApiVersion.Version}/Devices/{deviceId}/AppSettings");

        public async Task ExecuteCommandAsync(int deviceId, string command)
        {
            var url = $"{ApiVersion.Version}/Devices/{deviceId}/Commands/{command}";
            var resp = await _client.SendAsync(new HttpRequestMessage(HttpMethod.Post, url)).ConfigureAwait(false);

            resp.EnsureSuccessStatusCode();
        }

        public async Task<HttpResponseMessage> UpdateAppSettingsAsync(int deviceId, JsonPatchDocument<AppSettings> jsonPatch, bool errorOnBadStatus = true)
        {
            var content = new StringContent(JsonConvert.SerializeObject(jsonPatch), Encoding.UTF8, "application/json");
            var resp = await _client.PatchAsync($"{ApiVersion.Version}/Devices/{deviceId}/Appsettings", content).ConfigureAwait(false);

            if (errorOnBadStatus)
            {
                resp.EnsureSuccessStatusCode();
            }

            return resp;
        }
    }
}
