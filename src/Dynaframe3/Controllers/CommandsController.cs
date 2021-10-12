using Microsoft.AspNetCore.Mvc;

namespace Dynaframe3.Controllers
{
    [ApiController]
    [Route("Commands")]
    public class CommandsController
    {
        [HttpPost("{command}")]
        public void Execute([FromRoute] string command)
            => CommandProcessor.ProcessCommand(command);
    }
}
