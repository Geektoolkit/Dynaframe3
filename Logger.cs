using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

namespace Dynaframe3
{
    static public class Logger
    {
        static public void LogComment(string comment)
        {
            string date = DateTime.Now.ToString("g");
            string logComment = date + ":" + comment;

            Console.WriteLine(logComment);

            Debug.WriteLine(logComment);

        }
    }
}
