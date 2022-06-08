using Dynaframe3.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;

namespace Dynaframe3
{
    public struct SyncedFrame
    {
        public string hostname { get; set; }
        public int offsetdelay
        {
            get; set;
        }

        public static class SyncEngine
        {
            public static List<SyncedFrame> syncedFrames = new List<SyncedFrame>();
            public static HttpClient client = new HttpClient();

            public static void Initialize(AppSettings appSettings)
            {
                foreach (string ip in appSettings.RemoteClients)
                {
                    SyncedFrame syncFrame = new SyncedFrame() { hostname = ip, offsetdelay = 0 };
                    syncedFrames.Add(syncFrame);
                }
            }

            public static void SyncSettiings(AppSettings newSettings)
            {
                foreach (var newClient in newSettings.RemoteClients.Where(c => !syncedFrames.Any(s => s.hostname == c)).ToList())
                {
                    AddFrame(newClient);
                }

                foreach (var clientToRemove in syncedFrames.Where(s => !newSettings.RemoteClients.Any(c => c == s.hostname)).ToList())
                {
                    syncedFrames.Remove(clientToRemove);
                }
            }

            public static void AddFrame(string frame, int delay = 0)
            {
                SyncedFrame syncFrame = new SyncedFrame() { hostname = frame, offsetdelay = delay };
                if (!syncedFrames.Contains(syncFrame))
                {
                    syncedFrames.Add(syncFrame);
                }
            }

            //TODO: Control on server
            public static async Task SyncFramesAsync(AppSettings appSettings, string imageUrl)
            {
                Logger.LogComment("SyncFrames enabled...sending sync signals..");
                await Parallel.ForEachAsync(syncedFrames, async (frame, cancellationToken) =>
                {
                    string host = "http://" + frame.hostname + ":" + appSettings.ListenerPort;
                    string command = "SETFILE";
                    string uri = host + command;
                    Logger.LogComment("SyncEngine - SyncFrames: Sending: " + uri);

                    try
                    {
                        // TODO: We should move commands like this to return automation friendly return values
                        // such as 200OK or even JSON to give status back.  
                        // This sends the request to the other frames.

                        var resp = await client.PostAsJsonAsync(uri, new { File = imageUrl }, cancellationToken).ConfigureAwait(false);

                        resp.EnsureSuccessStatusCode();
                    }
                    catch (System.UriFormatException exc)
                    {
                        // TODO: We should clear this uri out automatically, but we can't modify
                        // the syncedFames enum here.
                        Logger.LogComment("SyncFrames: Bad URI in Sync list! " + exc.ToString());
                        try
                        {
                            // We have a bad hostame in the list...a bad uri will neverwork
                            // and so lets try to clean it out to keep things from going sideways..
                            appSettings.RemoteClients.Remove(frame.hostname);
                        }
                        catch (Exception)
                        {
                            // at least we tried...
                            Logger.LogComment("Unable to self-heal from bad hostname :(");
                        }

                    }
                    catch (HttpRequestException e)
                    {
                        Logger.LogComment("SyncFrames: Exception Sending HTTP Message! " + e.ToString());
                    }
                    catch (Exception except)
                    {
                        Logger.LogComment("SyncFrames: Unknown Exception when sendng message! " + except.ToString());
                    }
                });
            }
        }
    }
}
