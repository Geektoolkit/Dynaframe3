using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Dynaframe3.Controllers
{
    [ApiController]
    [Route("AppSettings")]
    public class AppSettingsController : Controller
    {
        [HttpGet("")]
        public IActionResult Get()
            => Ok(ServerAppSettings.Default);

        [HttpPatch("")]
        public IActionResult Patch([FromBody]JsonPatchDocument<ServerAppSettings> jsonPatch)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            jsonPatch.ApplyTo(ServerAppSettings.Default);
            ServerAppSettings.Default.Save();
            return Ok(ServerAppSettings.Default);
        }
    }
}
