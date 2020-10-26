// MIT License - Copyright (c) 2016 Can Güney Aksakalli
// https://aksakalli.github.io/2014/02/24/simple-http-server-with-csparp.html

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Net;
using System.IO;
using System.Threading;
using System.Diagnostics;
using Microsoft.VisualBasic.FileIO;
using Dynaframe3;
using System.Runtime.InteropServices;

class SimpleHTTPServer
{
    private readonly string[] _indexFiles = {
        "index.html",
        "index.htm",
        "default.html",
        "default.htm"
    };

    private static IDictionary<string, string> _mimeTypeMappings = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase) {
        #region extension to MIME type list
        {".asf", "video/x-ms-asf"},
        {".asx", "video/x-ms-asf"},
        {".avi", "video/x-msvideo"},
        {".bin", "application/octet-stream"},
        {".cco", "application/x-cocoa"},
        {".crt", "application/x-x509-ca-cert"},
        {".css", "text/css"},
        {".deb", "application/octet-stream"},
        {".der", "application/x-x509-ca-cert"},
        {".dll", "application/octet-stream"},
        {".dmg", "application/octet-stream"},
        {".ear", "application/java-archive"},
        {".eot", "application/octet-stream"},
        {".exe", "application/octet-stream"},
        {".flv", "video/x-flv"},
        {".gif", "image/gif"},
        {".hqx", "application/mac-binhex40"},
        {".htc", "text/x-component"},
        {".htm", "text/html"},
        {".html", "text/html"},
        {".ico", "image/x-icon"},
        {".img", "application/octet-stream"},
        {".iso", "application/octet-stream"},
        {".jar", "application/java-archive"},
        {".jardiff", "application/x-java-archive-diff"},
        {".jng", "image/x-jng"},
        {".jnlp", "application/x-java-jnlp-file"},
        {".jpeg", "image/jpeg"},
        {".jpg", "image/jpeg"},
        {".js", "application/x-javascript"},
        {".mml", "text/mathml"},
        {".mng", "video/x-mng"},
        {".mov", "video/quicktime"},
        {".mp3", "audio/mpeg"},
        {".mpeg", "video/mpeg"},
        {".mpg", "video/mpeg"},
        {".msi", "application/octet-stream"},
        {".msm", "application/octet-stream"},
        {".msp", "application/octet-stream"},
        {".pdb", "application/x-pilot"},
        {".pdf", "application/pdf"},
        {".pem", "application/x-x509-ca-cert"},
        {".pl", "application/x-perl"},
        {".pm", "application/x-perl"},
        {".png", "image/png"},
        {".prc", "application/x-pilot"},
        {".ra", "audio/x-realaudio"},
        {".rar", "application/x-rar-compressed"},
        {".rpm", "application/x-redhat-package-manager"},
        {".rss", "text/xml"},
        {".run", "application/x-makeself"},
        {".sea", "application/x-sea"},
        {".shtml", "text/html"},
        {".sit", "application/x-stuffit"},
        {".swf", "application/x-shockwave-flash"},
        {".tcl", "application/x-tcl"},
        {".tk", "application/x-tcl"},
        {".txt", "text/plain"},
        {".war", "application/java-archive"},
        {".wbmp", "image/vnd.wap.wbmp"},
        {".wmv", "video/x-ms-wmv"},
        {".xml", "text/xml"},
        {".xpi", "application/x-xpinstall"},
        {".zip", "application/zip"},
        #endregion
    };
    private Thread _serverThread;
    private string _rootDirectory;
    private HttpListener _listener;
    private int _port;
    private CancellationTokenSource cts;

    public int Port
    {
        get { return _port; }
        private set { }
    }

    /// <summary>
    /// Construct server with given port.
    /// </summary>
    /// <param name="path">Directory path to serve.</param>
    /// <param name="port">Port of the server.</param>
    public SimpleHTTPServer(string path, int port)
    {
        cts = new CancellationTokenSource();
        this.Initialize(path, port);
    }

    /// <summary>
    /// Construct server with suitable port.
    /// </summary>
    /// <param name="path">Directory path to serve.</param>
    public SimpleHTTPServer(string path)
    {
        //get an empty port
        TcpListener l = new TcpListener(IPAddress.Loopback, 0);
        l.Start();
        int port = ((IPEndPoint)l.LocalEndpoint).Port;
        l.Stop();
        cts = new CancellationTokenSource();
        this.Initialize(path, port);
    }

    /// <summary>
    /// Stop server and dispose all functions.
    /// Note: This currently doesn't kill the listener thread
    /// despite the cancellation token. This means that the app doesn't
    /// exit gracefully. I call thread.abort forcing an exception to terminate it.
    /// </summary>
    public void Stop()
    {
         cts.Cancel();
         _listener.Abort(); // try to forcefully shut down the listener
         cts.Dispose();
        _serverThread.Abort(); // PNE fires!
    }

    private void Listen(object obj)
    {
        CancellationToken ct = (CancellationToken)obj;
        _listener = new HttpListener();
        _listener.Prefixes.Add("http://*:" + _port.ToString() + "/");
        _listener.Start();

        while (!ct.IsCancellationRequested)
        {
            try
            {
                HttpListenerContext context = _listener.GetContext();
                Process(context);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Exception from httplistener: " + ex.ToString());
            }
        }
    }

    private void Process(HttpListenerContext context)
    {
        // TODO: Clean this up. Need a consistent way to read in values
        // and cleanly set settings. For now having this ugly is a good
        // tradeoff to let me learn how to do it better in the future

        if (context.Request.QueryString.Count > 0)
        {

            // Set in App Settings takes the querystring and the Appsettings.Default value name
            Helpers.SetIntAppSetting(context.Request.QueryString.Get("rotation"), "Rotation");
            Helpers.SetIntAppSetting(context.Request.QueryString.Get("infobarfontsize"), "InfoBarFontSize");
            Helpers.SetIntAppSetting(context.Request.QueryString.Get("slideshowduration"), "SlideshowTransitionTime");
            Helpers.SetIntAppSetting(context.Request.QueryString.Get("ipaddresstime"), "NumberOfSecondsToShowIP");
            Helpers.SetIntAppSetting(context.Request.QueryString.Get("transitiontime"), "FadeTransitionTime");

            Helpers.SetBoolAppSetting(context.Request.QueryString.Get("Shuffle"), "Shuffle");
            Helpers.SetBoolAppSetting(context.Request.QueryString.Get("clock"), "Clock");

            Helpers.SetStringAppSetting(context.Request.QueryString.Get("DateTimeFormat"), "DateTimeFormat");
            Helpers.SetStringAppSetting(context.Request.QueryString.Get("DateTimeFontFamily"), "DateTimeFontFamily");


            #region SpecialCasesWhichNeedCleanup
            // Process 'directory' if passed
            string? dir = context.Request.QueryString.Get("dir");
            if(dir != null)
            {
                string pictureDir = dir.Replace("'","");
                AppSettings.Default.CurrentDirectory = pictureDir;
                AppSettings.Default.ReloadSettings = true;
            }

            // see if 'rem' (remove) was passed
            string? rem = context.Request.QueryString.Get("rem");
            if (rem != null)
            {
                string removeDir = rem.Replace("'", "");
                AppSettings.Default.SearchDirectories.Remove(rem);
                AppSettings.Default.ReloadSettings = true;
            }

            string imagestretchVal = context.Request.QueryString.Get("imagescaling");
            if (imagestretchVal != null)
            {
                switch (imagestretchVal)
                {
                    case "Uniform":
                        {
                            AppSettings.Default.ImageStretch = Avalonia.Media.Stretch.Uniform;
                            break;
                        }
                    case "UniformToFill":
                        {
                            AppSettings.Default.ImageStretch = Avalonia.Media.Stretch.UniformToFill;
                            break;
                        }
                    case "Fill":
                        {
                            AppSettings.Default.ImageStretch = Avalonia.Media.Stretch.Fill;
                            break;
                        }
                    case "None":
                        {
                            AppSettings.Default.ImageStretch = Avalonia.Media.Stretch.None;
                            break;
                        }
                    default:
                        break;
                }
                
            }

            // Shutdown command. This helps raspberry pis shutdown gracefully
            // but is likely useful for all platforms since the mirror may not
            // have a keyboard.  In the future may hook this up to an IR remote
            if (context.Request.QueryString.Get("shutdown") == "oneminute")
            {
                // shutdown requested!
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    Helpers.RunProcess("shutdown", "");
                }

                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    Helpers.RunProcess("shutdown", "/t /60");
                }

            }


            if (context.Request.QueryString.Get("shutdown") == "tenseconds")
            {
                // shutdown requested!
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    System.Threading.Thread.Sleep(10000);
                    Helpers.RunProcess("shutdown", "now");
                }

                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    Helpers.RunProcess("shutdown", "/t /10");
                }

            }


            string directoryToAdd = context.Request.QueryString.Get("directoryAdd");
            if (directoryToAdd != null)
            {
                // We use search directories to add things like NAS drives and usb drives to the system.
                // This checks to make sure it exists when they're adding it, and that it isn't already in the list..
                if (Directory.Exists(directoryToAdd) && (!AppSettings.Default.SearchDirectories.Contains(directoryToAdd)))
                {
                    AppSettings.Default.SearchDirectories.Add(directoryToAdd);
                    // Cleanup. Somewhere this is getting doubled up sometimes still. It's a small list so this should be fast.
                    AppSettings.Default.SearchDirectories = AppSettings.Default.SearchDirectories.Distinct().ToList();
                }
            }

            string subdirectorytoadd = context.Request.QueryString.Get("cbDirectory");
            if (subdirectorytoadd != null)
            {
                AppSettings.Default.CurrentPlayList.Clear();
                // We use search directories to add things like NAS drives and usb drives to the system.
                // This checks to make sure it exists when they're adding it, and that it isn't already in the list..
                string[] directoryList = subdirectorytoadd.Split(',');
                foreach (string newdir in directoryList)
                {
                    string decodedDirectory = System.Web.HttpUtility.UrlDecode(newdir);
                    if (Directory.Exists(decodedDirectory) && (!AppSettings.Default.CurrentPlayList.Contains(decodedDirectory)))
                    {
                        AppSettings.Default.CurrentPlayList.Add(decodedDirectory);
                    }
                }
                AppSettings.Default.ReloadSettings = true;

            }
            #endregion
            AppSettings.Default.ReloadSettings = true;
            AppSettings.Default.Save();
        }




        // return the default page back:
        GetDefaultPage();

        string filename = context.Request.Url.AbsolutePath;
        filename = filename.Substring(1);

        if (string.IsNullOrEmpty(filename))
        {
            foreach (string indexFile in _indexFiles)
            {
                if (File.Exists(Path.Combine(_rootDirectory, indexFile)))
                {
                    filename = indexFile;
                    break;
                }
            }
        }


        filename = Path.Combine(_rootDirectory, filename);

        if (File.Exists(filename))
        {
            try
            {
                Stream input = new FileStream(filename, FileMode.Open);

                //Adding permanent http response headers
                string mime;
                context.Response.ContentType = _mimeTypeMappings.TryGetValue(Path.GetExtension(filename), out mime) ? mime : "application/octet-stream";
                context.Response.ContentLength64 = input.Length;
                context.Response.AddHeader("Date", DateTime.Now.ToString("r"));
                context.Response.AddHeader("Last-Modified", System.IO.File.GetLastWriteTime(filename).ToString("r"));

                byte[] buffer = new byte[1024 * 16];
                int nbytes;
                while ((nbytes = input.Read(buffer, 0, buffer.Length)) > 0)
                    context.Response.OutputStream.Write(buffer, 0, nbytes);
                input.Close();

                context.Response.StatusCode = (int)HttpStatusCode.OK;
                context.Response.OutputStream.Flush();
            }
            catch (Exception ex)
            {
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                Debug.WriteLine("Exception processing HTTPContext: " + ex.ToString());
            }

        }
        else
        {
            string response = GetDefaultPage();
            byte[] buffer = Encoding.ASCII.GetBytes(response);
            context.Response.OutputStream.Write(buffer, 0, buffer.Length);
            context.Response.OutputStream.Flush();
            context.Response.StatusCode = (int)HttpStatusCode.OK;
        }

        context.Response.OutputStream.Close();
    }

    private void Initialize(string path, int port)
    {


        this._rootDirectory = path;
        this._port = port;
        _serverThread = new Thread(new ParameterizedThreadStart(this.Listen));
        _serverThread.Start(cts.Token);
    }

    public string GetDefaultPage()
    {
        try
        {
            TextReader reader = File.OpenText("./web/WebTemplate.htm");
            string page = reader.ReadToEnd();


            // TODO: Break this out into a File/Folder management class
            // to handle this both in Dynaframe core and this engine
            // This goes through all folders in the settings, and gets
            // the first level directory folders to act as playlists

            string dirChoices = "<br>";
            dirChoices += "<div id='directories'><ul id='dirs' class='directorylist'><br>";
            foreach (string dir in AppSettings.Default.SearchDirectories)
            {
                // Top level directory:
                dirChoices += "<li class='topleveldirectory'>" + dir;

                string[] subdirectories = Directory.GetDirectories(dir);
                // TODO: Handle more than one subdirectory / add recursion
                // lets wait till we get this logic nailed down.
                dirChoices += "<ul>";
                foreach (string subdir in subdirectories)
                {
                    string subdirectory = Path.GetFileName(subdir);
                    string encdirectory = System.Web.HttpUtility.UrlEncode(subdir);
                    string cbChecked = "";
                    if (AppSettings.Default.CurrentPlayList.Contains(subdir))
                    {
                        cbChecked = "checked";
                    }

                    dirChoices += "<li class='subdirectory'><input type='checkbox' " +
                        cbChecked + " class='directorycb' id='cbDirectory' name='cbDirectory' value='" + 
                        encdirectory + "'>" + subdirectory + "</li>";
                }
                dirChoices += "</ul></li>";
            }

            dirChoices += "</ul></div>";

            dirChoices += "<br><br><br><div class ='settings'><h2>Search Directories: </h2>";
            foreach (string directory in AppSettings.Default.SearchDirectories)
            {
                dirChoices += directory + "&nbsp&nbsp&nbsp<a href=?rem=" + directory + " class='remove'>Remove</a><br>";
            }
            dirChoices += "</div><br>";

            page = page.Replace("<!-- DIRECTORIES -->", dirChoices);

            // Generate custom settings here
            page = page.Replace("<!--INFOBARFONTSIZE-->", "value=" + AppSettings.Default.InfoBarFontSize.ToString() + ">");
            page = page.Replace("<!--SLIDESHOWDURATION-->", "value=" + AppSettings.Default.SlideshowTransitionTime.ToString() + ">");
            page = page.Replace("<!--TRANSITIONTIME-->", "value=" + AppSettings.Default.FadeTransitionTime.ToString() + ">");
            page = page.Replace("<!--IPADDRESSTIME-->", "value=" + AppSettings.Default.NumberOfSecondsToShowIP.ToString() + ">");
            page = page.Replace("<!--DATETIMEFORMAT-->", "value='" + AppSettings.Default.DateTimeFormat + "'>");
            page = page.Replace("<!--DATETIMEFONTFAMILY-->", "value='" + AppSettings.Default.DateTimeFontFamily + "'>");
            return page;
        }
        catch (Exception exc)
        { 
            // If anything happens, we have to return some data so the user knows what is going on)
            return "Fatal Error! Excpetion occurred.  Info: " + exc.ToString();
        }

    }
}