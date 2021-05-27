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
using System.Reflection;
using System.Web;

internal class SimpleHTTPServer
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
         //_serverThread.Abort(); // PNE fires!  // .net 5.0 this may no longer be necessary
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
        bool ImageUploaded = true;

        if (context.Request.HttpMethod == "POST")
        {
            string text;
            using (var reader = new StreamReader(context.Request.InputStream,
                                                 context.Request.ContentEncoding))
            {
                text = reader.ReadToEnd();
                if (!String.IsNullOrEmpty(text))
                {
                   string command = text.Split('=')[1];
                   CommandProcessor.ProcessCommand(command);
                }  
            }
        }

        if (context.Request.QueryString.Count > 0)
        {
            int refreshDirectories = 0; // track if a directory change is made. 0 = no change.
            int refreshSettings = 0;    // track i fa setting change is made. 0 = no change.

            if (context.Request.QueryString.Get("debug") != null)
            {
                
                return;
            }

            Logger.LogComment("*************************************************************");
            Logger.LogComment("SIMPLEHTTPSERVER: Incoming Web Request...");
            Logger.LogComment("SIMPLEHTTPSERVER: Checking query string for values to edit..");
            Logger.LogComment("Requested URL is: " + context.Request.RawUrl);
            Logger.LogComment("*************************************************************");
            // Set in App Settings takes the querystring and the Appsettings.Default value name
            refreshSettings += Helpers.SetIntAppSetting(context.Request.QueryString.Get("rotation"), "Rotation");
            refreshSettings += Helpers.SetIntAppSetting(context.Request.QueryString.Get("infobarfontsize"), "InfoBarFontSize");
            refreshSettings += Helpers.SetIntAppSetting(context.Request.QueryString.Get("slideshowduration"), "SlideshowTransitionTime");
            refreshSettings += Helpers.SetIntAppSetting(context.Request.QueryString.Get("ipaddresstime"), "NumberOfSecondsToShowIP");
            refreshSettings += Helpers.SetIntAppSetting(context.Request.QueryString.Get("transitiontime"), "FadeTransitionTime");

            // Tag Settings
            Helpers.SetStringAppSetting(context.Request.QueryString.Get("InclusiveTagFilters"), "InclusiveTagFilters");

            refreshDirectories += Helpers.SetBoolAppSetting(context.Request.QueryString.Get("Shuffle"), "Shuffle");
            Helpers.SetBoolAppSetting(context.Request.QueryString.Get("syncenabled"), "IsSyncEnabled");

            Helpers.SetBoolAppSetting(context.Request.QueryString.Get("VideoVolume"), "VideoVolume");
            Helpers.SetBoolAppSetting(context.Request.QueryString.Get("ExpandDirectoriesByDefault"), "ExpandDirectoriesByDefault");
            Helpers.SetBoolAppSetting(context.Request.QueryString.Get("EnableLogging"), "EnableLogging");
            Helpers.SetBoolAppSetting(context.Request.QueryString.Get("PlaybackFullVideo"), "PlaybackFullVideo");

            refreshSettings += Helpers.SetStringAppSetting(context.Request.QueryString.Get("DateTimeFormat"), "DateTimeFormat");
            refreshSettings += Helpers.SetStringAppSetting(context.Request.QueryString.Get("DateTimeFontFamily"), "DateTimeFontFamily");
            refreshSettings += Helpers.SetStringAppSetting(context.Request.QueryString.Get("VideoStretch"), "VideoStretch");
            refreshSettings += Helpers.SetStringAppSetting(context.Request.QueryString.Get("ShowInfoIP"), "ShowInfoIP");
            
            Helpers.SetFloatAppSetting(context.Request.QueryString.Get("BlurBoxSigmaX"), "BlurBoxSigmaX");
            Helpers.SetFloatAppSetting(context.Request.QueryString.Get("BlurBoxSigmaY"), "BlurBoxSigmaY");
            Helpers.SetDoubleAppSetting(context.Request.QueryString.Get("BlurBoxMargin"), "BlurBoxMargin");
            

            // setup commands
            if (context.Request.QueryString.Get("COMMAND") != null)
            {
                if (context.Request.QueryString.Get("COMMAND") == "INFOBAR_DATETIME_On" || context.Request.QueryString.Get("COMMAND") == "INFOBAR_DATETIME_Off")
                {
                    if (context.Request.QueryString.Get("on") == "false")
                    {
                        refreshSettings += Helpers.SetBoolAppSetting("on", "ShowInfoDateTime");
                        refreshSettings += Helpers.SetBoolAppSetting("off", "ShowInfoFileName");
                        refreshSettings += Helpers.SetStringAppSetting("false", "ShowInfoIP");
                    }
                    else
                    {
                        refreshSettings += Helpers.SetBoolAppSetting("off", "ShowInfoDateTime");
                    }
                }
                if (context.Request.QueryString.Get("COMMAND") == "INFOBAR_FILENAME_On" || context.Request.QueryString.Get("COMMAND") == "INFOBAR_FILENAME_Off")
                {
                    if (context.Request.QueryString.Get("on") == "false")
                    {
                        refreshSettings += Helpers.SetBoolAppSetting("on", "ShowInfoFileName");
                        refreshSettings += Helpers.SetBoolAppSetting("off", "ShowInfoDateTime");
                        refreshSettings += Helpers.SetStringAppSetting("false", "ShowInfoIP");
                    }
                    else
                    {
                        refreshSettings += Helpers.SetBoolAppSetting("off", "ShowInfoFileName");
                    }
                }
                if (context.Request.QueryString.Get("COMMAND") == "INFOBAR_IP_On" || context.Request.QueryString.Get("COMMAND") == "INFOBAR_IP_Off")
                {
                    if (context.Request.QueryString.Get("on") == "false")
                    {
                        refreshSettings += Helpers.SetStringAppSetting("true", "ShowInfoIP");
                        refreshSettings += Helpers.SetBoolAppSetting("off", "ShowInfoDateTime");
                        refreshSettings += Helpers.SetBoolAppSetting("off", "ShowInfoFileName");
                    }
                    else
                    {
                        refreshSettings += Helpers.SetStringAppSetting("false", "ShowInfoIP");
                    }
                }

                if (context.Request.QueryString.Get("COMMAND") == "SCREENON")
                {
                    refreshSettings += Helpers.SetBoolAppSetting("on", "ScreenStatus");
                }
                else if (context.Request.QueryString.Get("COMMAND") == "SCREENOFF")
                {
                    refreshSettings += Helpers.SetBoolAppSetting("off", "ScreenStatus");
                }

                

                if (context.Request.QueryString.Get("COMMAND") == "CONTROL_PAUSE_On")
                {
                    refreshSettings += Helpers.SetBoolAppSetting("on", "SlideShowPaused");
                }
                else if (context.Request.QueryString.Get("COMMAND") == "CONTROL_PAUSE_Off")
                {
                    refreshSettings += Helpers.SetBoolAppSetting("off", "SlideShowPaused");
                }



                if (context.Request.QueryString.Get("COMMAND") == "SHUFFLE_On")
                {
                    refreshSettings += Helpers.SetBoolAppSetting("on", "Shuffle");
                }
                else if (context.Request.QueryString.Get("COMMAND") == "SHUFFLE_Off")
                {
                    refreshSettings += Helpers.SetBoolAppSetting("off", "Shuffle");
                }


                //Upload Images
                if (context.Request.QueryString.Get("COMMAND") == "UTILITY_UPLOADFILE")
                {
                    if (context.Request.ContentType != null)
                    {
                        ImageUploaded = CommandProcessor.SaveFile(context.Request.ContentEncoding, CommandProcessor.GetBoundary(context.Request.ContentType), context.Request.InputStream, context.Request.QueryString.Get("Extension"));
                    }
                }
                else if (context.Request.QueryString.Get("COMMAND") == "UTILITY_DELETEFILE")
                {
                    CommandProcessor.DeleteFile(context.Request.QueryString.Get("FILENAME"));
                }
                else
                {
                    CommandProcessor.ProcessCommand(context.Request.QueryString.Get("COMMAND"));
                }

                refreshSettings++; // Any commands should invoke a settings refresh
            }

            if (context.Request.QueryString.Get("SETFILE") != null)
            {
                CommandProcessor.ProcessSetFile(context.Request.QueryString.Get("SETFILE"));
            }

            // Setup synced frames
            if ((context.Request.QueryString.Get("ClientIP") != null) && (context.Request.QueryString.Get("ClientIP") != ""))
            {
                string ip = context.Request.QueryString.Get("ClientIP");
                if (!AppSettings.Default.RemoteClients.Contains(ip))
                {
                    AppSettings.Default.RemoteClients.Add(context.Request.QueryString.Get("ClientIP"));
                }

                SyncedFrame.SyncEngine.AddFrame(ip);
                Logger.LogComment("Added Frame to be Synced...");
            }
            if (context.Request.QueryString.Get("CLEARSYNCLIST") != null)
            {
                AppSettings.Default.RemoteClients.Clear();
                SyncedFrame.SyncEngine.syncedFrames.Clear();
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
            try
            {
                string rem = context.Request.QueryString.Get("rem");
                if (rem != null)
                {
                    Logger.LogComment("SimpleHTTPServer: Removing directory..." + rem);
                    string removeDir = rem.Replace("'", "");
                    AppSettings.Default.SearchDirectories.Remove(rem);

                    List<string> ToRemove = new List<string>();

                    // Go through and find items to remove. We can't modify the list 'in place' so make a target
                    // list and then go through that. Awkward but too tired to figure out the right way to fix
                    // this right now.
                    Logger.LogComment("Emumerating directories to remove..");
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
            }
            catch (Exception exc)
            {
                // Note: Lots of potentially dangerous work in removing, so adding a check
                // to backstop any issues.
                Logger.LogComment("Excpetion when trying to remove directory! " + exc.ToString());
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
            if (!String.IsNullOrEmpty(directoryToAdd))
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
                Logger.LogComment("SIMPLEHTTPSERVER: Adding directory: " + HttpUtility.HtmlDecode(subdirectorytoadd));
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

            Logger.LogComment("Processed web request. Refreshdirectories Value was: " + refreshDirectories);
            Logger.LogComment("Processed web request. RefreshSettings Value is: " + refreshSettings);
            if (refreshDirectories > 0)
            {
                AppSettings.Default.RefreshDirctories = true;
            }
            if (refreshSettings > 0)
            {
                AppSettings.Default.ReloadSettings = true;
            }
            Logger.LogComment("SimpleHTTPServer: saving appsettings");
            AppSettings.Default.Save();

        }



        // return the default page back:

        if (context.Request.QueryString.Get("COMMAND") == "PAGE_UPLOADFILE" ||
                context.Request.QueryString.Get("COMMAND") == "UTILITY_UPLOADFILE" ||
                    context.Request.QueryString.Get("COMMAND") == "UTILITY_DELETEFILE")
        {
            GetUploadPage(ImageUploaded);
        }

        else if (context.Request.RawUrl == "/AppData.htm")
        {
            GetAppDataPage();
        }
        else
        {
            GetDefaultPage();
        }


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

        //need to change 2nd condition later, need a more appropriate condition
        if (File.Exists(filename) && !filename.Contains("upload.htm") && context.Request.RawUrl != "/AppData.htm" && context.Request.RawUrl != "/Log.htm" && context.Request.RawUrl !="/ico/favicon-32x32.png")
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
            if (context.Request.QueryString.Get("COMMAND") == "PAGE_UPLOADFILE" ||
                  context.Request.QueryString.Get("COMMAND") == "UTILITY_UPLOADFILE" ||
                    context.Request.QueryString.Get("COMMAND") == "UTILITY_DELETEFILE")
            {
                response = GetUploadPage(ImageUploaded);
            }
            else if (context.Request.RawUrl == "/AppData.htm")
            {
                response = GetAppDataPage();
            }
            else if (context.Request.RawUrl == "/Log.htm")
            {
                response = GetLogPage();
            }
            else
            {
                response = GetDefaultPage();
            }

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

    public string GetUploadPage(bool ImageUploaded)
    {
        TextReader reader = File.OpenText("./web/upload.htm");
        string page = reader.ReadToEnd();

        page = page.Replace("<!--VERSIONSTRING-->", Assembly.GetExecutingAssembly().GetName().Version.ToString());

        var filePaths = Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory + "/web/uploads/");
        StringBuilder tr = new StringBuilder();
        foreach (string filePath in filePaths)
        {
            tr.Append("<tr><td><img class='myImg' src='/uploads/" + Path.GetFileName(filePath) + "' style='width:80%; max-width:175px; max-height:175px'></td><td><a class='' href='upload.htm?COMMAND=UTILITY_DELETEFILE&FILENAME=" + Path.GetFileName(filePath) + "'>Delete</a></td></tr>");
        }

        page = page.Replace("<!-- IMAGE TABLE BODY -->", tr.ToString());
        if (ImageUploaded)
        {
            page = page.Replace("<!-- ImageDisplay -->", "none");
        }
        else
        {
            page = page.Replace("<!-- ImageDisplay -->", "block");
        }
        return page;
    }

    /// <summary>
    /// Returns a 'hidden' page to enable easy viewing of the log file
    /// </summary>
    /// <returns></returns>
    public string GetLogPage()
    {
        TextReader reader = File.OpenText("./web/Log.htm");
        string page = reader.ReadToEnd();

        page = page.Replace("<!--VERSIONSTRING-->", Assembly.GetExecutingAssembly().GetName().Version.ToString());

        page = page.Replace("<!--LOG-->", Logger.GetLogAsHTML());

        return page;
         
    }

    public string GetAppDataPage()
    {
        return Newtonsoft.Json.JsonConvert.SerializeObject(AppSettings.Default, Newtonsoft.Json.Formatting.Indented);
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
                dirChoices += "</ul></li><br>";
            }

            dirChoices += "</li></div>";

            dirChoices += "<br><br><br><div class ='settings'><h4>Search Directories: </h4>";
            foreach (string directory in AppSettings.Default.SearchDirectories)
            {
                if (directory != AppDomain.CurrentDomain.BaseDirectory + "web/uploads/")
                {
                    dirChoices += directory + "      <br><a class='btn btn-danger btn-sm' href=?rem=" + directory + "#tab2>Remove</a><br>";
                }
                else
                {
                    dirChoices += directory + "      <br><button type='button' class='btn btn-danger btn-sm' disabled>Remove</button><br>";
                }
            }
            dirChoices += "</div><br>";

            page = page.Replace("<!-- DIRECTORIES -->", dirChoices);
            page = page.Replace("<!--CURRENTTAGFILTER-->", AppSettings.Default.InclusiveTagFilters);

            // Sync Client list
            string clients = "";
            foreach (string client in AppSettings.Default.RemoteClients)
            {
                clients += "<br>" + client;
            }
            page = page.Replace("<!--CurrentClients-->", clients);


            // Generate custom settings here
            page = page.Replace("<!--INFOBARFONTSIZE-->", "value=" + AppSettings.Default.InfoBarFontSize.ToString() + ">");
            page = page.Replace("<!--SLIDESHOWDURATION-->", "value=" + AppSettings.Default.SlideshowTransitionTime.ToString() + ">");
            page = page.Replace("<!--TRANSITIONTIME-->", "value=" + AppSettings.Default.FadeTransitionTime.ToString() + ">");
            page = page.Replace("<!--IPADDRESSTIME-->", "value=" + AppSettings.Default.NumberOfSecondsToShowIP.ToString() + ">");
            page = page.Replace("<!--DATETIMEFORMAT-->", "value='" + AppSettings.Default.DateTimeFormat + "'>");
            page = page.Replace("<!--DATETIMEFONTFAMILY-->", "value='" + AppSettings.Default.DateTimeFontFamily + "'>");
            page = page.Replace("<!--BLURBOXSIGMAX-->", "value='" + AppSettings.Default.BlurBoxSigmaX + "'>");
            page = page.Replace("<!--BLURBOXSIGMAY-->", "value='" + AppSettings.Default.BlurBoxSigmaY + "'>");
            page = page.Replace("<!--BLURBOXMARGIN-->", "value='" + AppSettings.Default.BlurBoxMargin + "'>");

            //page = page.Replace("<!--ShowInfoIP-->", "value='" + AppSettings.Default.ShowInfoIP + "'>");


            // Fill in info from the app here
            page = page.Replace("<!--VERSIONSTRING-->", Assembly.GetExecutingAssembly().GetName().Version.ToString());
            // Fill in 'current settings' here
            // Note: I am terrible at the compressed notation for if/else, but it is really clean here. If you need a primer, or for my
            // reference, this helps: https://www.csharp-console-examples.com/conditional/if-else-statement/c-if-else-shorthand-with-examples/
            // We store settings as boolean, but really sometimes they're better expressed as 'on/off'. The 'friendly' strings below are transaltions
            // from the internal way we store stuff to a friendly way to show the user the current setting.


            // Rotation Settings 

            switch (AppSettings.Default.Rotation.ToString())
            {
                case "0":
                    page = page.Replace("<!--ROTATIONCURRENTSETTING0-->", "value=\"0\" checked");
                    page = page.Replace("<!--ROTATIONCURRENTSETTING90-->", "value=\"90\"");
                    page = page.Replace("<!--ROTATIONCURRENTSETTING180-->", "value=\"180\"");
                    page = page.Replace("<!--ROTATIONCURRENTSETTING270-->", "value=\"270\"");
                    break;
                case "90":
                    page = page.Replace("<!--ROTATIONCURRENTSETTING90-->", "value=\"90\" checked");
                    page = page.Replace("<!--ROTATIONCURRENTSETTING0-->", "value=\"0\"");
                    page = page.Replace("<!--ROTATIONCURRENTSETTING180-->", "value=\"180\"");
                    page = page.Replace("<!--ROTATIONCURRENTSETTING270-->", "value=\"270\"");
                    break;
                case "180":
                    page = page.Replace("<!--ROTATIONCURRENTSETTING180-->", "value=\"180\" checked");
                    page = page.Replace("<!--ROTATIONCURRENTSETTING90-->", "value=\"90\"");
                    page = page.Replace("<!--ROTATIONCURRENTSETTING0-->", "value=\"0\"");
                    page = page.Replace("<!--ROTATIONCURRENTSETTING270-->", "value=\"270\"");
                    break;
                case "270":
                    page = page.Replace("<!--ROTATIONCURRENTSETTING270-->", "value=\"270\" checked");
                    page = page.Replace("<!--ROTATIONCURRENTSETTING90-->", "value=\"90\"");
                    page = page.Replace("<!--ROTATIONCURRENTSETTING180-->", "value=\"180\"");
                    page = page.Replace("<!--ROTATIONCURRENTSETTING0-->", "value=\"0\"");
                    break;
            }

            //Image Scale Settings

            switch (AppSettings.Default.ImageStretch.ToString())
            {
                case "UniformToFill":
                    page = page.Replace("<!--IMAGESCALINGCURRENTSETTINGUNIFILL-->", "checked");
                    page = page.Replace("<!--IMAGESCALINGCURRENTSETTINGUNI-->", "");
                    page = page.Replace("<!--IMAGESCALINGCURRENTSETTINGNONE-->", "");
                    page = page.Replace("<!--IMAGESCALINGCURRENTSETTINGFILL-->", "");
                    break;
                case "Uniform":
                    page = page.Replace("<!--IMAGESCALINGCURRENTSETTINGUNI-->", "checked");
                    page = page.Replace("<!--IMAGESCALINGCURRENTSETTINGUNIFILL-->", "");
                    page = page.Replace("<!--IMAGESCALINGCURRENTSETTINGNONE-->", "");
                    page = page.Replace("<!--IMAGESCALINGCURRENTSETTINGFILL-->", "");
                    break;
                case "None":
                    page = page.Replace("<!--IMAGESCALINGCURRENTSETTINGNONE-->", "checked");
                    page = page.Replace("<!--IMAGESCALINGCURRENTSETTINGUNIFILL-->", "");
                    page = page.Replace("<!--IMAGESCALINGCURRENTSETTINGUNI-->", "");
                    page = page.Replace("<!--IMAGESCALINGCURRENTSETTINGFILL-->", "");
                    break;
                case "Fill":
                    page = page.Replace("<!--IMAGESCALINGCURRENTSETTINGFILL-->", "checked");
                    page = page.Replace("<!--IMAGESCALINGCURRENTSETTINGUNIFILL-->", "");
                    page = page.Replace("<!--IMAGESCALINGCURRENTSETTINGUNI-->", "");
                    page = page.Replace("<!--IMAGESCALINGCURRENTSETTINGNONE-->", "");
                    break;
            }

            // Shuffle Settings

            string shufflefriendly = AppSettings.Default.Shuffle ? "On" : "Off";
            switch (shufflefriendly.ToString())
            {
                case "On":
                    page = page.Replace("<!--SHUFFLECURRENTSETTINGON-->", "checked");
                    page = page.Replace("<!--SHUFFLECURRENTSETTINGOFF-->", "");
                    break;
                case "Off":
                    page = page.Replace("<!--SHUFFLECURRENTSETTINGOFF-->", "checked");
                    page = page.Replace("<!--SHUFFLECURRENTSETTINGON-->", "");
                    break;
            }


            // Video Aspect Setting


            page = page.Replace("<!--VIDEOASPECTMODECURRENTSETTING-->", "  " + AppSettings.Default.VideoStretch);

            switch (AppSettings.Default.VideoStretch.ToString())
            {
                case "Fill":
                    page = page.Replace("<!--VIDEOASPECTFILL-->", "checked");
                    page = page.Replace("<!--VIDEOASPECTLB-->", "");
                    page = page.Replace("<!--VIDEOASPECTSTRETCH-->", "");

                    break;
                case "Letterbox":
                    page = page.Replace("<!--VIDEOASPECTLB-->", "checked");
                    page = page.Replace("<!--VIDEOASPECTFILL-->", "");
                    page = page.Replace("<!--VIDEOASPECTSTRETCH-->", "");

                    break;
                case "Stretch":
                    page = page.Replace("<!--VIDEOASPECTSTRETCH-->", "checked");
                    page = page.Replace("<!--VIDEOASPECTFILL-->", "");
                    page = page.Replace("<!--VIDEOASPECTLB-->", "");

                    break;
            }


            // Video Player Audio Settings

            string videoplayasaudiofriendly = AppSettings.Default.VideoVolume ? "On" : "Off";
            page = page.Replace("<!--VIDEOPLAYAUDIOCURRENTSETTING-->", "(Current value: " + videoplayasaudiofriendly + " )");
            switch (videoplayasaudiofriendly.ToString())
            {
                case "On":
                    page = page.Replace("<!--VIDEOPLAYAUDIOON-->", "checked");
                    page = page.Replace("<!--VIDEOPLAYAUDIOOFF-->", "");
                    break;
                case "Off":
                    page = page.Replace("<!--VIDEOPLAYAUDIOOFF-->", "checked");
                    page = page.Replace("<!--VIDEOPLAYAUDIOON-->", "");
                    break;
            }



            // Sync Tab Setting

            string syncsettingsfriendly = AppSettings.Default.IsSyncEnabled ? "On" : "Off";
            switch (syncsettingsfriendly.ToString())
            {
                case "On":
                    page = page.Replace("<!--SYNCCURRENTSETTINGON-->", "checked");
                    page = page.Replace("<!--SYNCCURRENTSETTINGOFF-->", "");
                    break;
                case "Off":
                    page = page.Replace("<!--SYNCCURRENTSETTINGOFF-->", "checked");
                    page = page.Replace("<!--SYNCCURRENTSETTINGON-->", "");
                    break;
            }

            // Tree View Settings

            string expandcollaspedfriendly = AppSettings.Default.ExpandDirectoriesByDefault ? "Expanded" : "Toggleable";
            page = page.Replace("<!--TREEVIEWCURRENTSETTING-->", "(Current value: " + expandcollaspedfriendly + " )");
            switch (expandcollaspedfriendly)
            {
                case "Expanded":
                    page = page.Replace("<!--TREEVIEWON-->", "checked");
                    page = page.Replace("<!--TREEVIEWOFF-->", "");
                    break;
                case "Toggleable":
                    page = page.Replace("<!--TREEVIEWOFF-->", "checked");
                    page = page.Replace("<!--TREEVIEWON-->", "");
                    break;
            }
            // Video Playback Mode Settings
            if (AppSettings.Default.PlaybackFullVideo)
            {
                page = page.Replace("<!--VIDEOPLAYBACKOBEY-->", "");
                page = page.Replace("<!--VIDEOPLAYBACKIGNORE-->", "checked");
            }
            else
            {
                page = page.Replace("<!--VIDEOPLAYBACKOBEY-->", "checked");
                page = page.Replace("<!--VIDEOPLAYBACKIGNORE-->", "");
            }

            // Logging on/off Settings
            string loggingenabledfriendly = AppSettings.Default.EnableLogging ? "Enable" : "Disable";
            switch (loggingenabledfriendly)
            {
                case "Enable":
                    page = page.Replace("<!--LOGGINGON-->", "checked");
                    page = page.Replace("<!--LOGGINGOFF-->", "");
                    break;
                case "Disable":
                    page = page.Replace("<!--LOGGINGOFF-->", "checked");
                    page = page.Replace("<!--LOGGINGON-->", "");
                    break;
            }



            // Control Button Control

            if (AppSettings.Default.ShowInfoDateTime == true)
            {
                page = page.Replace("<!--ShowInfoDateTime-->", "<a class=\"btn btn-success btn-lg\"href=\"default.htm?COMMAND=INFOBAR_DATETIME_On&on=true\">Hide Date & Time</a>");
            }
            else
            {
                page = page.Replace("<!--ShowInfoDateTime-->", "<a class=\"btn btn-primary btn-lg\"href=\"default.htm?COMMAND=INFOBAR_DATETIME_Off&on=false\">Show Date & Time</a>");
            }
            if (AppSettings.Default.ShowInfoFileName == true)
            {
                page = page.Replace("<!--ShowInfoFileName-->", "<a class=\"btn btn-success btn-lg\"href=\"default.htm?COMMAND=INFOBAR_FILENAME_On&on=true\">Hide Filename</a>");
            }
            else
            {
                page = page.Replace("<!--ShowInfoFileName-->", "<a class=\"btn btn-primary btn-lg\"href=\"default.htm?COMMAND=INFOBAR_FILENAME_Off&on=false\">Show Filename</a>");
            }
            if (AppSettings.Default.ShowInfoIP == "true")
            {
                page = page.Replace("<!--ShowInfoIP-->", "<a class=\"btn btn-success btn-lg\"href=\"default.htm?COMMAND=INFOBAR_IP_On&on=true\">Hide IP</a>");
            }
            else
            {
                page = page.Replace("<!--ShowInfoIP-->", "<a class=\"btn btn-primary btn-lg\"href=\"default.htm?COMMAND=INFOBAR_IP_Off&on=false\">Show IP</a>");
            }

            if (AppSettings.Default.ScreenStatus == true)
            {
                page = page.Replace("<!--TurnScreenOnOff-->", "<a class=\"btn btn-success btn-lg\"href=\"default.htm?COMMAND=SCREENOFF \">Turn Screen Off</a>");
            }
            else
            {
                page = page.Replace("<!--TurnScreenOnOff-->", "<a class=\"btn btn-primary btn-lg\"href=\"default.htm?COMMAND=SCREENON \">Turn Screen On</a>");
            }

            if (AppSettings.Default.SlideShowPaused == true)
            {
                page = page.Replace("<!--SlideShowOnOff-->", "<a class=\"btn btn-danger btn-lg\"href=\"default.htm?COMMAND=CONTROL_PAUSE_Off \">Paused</a>");
            }
            else
            {
                page = page.Replace("<!--SlideShowOnOff-->", "<a class=\"btn btn-primary btn-lg\"href=\"default.htm?COMMAND=CONTROL_PAUSE_On \">Playing</a>");
            }

            if (AppSettings.Default.Shuffle == true)
            {
                page = page.Replace("<!--ShuffleOnOff-->", "<a class=\"btn btn-success btn-lg\"href=\"default.htm?COMMAND=SHUFFLE_Off \">Shuffle On</a>");
            }
            else
            {
                page = page.Replace("<!--ShuffleOnOff-->", "<a class=\"btn btn-primary btn-lg\"href=\"default.htm?COMMAND=SHUFFLE_On \">Shuffle Off</a>");
            }


            // Expand Directory

            if (AppSettings.Default.ExpandDirectoriesByDefault)
            {
                page = page.Replace(@"//JSLISTOPENSETTING", "JSLists.openAll();");
            }

            // Write JSON to hidden field, This should be done to a new URL file, but this works for me now. 

            string json = Newtonsoft.Json.JsonConvert.SerializeObject(AppSettings.Default, Newtonsoft.Json.Formatting.Indented);


            page = page.Replace("<!--JSONSettings-->", json);

            return page;
        }
        catch (Exception exc)
        {
            // If anything happens, we have to return some data so the user knows what is going on)
            return "Fatal Error! Excpetion occurred.  Info: " + exc.ToString();
        }

    }
}