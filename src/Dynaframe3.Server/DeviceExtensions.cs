using Dynaframe3.Shared;
using System.Net;

namespace Dynaframe3.Server
{
  internal static class DeviceExtensions
  {
    public static async Task ResetSubDirectoriesAsync(this Device device, HttpClient httpClient, ILogger logger, CancellationToken cancellationToken = default)
    {
      device.AppSettings.SearchSubDirectories = new();

      var parallelOptions = new ParallelOptions()
      {
        CancellationToken = cancellationToken,
        MaxDegreeOfParallelism = 3
      };

      await Parallel.ForEachAsync<string>(device.AppSettings.SearchDirectories, parallelOptions, async (dir, cancel) =>
      {   
        if (!Uri.TryCreate($"http://{device.Ip}:{device.Port}", new UriCreationOptions(), out var baseUri))
        {
          throw new InvalidOperationException($"Could not parse url from device http://{device.Ip}:{device.Port}");
        }

        var resp = await httpClient.GetAsync($"{baseUri}v1.0/Directories/Subdirectories?directory={WebUtility.UrlEncode(dir)}", cancel);

        if (resp.IsSuccessStatusCode)
        {
          device.AppSettings.SearchSubDirectories[dir] = await resp.Content.ReadFromJsonAsync<List<string>>();
        }
        else if (resp.StatusCode == HttpStatusCode.NotFound)
        {
          logger.LogInformation($"Directory '{dir}' on device {device.Ip} does not exist. This should be removed from the search directories");
        }
        else
        {
          logger.LogError($"Failed to pull sub directories for dir '{dir}' on device {device.Ip} with status '{(int)resp.StatusCode}'");
        }
      });
    }
  }
}
