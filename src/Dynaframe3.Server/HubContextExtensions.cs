using Dynaframe3.Server.SignalR;
using Dynaframe3.Shared.SignalR;
using Microsoft.AspNetCore.SignalR;

namespace Dynaframe3.Server
{
    internal static class HubContextExtensions
    {
        public static IFrameClient GetDevice(this IHubContext<DynaframeHub, IFrameClient> hub, int deviceId)
        {
            return hub.Clients.Group(deviceId.ToString());
        }
    }
}
