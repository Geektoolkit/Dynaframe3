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
        // Track if we need to refresh the page. Set this to false
        bool ReloadSettings = false;
        bool ReloadDirectories = false;

        // TODO: Clean this up. Need a consistent way to read in values
        // and cleanly set settings. For now having this ugly is a good
        // tradeoff to let me learn how to do it better in the future

        if (context.Request.QueryString.Count > 0)
        {
            int refreshDirectories = 0; // track if a directory change is made. 0 = no change.
            int refreshSettings = 0;    // track i fa setting change is made. 0 = no change.

            // Set in App Settings takes the querystring and the Appsettings.Default value name
            refreshSettings += Helpers.SetIntAppSetting(context.Request.QueryString.Get("rotation"), "Rotation");
            refreshSettings += Helpers.SetIntAppSetting(context.Request.QueryString.Get("infobarfontsize"), "InfoBarFontSize");
            refreshSettings += Helpers.SetIntAppSetting(context.Request.QueryString.Get("slideshowduration"), "SlideshowTransitionTime");
            refreshSettings += Helpers.SetIntAppSetting(context.Request.QueryString.Get("ipaddresstime"), "NumberOfSecondsToShowIP");
            refreshSettings += Helpers.SetIntAppSetting(context.Request.QueryString.Get("transitiontime"), "FadeTransitionTime");


            refreshDirectories += Helpers.SetBoolAppSetting(context.Request.QueryString.Get("Shuffle"), "Shuffle");
            Helpers.SetBoolAppSetting(context.Request.QueryString.Get("VideoVolume"), "VideoVolume");
            Helpers.SetBoolAppSetting(context.Request.QueryString.Get("ExpandDirectoriesByDefault"), "ExpandDirectoriesByDefault");

            refreshSettings += Helpers.SetStringAppSetting(context.Request.QueryString.Get("DateTimeFormat"), "DateTimeFormat");
            refreshSettings += Helpers.SetStringAppSetting(context.Request.QueryString.Get("DateTimeFontFamily"), "DateTimeFontFamily");
            refreshSettings += Helpers.SetStringAppSetting(context.Request.QueryString.Get("VideoStretch"), "VideoStretch");

            
            if (context.Request.QueryString.Get("COMMAND") != null)
            {
                CommandProcessor.ProcessCommand(context.Request.QueryString.Get("COMMAND"));
                refreshSettings++; // Any commands should invoke a settings refresh
            }

            if (context.Request.QueryString.Get("SETFILE") != null)
            {
                CommandProcessor.ProcessSetFile(context.Request.QueryString.Get("SETFILE"));
            }

            #region SpecialCasesWhichNeedCleanup
            // Process 'directory' if passed
            string dir = context.Request.QueryString.Get("dir");
            if (dir != null)
            {
                string pictureDir = dir.Replace("'", "");
                AppSettings.Default.CurrentDirectory = pictureDir;
                refreshDirectories++;
            }

            // see if 'rem' (remove) was passed
            string rem = context.Request.QueryString.Get("rem");
            if (rem != null)
            {
                Logger.LogComment("Removing directory..." + rem);
                string removeDir = rem.Replace("'", "");
                AppSettings.Default.SearchDirectories.Remove(rem);

                List<string> ToRemove = new List<string>(); 

                // Go through and find items to remove. We can't modify the list 'in place' so make a target
                // list and then go through that. Awkward but too tired to figure out the right way to fix
                // this right now.
                foreach (string subdirectory in AppSettings.Default.CurrentPlayList)
                {
                    if (subdirectory.Contains(removeDir))
                    {
                        ToRemove.Add(subdirectory);
                    }
                }

                foreach (string del in ToRemove)
                {
                    if (AppSettings.Default.CurrentPlayList.Contains(del))
                    {
                        AppSettings.Default.CurrentPlayList.Remove(del);
                        Logger.LogComment("Removing subdirectory: " + del);
                    }
                }

                Logger.LogComment("New Playlist is as follows----------------");
                foreach (string playlistDir in AppSettings.Default.CurrentPlayList)
                {
                    Logger.LogComment(playlistDir);
                }
                Logger.LogComment("End Playlist dump -------------");
                refreshDirectories++;
            }

            string imagestretchVal = context.Request.QueryString.Get("imagescaling");
            if (imagestretchVal != null)
            {
                refreshSettings++;
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
                    refreshDirectories++;
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
                refreshDirectories++;

            }
            #endregion
            if (refreshDirectories > 0)
            {
                AppSettings.Default.RefreshDirctories = true;
            }
            if (refreshSettings > 0)
            {
                AppSettings.Default.ReloadSettings = true;
            }
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
           
            foreach (string dir in AppSettings.Default.SearchDirectories)
            {
                // Top level directory:
                dirChoices += "<ul id='dirs' class='directorylist'>";
                dirChoices += "<li class='topleveldirectory'>" + dir;

                string[] subdirectories = Directory.GetDirectories(dir);
                // TODO: Handle more than one subdirectory / add recursion
                // lets wait till we get this logic nailed down.
                dirChoices += "<ul " + dir.GetHashCode() + ">";
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

            dirChoices += "</li></div>";

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

            // Fill in 'current settings' here
            // Note: I am terrible at the compressed notation for if/else, but it is really clean here. If you need a primer, or for my
            // reference, this helps: https://www.csharp-console-examples.com/conditional/if-else-statement/c-if-else-shorthand-with-examples/
            // We store settings as boolean, but really sometimes they're better expressed as 'on/off'. The 'friendly' strings below are transaltions
            // from the internal way we store stuff to a friendly way to show the user the current setting.


            page = page.Replace("<!--ROTATIONCURRENTSETTING-->",                "(Current value: " + AppSettings.Default.Rotation.ToString() + " )");
            page = page.Replace("<!--IMAGESCALINGCURRENTSETTING-->",            "(Current value: " + AppSettings.Default.ImageStretch.ToString() + " )");

            string shufflefriendly = AppSettings.Default.Shuffle ? "On" : "Off";
            page = page.Replace("<!--SHUFFLECURRENTSETTING-->",                 "(Current value: " + shufflefriendly + " )");
            page = page.Replace("<!--VIDEOASPECTMODECURRENTSETTING-->",         "(Current value: " + AppSettings.Default.VideoStretch + " )");
            
            string videoplayasaudiofriendly = AppSettings.Default.VideoVolume ? "On" : "Off";
            page = page.Replace("<!--VIDEOPLAYAUDIOCURRENTSETTING-->",          "(Current value: " + videoplayasaudiofriendly + " )");
            
            string  expandcollaspedfriendly = AppSettings.Default.ExpandDirectoriesByDefault ? "Expanded" : "Toggleable";
            page = page.Replace("<!--TREEVIEWCURRENTSETTING-->",                "(Current value: " + expandcollaspedfriendly + " )");

            if (AppSettings.Default.ExpandDirectoriesByDefault)
            {
                page = page.Replace(@"//JSLISTOPENSETTING", "JSLists.openAll();");
            }

            return page;
        }
        catch (Exception exc)
        { 
            // If anything happens, we have to return some data so the user knows what is going on)
            return "Fatal Error! Excpetion occurred.  Info: " + exc.ToString();
        }

    }
}