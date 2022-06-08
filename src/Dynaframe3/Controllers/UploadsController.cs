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
        [HttpPost("")]
        public async Task<IActionResult> UploadFileAsync([FromForm]IFormFile file)
        {
            var fileName = await CommandProcessor.SaveFileAsync(file.OpenReadStream(), Path.GetExtension(file.FileName));

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
            var files = CommandProcessor.GetFiles();
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
            return File(CommandProcessor.GetFile(fileName), contentType);
        }

        [HttpDelete("{fileName}")]
        public IActionResult DeleteFile([FromRoute] string fileName)
        {
            CommandProcessor.DeleteFile(fileName);
            return Ok();
        }
    }
}
