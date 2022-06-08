using Dynaframe3.Server.Data;
using Dynaframe3.Server.SignalR;
using Dynaframe3.Shared;
using Dynaframe3.Shared.SignalR;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace Dynaframe3.Server.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("v{version:apiVersion}/Devices/{deviceId}/AppSettings")]
    public class AppSettingsController : Controller
    {
        private readonly ServerDbContext _db;
        private readonly HttpClient _httpClient;
        private readonly IHubContext<DynaframeHub, IFrameClient> _hub;
        private readonly ILogger<AppSettingsController> _logger;

        public AppSettingsController(ServerDbContext db, HttpClient httpClient, IHubContext<DynaframeHub,
            IFrameClient> hub, ILogger<AppSettingsController> logger)
        {
            _db = db;
            _httpClient = httpClient;
            _hub = hub;
            _logger = logger;
        }

        [HttpGet("")]
        public async Task<IActionResult> GetAsync([FromRoute] int deviceId, CancellationToken cancellationToken)
        {
            var device = await GetDevicesAsync(deviceId);

            if (device is null)
            {
                return NotFound($"Could not find AppSettings for Device ID '{deviceId}'");
            }

            await device.ResetSubDirectoriesAsync(_httpClient, _logger, cancellationToken);
            return Ok(device.AppSettings);
        }

        private async Task<Device?> GetDevicesAsync(int devideId)
        {
            return await _db.Devices
                .Include(d => d.AppSettings)
                .Where(d => d.Id == devideId)
                .FirstOrDefaultAsync();
        }

        [HttpPatch("")]
        public async Task<IActionResult> PatchAsync([FromRoute] int deviceId, [FromBody] JsonPatchDocument<AppSettings> jsonPatch)
        {
            _logger.LogInformation($"DAN: {Newtonsoft.Json.JsonConvert.SerializeObject(jsonPatch)}");
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var device = await GetDevicesAsync(deviceId);

            if (device is null)
            {
                return NotFound($"Could not find AppSettings for Device ID '{deviceId}'");
            }

            jsonPatch.ApplyTo(device.AppSettings);

            device.AppSettings.SearchDirectories = device.AppSettings.SearchDirectories.Distinct().ToList();

            if (!jsonPatch.Operations.Any(o => o.path == "/ReloadSettings"))
            {
                device.AppSettings.ReloadSettings = true;
            }

            await _db.SaveChangesAsync();

            await _hub.GetDevice(deviceId).SyncAppSettings(device.AppSettings);

            return Ok(device);
        }
    }
}
