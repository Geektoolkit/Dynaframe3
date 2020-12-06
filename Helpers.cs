using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Text;

namespace Dynaframe3
{
    public static class Helpers
    {
        // Fisher Yates shuffle - source: https://stackoverflow.com/questions/273313/randomize-a-listt/4262134#4262134
        public static IList<T> Shuffle<T>(this IList<T> list, Random rnd)
        {
            for (var i = list.Count; i > 0; i--)
                list.Swap(0, rnd.Next(0, i));
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

            NetworkInterface[] nets = System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces();
            if (nets.Length > 0)
            {
                foreach (NetworkInterface net in nets)
                {
                    try
                    {
                        var addresses = net.GetIPProperties().UnicastAddresses;
 

                        for (int i = 0; i < addresses.Count; i++)
                        {
                            string ip = addresses[i].Address.ToString();
                            // Filter out IPV6, local, loopback, etc.
                            

                            if ((!ip.StartsWith("169.")) && (!ip.StartsWith("127.")) && (!ip.Contains("::")))
                                returnval += "http://" + ip + ":8000 "+ Environment.NewLine;
                        }
                    }
                    catch (Exception exc)
                    {
                        Logger.LogComment("Exception in GetIPString() : " + exc.ToString());
                    }
                }
            }
            return returnval;

        }

        /// <summary>
        /// Runs a process and exits. If there is a failure or exception, returns false
        /// </summary>
        /// <param name="process">Process path and name</param>
        /// <param name="args">any arguments to pass</param>
        /// <returns>Process ID if it runs, else -1 if something fails</returns>

        public static int RunProcess(string patoToProcess, string args)
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
                process.Start();
                return process.Id;
            }
            catch (Exception)
            {
                return -1;
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

        /// <summary>
        /// Parses out a value as an int and sets it based on the query string. 
        /// </summary>
        /// <param name="querystring"></param>
        /// <param name="property"></param>
        /// <returns>0 if a value was not found, 1 if a value was set</returns>
        public static int SetIntAppSetting(string querystring, string property)
        {
            if (querystring != null)
            {
                int i = 0;
                if (int.TryParse(querystring, out i))
                {
                    typeof(AppSettings).GetProperty(property).SetValue(AppSettings.Default, i);
                }
                return 1;
            }
            return 0;
        }

        /// <summary>
        /// Takes in a string value and returns a 0/1 based on if a setting was found
        /// </summary>
        /// <param name="querystring"></param>
        /// <param name="property"></param>
        /// <returns>0 if no setting was changed, 1 if it was</returns>
        public static int SetStringAppSetting(string querystring, string property)
        {
            if (querystring != null)
            {
               typeof(AppSettings).GetProperty(property).SetValue(AppSettings.Default, querystring);
                return 1;
            }
            return 0;
        }

        /// <summary>
        /// Sets a boolean value based on the query string. 
        /// </summary>
        /// <param name="querystring"></param>
        /// <param name="property"></param>
        /// <returns>0 if no setting was passed, 1 if it was.</returns>
        public static int SetBoolAppSetting(string querystring, string property)
        {
            if (querystring != null)
            {
                if (querystring.ToUpper() == "ON")
                {
                    typeof(AppSettings).GetProperty(property).SetValue(AppSettings.Default, true);
                }
                else
                {
                    typeof(AppSettings).GetProperty(property).SetValue(AppSettings.Default, false);
                }
                return 1;
            }
            return 0;
        }

        public static void DumpAppSettingsToLogger()
        {
            Logger.LogComment("Current App Settings");
            Logger.LogComment("FadeTransition: " + AppSettings.Default.FadeTransitionTime);
            Logger.LogComment("SlideshowTransitionTime: " + AppSettings.Default.SlideshowTransitionTime);
            Logger.LogComment("FontSize: " + AppSettings.Default.InfoBarFontSize);
            Logger.LogComment("FontFamily: " + AppSettings.Default.DateTimeFontFamily);
            Logger.LogComment("DateTimeFormat: " + AppSettings.Default.DateTimeFormat);
            Logger.LogComment("Rotation: " + AppSettings.Default.Rotation);
            Logger.LogComment("Shuffle: " + AppSettings.Default.Shuffle);
            Logger.LogComment("ImageStretch: " + AppSettings.Default.ImageStretch);
            Logger.LogComment("VideoStretch: " + AppSettings.Default.VideoStretch);
            Logger.LogComment("VideoVolume: " + AppSettings.Default.VideoVolume);
            Logger.LogComment("isSyncEnabled: " + AppSettings.Default.IsSyncEnabled);
            Logger.LogComment("Number of Sync Clients: " + AppSettings.Default.RemoteClients.Count);
        }



    }   
}
