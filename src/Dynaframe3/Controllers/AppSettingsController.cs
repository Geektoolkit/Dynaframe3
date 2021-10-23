using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.JsonPatch.Adapters;
using Microsoft.AspNetCore.JsonPatch.Operations;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net;

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

            try
            {
                jsonPatch.ApplyTo(ServerAppSettings.Default, new AppSettingsObjectAdapter(jsonPatch.ContractResolver));
            }
            catch (IOException ex)
            {
                return BadRequest(ex.Message);
            }

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

    public class AppSettingsObjectAdapter : IObjectAdapter
    {
        private IObjectAdapter _nestedAdapter;

        public AppSettingsObjectAdapter(IContractResolver contractResolver)
        {
            _nestedAdapter = new ObjectAdapter(contractResolver, null, new AdapterFactory());
        }

        public void Add(Operation operation, object objectToApplyTo)
        {
            if (objectToApplyTo is ServerAppSettings appSettings)
            {
                if (operation.path == $"/{nameof(ServerAppSettings.SearchDirectories)}/-"
                        && operation.value is string dir
                        && !Directory.Exists(dir))
                {
                    throw new IOException($"Directory '{dir}' does not exist");
                }

                if (operation.path == $"/{nameof(ServerAppSettings.RemoteClients)}/-")
                {
                    if (appSettings.RemoteClients.Contains(operation.value))
                    {
                        return;
                    }
                    else
                    {
                        SyncedFrame.SyncEngine.AddFrame((string)operation.value);
                    }
                }
            }
            _nestedAdapter.Add(operation, objectToApplyTo);
        }

        public void Copy(Operation operation, object objectToApplyTo)
        {
            _nestedAdapter.Copy(operation, objectToApplyTo);
        }

        public void Move(Operation operation, object objectToApplyTo)
        {
            _nestedAdapter.Move(operation, objectToApplyTo);
        }

        public void Remove(Operation operation, object objectToApplyTo)
        {
            if (objectToApplyTo is ServerAppSettings appSettings)
            {
                if (operation.path == $"/{nameof(ServerAppSettings.RemoteClients)}/-")
                {
                    var client = appSettings.RemoteClients[(int)operation.value];
                    var item = SyncedFrame.SyncEngine.syncedFrames.FirstOrDefault(s => s.hostname == client);
                    SyncedFrame.SyncEngine.syncedFrames.Remove(item);
                }
            }
            _nestedAdapter.Remove(operation, objectToApplyTo);
        }

        public void Replace(Operation operation, object objectToApplyTo)
        {
            if (operation.path == $"/{nameof(ServerAppSettings.SearchDirectories)}" && operation.value is JArray searchArray)
            {
                foreach (var item in searchArray.Select(s => s.Value<string>()))
                {
                    if (!Directory.Exists(item))
                    {
                        throw new IOException($"Directory '{item}' does not exist");
                    }
                }
            }

            if (objectToApplyTo is ServerAppSettings appSettings)
            {
                if (operation.path == $"/{nameof(ServerAppSettings.RemoteClients)}" && operation.value is JArray clientsArray)
                {
                    SyncedFrame.SyncEngine.syncedFrames.Clear();
                    foreach (var item in clientsArray)
                    {
                        SyncedFrame.SyncEngine.AddFrame(item.Value<string>()!);
                    }
                }
            }
            _nestedAdapter.Replace(operation, objectToApplyTo);
        }
    }
}
