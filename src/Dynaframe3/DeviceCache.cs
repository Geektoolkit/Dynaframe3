using Dynaframe3.Shared;
using Microsoft.VisualBasic.FileIO;
using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Dynaframe3
{
  internal class DeviceCache
  {
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly HttpSettings _httpSettings;

    public Device CurrentDevice { get; private set; }

    public DeviceCache(IHttpClientFactory httpClientFactory, HttpSettings httpSettings)
    {
      _httpClientFactory = httpClientFactory;
      _httpSettings = httpSettings;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
      var client = _httpClientFactory.CreateClient();
      var endpoint = _httpSettings.Endpoints.First();

      if (endpoint is null)
      {
        throw new InvalidOperationException("Could not find endpoint set up with '*', 'localhost', or an IP Address. One of these needs to be entered with the --urls command line arg");
      }

      string hostName, ip;
      if (IPAddress.TryParse(endpoint.Host, out var iPAddress))
      {
        hostName = Dns.GetHostName();
        ip = iPAddress.ToString();
      }
      else
      {
        hostName = endpoint.Host;
        ip = Dns.GetHostAddresses(hostName).FirstOrDefault(ip => (hostName == "localhost" || ip.ToString() != "127.0.1.1")
          && Uri.TryCreate($"http://{ip}", new UriCreationOptions(), out _))?.ToString()
          ?? throw new InvalidOperationException($"Could not determine IP Address for {hostName}");
      }

      var device = new Device()
      {
        HostName = hostName,
        Ip = ip,
        Port = endpoint.Port
      };

      device.AppSettings.SearchDirectories.Add(SpecialDirectories.MyPictures);
      device.AppSettings.SearchDirectories.Add(AppDomain.CurrentDomain.BaseDirectory + "uploads/");
      var resp = await client.PutAsJsonAsync($"v1.0/Devices", device, cancellationToken).ConfigureAwait(false);

      resp.EnsureSuccessStatusCode();

      device = await resp.Content.ReadFromJsonAsync<Device>(cancellationToken: cancellationToken).ConfigureAwait(false);
      CurrentDevice = device;
    }

    public void UpdateAppSettings(AppSettings newAppSettings)
    {
      CurrentDevice.AppSettings = newAppSettings;
    }
  }
}
