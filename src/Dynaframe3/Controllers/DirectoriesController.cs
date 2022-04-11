using Microsoft.AspNetCore.Mvc;
using System.IO;

namespace Dynaframe3.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("v{version:apiVersion}/Directories")]
    public class DirectoriesController : Controller
    {
        [HttpGet("Subdirectories")]
        public IActionResult GetSubdirectories([FromQuery]string directory)
        {
            if (Directory.Exists(directory))
            {
                return Ok(Directory.GetDirectories(directory));
            }
            return NotFound($"Directory '{directory}' not found");
        }
    }
}
