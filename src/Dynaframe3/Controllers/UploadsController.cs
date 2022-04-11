using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Dynaframe3.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("v{version:apiVersion}/Uploads")]
    public class UploadsController : Controller
    {
        static readonly string _uploadsDirectory = AppDomain.CurrentDomain.BaseDirectory + "/wwwroot/uploads/";

        [HttpPost("")]
        public async Task<IActionResult> UploadFileAsync([FromForm]IFormFile file)
        {
            var fileName = "image_" + DateTime.Now.ToString("ddMMyyhhmmss") + Path.GetExtension(file.FileName);
            var dirPath = _uploadsDirectory + fileName;

            using (var output = new FileStream(dirPath, FileMode.Create, FileAccess.Write))
            {
                await file.OpenReadStream().CopyToAsync(output).ConfigureAwait(false);
            }

            return base.Created(GetFileUrl(fileName),
                            fileName);
        }

        private string GetFileUrl(string fileName)
        {
            return Url.Action(nameof(GetFile),
                                    nameof(UploadsController).Replace("Controller", ""),
                                    new { fileName },
                                    Request.Scheme);
        }

        [HttpGet("")]
        public IActionResult GetFiles()
        {
            var files = Directory.GetFiles(_uploadsDirectory).Select(f => Path.GetFileName(f));
            var fileUrls = files.Select(f => GetFileUrl(f));
            return Ok(fileUrls);
        }

        [HttpGet("{fileName}")]
        public IActionResult GetFile([FromRoute] string fileName)
        {
            var provider = new FileExtensionContentTypeProvider();
            if (!provider.TryGetContentType(fileName, out var contentType))
            {
                contentType = "application/octet-stream";
            }
            var dirPath = _uploadsDirectory + fileName;
            var stream = System.IO.File.OpenRead(dirPath);
            return File(stream, contentType);
        }

        [HttpDelete("{fileName}")]
        public IActionResult DeleteFile([FromRoute] string fileName)
        {
            var dirPath = _uploadsDirectory + fileName;
            System.IO.File.Delete(dirPath);
            return Ok();
        }
    }
}
