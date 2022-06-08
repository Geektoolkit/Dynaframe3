using System.Collections.Specialized;
using System.Diagnostics;

namespace Dynaframe3.Server
{
    internal static class Logger
    {

        public static List<string> memoryLog = new List<string>();
        static int MaxLogLength = 1000; // arbitrary for now

        static public void LogComment(string comment)
        {
            // Note: Appsettings determins if this actually logs or not. Defaults to 'off'.
            //if (ServerAppSettings.Default.EnableLogging)
            //{
                string date = DateTime.Now.ToString("g");
                string logComment = date + ":" + comment;

                Console.WriteLine(logComment);
                Debug.WriteLine(logComment);
                memoryLog.Add(logComment);
                if (memoryLog.Count > 1000)
                {
                    memoryLog.RemoveRange(0, memoryLog.Count - MaxLogLength);
                }
            //}

        }
        /// <summary>
        /// Returns the log in a webpage friendly format. We can add color syntax in the future using this.
        /// </summary>
        /// <returns></returns>
        public static string GetLogAsHTML()
        {
            // if disabled help the user out
            //if (!ServerAppSettings.Default.EnableLogging)
            //{
            //    return "Logging is currently disabled! Please enable logging to continue...";
            //}

            string returnVal = "";
            for (int i = memoryLog.Count - 1; i > 0; i--)
            {
                returnVal += memoryLog[i] + "\r\n";
            }
            return returnVal;
        }
    }
}
