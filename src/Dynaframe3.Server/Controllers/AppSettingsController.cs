using Dynaframe3.Server.Data;
using Dynaframe3.Shared;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Net;

namespace Dynaframe3.Server.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("v{version:apiVersion}/Devices/{deviceId}/AppSettings")]
    public class AppSettingsController : Controller
    {
        private readonly ServerDbContext _db;
        private readonly HttpClient _httpClient;
        private readonly ILogger<AppSettingsController> _logger;

        public AppSettingsController(ServerDbContext db, HttpClient httpClient,
            ILogger<AppSettingsController> logger)
        {
            _db = db;
            _httpClient = httpClient;
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
            return await _db.Devices.AsNoTracking()
                .Include(d => d.AppSettings)
                .Where(d => d.Id == devideId)
                .FirstOrDefaultAsync();
        }

        [HttpPatch("")]
        public async Task<IActionResult> PatchAsync([FromRoute] int devideId, [FromBody] JsonPatchDocument<AppSettings> jsonPatch)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var device = await GetDevicesAsync(devideId);

            if (device is null)
            {
                return NotFound($"Could not find AppSettings for Device ID '{devideId}'");
            }

            jsonPatch.ApplyTo(device.AppSettings);

            device.AppSettings.SearchDirectories = device.AppSettings.SearchDirectories.Distinct().ToList();

            device.AppSettings.ReloadSettings = true;

            await _db.SaveChangesAsync();

            return Ok(device);
        }
    }
}
