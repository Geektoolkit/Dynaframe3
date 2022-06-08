using Dynaframe3.Server.Data;
using Dynaframe3.Shared;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;

namespace Dynaframe3.Server.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("v{version:apiVersion}/Devices/{deviceId}/Uploads")]
    public class UploadsController : Controller
    {
        private readonly ServerDbContext _db;
        private readonly HttpClient _httpClient;

        public UploadsController(ServerDbContext db, HttpClient httpClient)
        {
            _db = db;
            _httpClient = httpClient;
        }

        [HttpPost("")]
        public async Task<IActionResult> UploadFileAsync([FromForm]IFormFile file, [FromRoute]int deviceId, CancellationToken cancellationToken)
        {
            return await WithDeviceAsync(async device =>
            {
                var formContent = new MultipartFormDataContent(Guid.NewGuid().ToString());
                var fileContent = new StreamContent(file.OpenReadStream());
                formContent.Add(fileContent, "file", file.FileName);

                var response = await _httpClient.PostAsync($"{device.HostName}/v1.0/Uploads", formContent, cancellationToken);

                response.EnsureSuccessStatusCode();

                var fileName = Path.GetFileName(response.Headers.Location!.ToString());

                return base.Created(GetFileUrl(fileName, deviceId),
                                fileName);
            }, deviceId, cancellationToken);
        }

        private string GetFileUrl(string fileName, int deviceId)
        {
            // Url.Action fails if Async or Controller are in the parameters
            return Url.Action(nameof(GetFileAsync).Replace("Async", ""),
                                    nameof(UploadsController).Replace("Controller", ""),
                                    new { fileName, deviceId },
                                    Request.Scheme)!;
        }

        [HttpGet("")]
        public async Task<IActionResult> GetFilesAsync([FromRoute] int deviceId, CancellationToken cancellationToken)
        {
            return await WithDeviceAsync(async device =>
            {
                var files = await _httpClient.GetFromJsonAsync<IAsyncEnumerable<string>>($"{device.HostName}/v1.0/Uploads", cancellationToken);

                var fileUrls = files!.Select(f => GetFileUrl(f, deviceId)).WithCancellation(cancellationToken);
                return Ok(fileUrls);
            }, deviceId, cancellationToken);
        }

        [HttpGet("{fileName}")]
        public async Task<IActionResult> GetFileAsync([FromRoute] int deviceId, [FromRoute] string fileName, CancellationToken cancellationToken)
        {
            return await WithDeviceAsync(async device =>
            {
                var resp = await _httpClient.GetAsync($"{device.HostName}/v1.0/Uploads/{fileName}", cancellationToken);

                resp.EnsureSuccessStatusCode();

                return File(await resp.Content.ReadAsStreamAsync(cancellationToken), resp.Content.Headers.ContentType!.MediaType!);
            }, deviceId, cancellationToken);
        }

        [HttpDelete("{fileName}")]
        public async Task<IActionResult> DeleteFileAsync([FromRoute] string fileName, [FromRoute] int deviceId, CancellationToken cancellationToken)
        {
            return await WithDeviceAsync(async device =>
            {
                var resp = await _httpClient.DeleteAsync($"{device.HostName}/v1.0/Uploads/{fileName}", cancellationToken);

                resp.EnsureSuccessStatusCode();

                return Ok();
            }, deviceId, cancellationToken);
        }

        private async Task<IActionResult> WithDeviceAsync(Func<Device, Task<IActionResult>> func, int deviceId, CancellationToken cancellationToken)
        {
            var device = await GetDeviceAsync(deviceId, cancellationToken);

            if (device is null)
            {
                return NotFound($"No device found for Device Id '{deviceId}'");
            }
            return await func(device);
        }

        private async Task<Device?> GetDeviceAsync(int deviceId, CancellationToken cancellationToken)
            => await _db.Devices.AsNoTracking()
                                    .Where(d => d.Id == deviceId)
                                    .FirstOrDefaultAsync(cancellationToken);
    }
}
