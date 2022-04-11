using Microsoft.AspNetCore.Mvc;

namespace Dynaframe3.Server.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("v{version:apiVersion}/Devices/{deviceId}/Commands")]
    public class CommandsController
    {
        private readonly CommandProcessor _commandProcessor;

        public CommandsController(CommandProcessor commandProcessor)
        {
            _commandProcessor = commandProcessor;
        }

        [HttpPost("SETFILE")]
        public async Task SetFileAsync([FromBody] FileBody body, [FromRoute] int deviceId)
            => await _commandProcessor.ProcessSetFile(body.File, deviceId);

        [HttpPost("{command}")]
        public async Task ExecuteAsync([FromRoute] string command, [FromRoute] int deviceId)
            => await _commandProcessor.ProcessCommandAsync(command, deviceId);
    }

    public class FileBody
    {
        public string File { get; set; } = "";
    }
}
