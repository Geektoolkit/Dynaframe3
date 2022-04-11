using Dynaframe3.Server.Data;
using Dynaframe3.Shared;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualBasic.FileIO;

namespace Dynaframe3.Server.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("v{version:apiVersion}/Devices")]
    public class DevicesController : Controller
    {
        private readonly ServerDbContext _db;
        private readonly HttpClient _httpClient;
        private readonly ILogger<DevicesController> _logger;

        public DevicesController(ServerDbContext db, HttpClient httpClient, ILogger<DevicesController> logger)
        {
            _db = db;
            _httpClient = httpClient;
            _logger = logger;
        }

        [HttpGet]
        public IActionResult Index()
        {
            var devices = _db.Devices.AsNoTracking().AsAsyncEnumerable();
            return Ok(devices);
        }

        [HttpGet("{deviceId}")]
        public async Task<IActionResult> GetDeviceAsync([FromRoute] int deviceId, CancellationToken cancellationToken)
        {
            var device = await _db.Devices
                .Include(d => d.AppSettings)
                .FirstOrDefaultAsync(d => d.Id == deviceId);

            if (device is null)
            {
                return NotFound($"No device found for id {deviceId}");
            }
            await device.ResetSubDirectoriesAsync(_httpClient, _logger, cancellationToken);
            return Ok(device);
        }

        [HttpPut]
        public async Task<IActionResult> UpsertAsync([FromBody] Device upsert, CancellationToken cancellationToken)
        {
            var device = await _db.Devices
                .Include(d => d.AppSettings)
                .FirstOrDefaultAsync(d => d.HostName == upsert.HostName);

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (device is null)
            {
                upsert.LastCheckin = DateTimeOffset.Now;
                _db.Devices.Add(upsert);
                await _db.SaveChangesAsync(cancellationToken);
                await upsert.ResetSubDirectoriesAsync(_httpClient, _logger, cancellationToken);
                return CreatedAtAction(nameof(GetDeviceAsync).Replace("Async", ""), 
                    new { deviceId = upsert.Id }, upsert);
            }
            else
            {
                device.Ip = upsert.Ip;
                device.Port = upsert.Port;
                device.LastCheckin = DateTimeOffset.Now;
                await _db.SaveChangesAsync(cancellationToken);
                await device.ResetSubDirectoriesAsync(_httpClient, _logger, cancellationToken);
                return Ok(device);
            }
        }
    }
}
