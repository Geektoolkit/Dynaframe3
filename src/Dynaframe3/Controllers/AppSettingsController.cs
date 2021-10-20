using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.JsonPatch.Adapters;
using Microsoft.AspNetCore.JsonPatch.Operations;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Serialization;
using System.IO;
using System.Linq;

namespace Dynaframe3.Controllers
{
    [ApiController]
    [Route("AppSettings")]
    public class AppSettingsController : Controller
    {
        [HttpGet("")]
        public IActionResult Get()
        {
            ResetSubDirectories();
            return Ok(ServerAppSettings.Default);
        }

        [HttpPatch("")]
        public IActionResult Patch([FromBody] JsonPatchDocument<ServerAppSettings> jsonPatch)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            foreach (var operation in jsonPatch.Operations)
            {
                if (operation.OperationType == OperationType.Add
                    && operation.path == $"/{nameof(ServerAppSettings.SearchDirectories)}/-"
                    && operation.value is string dir
                    && !Directory.Exists(dir))
                {
                    return BadRequest("Directory does not exist");
                }
            }

            jsonPatch.ApplyTo(ServerAppSettings.Default);
            ServerAppSettings.Default.SearchDirectories = ServerAppSettings.Default.SearchDirectories.Distinct().ToList();

            ServerAppSettings.Default.ReloadSettings = true;
            ServerAppSettings.Default.Save();

            ResetSubDirectories();
            return Ok(ServerAppSettings.Default);
        }

        private static void ResetSubDirectories()
        {
            ServerAppSettings.Default.SearchSubDirectories = new();
            foreach (var dir in ServerAppSettings.Default.SearchDirectories)
            {
                ServerAppSettings.Default.SearchSubDirectories[dir] = Directory.GetDirectories(dir).ToList();
            }
        }
    }
}
