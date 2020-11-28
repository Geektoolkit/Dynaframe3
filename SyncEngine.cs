using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
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

            public static void Initialize()
            {
                foreach (string ip in AppSettings.Default.RemoteClients)
                {
                    SyncedFrame syncFrame = new SyncedFrame() { hostname = ip, offsetdelay = 0 };
                    syncedFrames.Add(syncFrame);
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

            public static void SyncFrames(string imageUrl)
            {
                int i = 0;
                Parallel.For(0, syncedFrames.Count, i =>
                {
                    string host = "http://" + syncedFrames[i].hostname + ":" + AppSettings.Default.ListenerPort;
                    string command = "?SETFILE=" + imageUrl;
                    string uri = host + command;
                    Logger.LogComment("SyncEngine - SyncFrames: Sending: " + uri);

                    try
                    {
                        // TODO: We should move commands like this to return automation friendly return values
                        // such as 200OK or even JSON to give status back.  
                        // This sends the request to the other frames.

                        client.GetStringAsync(uri);
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
                            AppSettings.Default.RemoteClients.Remove(syncedFrames[i].hostname);
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
