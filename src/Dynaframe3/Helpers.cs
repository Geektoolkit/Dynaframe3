using Dynaframe3.Shared;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Dynaframe3
{
    public static class Helpers
    {
        // Fisher Yates shuffle - source: https://stackoverflow.com/questions/273313/randomize-a-listt/4262134#4262134
        // Modified on 3/11 based on A380Coding's feedback. Long lists aren't getting as well shuffled (thousands of images) towards the end with
        // the original algo.
        public static IList<T> Shuffle<T>(this IList<T> list, Random rnd)
        {
            for (var i = list.Count-1; i > 0; i--)
                list.Swap(i, rnd.Next(list.Count-1));
            return list;
        }

        public static void Swap<T>(this IList<T> list, int i, int j)
        {
            var temp = list[i];
            list[i] = list[j];
            list[j] = temp;
        }


        /// <summary>
        /// Gets the ip address of the system. Dynaframe uses this to help the user locate thier device
        /// on the network
        /// </summary>
        /// <returns></returns>
        public static string GetIPString()
        {
            string returnval = "";

            string version = Assembly.GetExecutingAssembly().GetName().Version.ToString();
            returnval += "(Version: " + version + ")\r\n";


            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                var ipStr = ip.ToString();
                if ((!ipStr.StartsWith("169.")) && (!ipStr.StartsWith("127.")) && (!ipStr.Contains("::")))
                {
                    returnval += "http://" + ipStr + ":8000 " + Environment.NewLine;
                }
            }
            return returnval;

        }

        /// <summary>
        /// Gets the ip address of the system (only IP, not whole string).
        /// </summary>
        /// <returns></returns>
        public static string GetIP()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    return ip.ToString();
                }
            }
            throw new Exception("No network adapters with an IPv4 address in the system!");

        }

        /// <summary>
        /// Runs a process and exits. If there is a failure or exception, returns false
        /// </summary>
        /// <param name="process">Process path and name</param>
        /// <param name="args">any arguments to pass</param>
        /// <returns>Process ID if it runs, else -1 if something fails</returns>

        public static Task RunProcessAsync(string patoToProcess, string args)
        {
            try
            {
                ProcessStartInfo info = new ProcessStartInfo(patoToProcess);
                if (!String.IsNullOrEmpty(args))
                {
                    info.Arguments = args;
                }
                Process process = new Process();
                process.StartInfo = info;

                var tcs = new TaskCompletionSource<object>();
                process.EnableRaisingEvents = true;
                process.Exited += (sender, args) => tcs.TrySetResult(null);

                process.Start();
                return process.HasExited ? Task.CompletedTask : tcs.Task;
            }
            catch (Exception)
            {
                return Task.CompletedTask;
            }
        }

        /// <summary>
        /// NYI - Future work to get the temp and give warnings, or to build a 'pi dashboard'
        /// </summary>
        public static void GetTemp()
        {
            // to get temp in C
            // vcgencmd measure_temp | egrep -o '[0-9]*\.[0-9]*'
            // 9/5 * C +32 = Farenheight
        }

        public static void DumpAppSettingsToLogger(AppSettings appSettings)
        {
            Logger.LogComment("Current App Settings");
            Logger.LogComment("FadeTransition: " + appSettings.FadeTransitionTime);
            Logger.LogComment("SlideshowTransitionTime: " + appSettings.SlideshowTransitionTime);
            Logger.LogComment("FontSize: " + appSettings.InfoBarFontSize);
            Logger.LogComment("FontFamily: " + appSettings.DateTimeFontFamily);
            Logger.LogComment("DateTimeFormat: " + appSettings.DateTimeFormat);
            Logger.LogComment("Rotation: " + appSettings.Rotation);
            Logger.LogComment("Shuffle: " + appSettings.Shuffle);
            Logger.LogComment("ImageStretch: " + appSettings.ImageStretch);
            Logger.LogComment("VideoStretch: " + appSettings.VideoStretch);
            Logger.LogComment("VideoVolume: " + appSettings.VideoVolume);
            Logger.LogComment("isSyncEnabled: " + appSettings.IsSyncEnabled);
            Logger.LogComment("Number of Sync Clients: " + appSettings.RemoteClients.Count);
        }



    }   
}
