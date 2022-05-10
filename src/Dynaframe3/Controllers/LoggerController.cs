using Microsoft.AspNetCore.Mvc;

namespace Dynaframe3.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("v{version:apiVersion}/Logs")]
    public class LoggerController : Controller
    {
        [HttpGet("")]
        public IActionResult GetLogs()
        {
            return Ok(Logger.GetLogAsHTML());
        }
    }
}
