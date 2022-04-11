using Dynaframe3.Shared.SignalR;
using Microsoft.AspNetCore.SignalR;

namespace Dynaframe3.Server.SignalR
{
    public class DynaframeHub : Hub<IFrameClient>
    {
        public async Task ConnectDeviceAsync(int deviceId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, deviceId.ToString(), Context.ConnectionAborted);
        }

        public async Task DisconnectDeviceAsync(int deviceId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, deviceId.ToString(), Context.ConnectionAborted);
        }
    }
}
