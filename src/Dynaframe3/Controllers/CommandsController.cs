using Microsoft.AspNetCore.Mvc;

namespace Dynaframe3.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("v{version:apiVersion}/Commands")]
    public class CommandsController
    {
        [HttpPost("SETFILE")]
        public void SetFile([FromBody] FileBody body)
            => CommandProcessor.ProcessSetFile(body.File);

        [HttpPost("{command}")]
        public void Execute([FromRoute] string command)
            => CommandProcessor.ProcessCommand(command);
    }

    public class FileBody
    {
        public string File { get; set; }
    }
}
