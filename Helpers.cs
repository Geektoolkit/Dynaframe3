using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
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
        /// Source: https://stackoverflow.com/questions/3527203/getfiles-with-multiple-extensions
        /// </summary>
        /// <param name="dirInfo"></param>
        /// <param name="extensions"></param>
        /// <returns></returns>
        public static IEnumerable<string> GetFilesByExtensions(this DirectoryInfo dirInfo, params string[] extensions)
        {
            var allowedExtensions = new HashSet<string>(extensions, StringComparer.OrdinalIgnoreCase);

            return dirInfo.EnumerateFiles("*.*", SearchOption.AllDirectories)
                          .Where(f => allowedExtensions.Contains(f.Extension)).Select(s => s.FullName);
        }


        public static string GetIPString()
        {
            string host = System.Net.Dns.GetHostName();
            string ip = "";
            foreach (IPAddress address in Dns.GetHostByName(host).AddressList)
            {
                ip = address.ToString();
                Console.WriteLine("IP found: " + ip);
                if ((address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork) && (!ip.StartsWith("127.")))
                    break;

            }
            return "Host: " + host + "   IP: " + ip;

        }

    }
}
