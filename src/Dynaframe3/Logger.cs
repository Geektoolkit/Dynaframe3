using System;
using System.Collections.Generic;
using Splat;

namespace Dynaframe3
{
    static public class Logger
    {
        public static List<string> memoryLog = new();
        static int MaxLogLength = 1000; // arbitrary for now

        static public void LogComment(string comment)
        {
            var deviceCache = Locator.Current.GetService<DeviceCache>();
            var settings = deviceCache.CurrentDevice.AppSettings;
            // Note: Appsettings determins if this actually logs or not. Defaults to 'off'.
            if (settings.EnableLogging)
            {
                string date = DateTime.Now.ToString("g");
                string logComment = date + ":" + comment;

                Locator.Current.GetService<Serilog.ILogger>().Information(logComment);
                memoryLog.Add(logComment);
                if (memoryLog.Count > 1000)
                {
                    memoryLog.RemoveRange(0, memoryLog.Count - MaxLogLength);
                }
            }

        }
        /// <summary>
        /// Returns the log in a webpage friendly format. We can add color syntax in the future using this.
        /// </summary>
        /// <returns></returns>
        public static string GetLogAsHTML()
        {
            var deviceCache = Locator.Current.GetService<DeviceCache>();
            var settings = deviceCache.CurrentDevice.AppSettings;
            // if disabled help the user out
            if (!settings.EnableLogging)
            {
                return "Logging is currently disabled! Please enable logging to continue...";
            }
            
            string returnVal = "";
            for (int i = memoryLog.Count - 1; i > 0; i--)
            {
                returnVal += memoryLog[i] + "\r\n";
            }
            return returnVal;
        }
    }
}
