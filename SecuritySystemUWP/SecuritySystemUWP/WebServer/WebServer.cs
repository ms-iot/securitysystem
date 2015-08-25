using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.AppService;
using Windows.ApplicationModel.Core;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Networking.Sockets;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.System.Threading;

namespace SecuritySystemUWP
{
    public class WebServer
    {
        public bool IsRunning { get; private set; }

        /// <summary>
        /// Starts the web server on the specified port
        /// </summary>
        /// <param name="serverPort">Web server port</param>
        public void Start(int serverPort)
        {
            var server = new HttpServer(serverPort);
            IAsyncAction asyncAction = ThreadPool.RunAsync(
                (s) =>
                {
                    server.StartServer();
                });

            IsRunning = true;
        }
    }

    /// <summary>
    /// HttpServer class that services the content for the Security System web interface
    /// </summary>
    public sealed class HttpServer : IDisposable
    {
        private const uint BufferSize = 8192;
        private int port = 8000;
        private readonly StreamSocketListener listener;
        private WebHelper helper;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="serverPort">Port to start server on</param>
        public HttpServer(int serverPort)
        {
            helper = new WebHelper();
            listener = new StreamSocketListener();
            port = serverPort;
            listener.ConnectionReceived += (s, e) =>
            {
                try
                {
                    // Process incoming request
                    processRequestAsync(e.Socket);
                }catch(Exception ex)
                {
                    Debug.WriteLine("Exception in StreamSocketListener.ConnectionReceived(): " + ex.Message);
                }
            };
        }

        public async void StartServer()
        {
            await helper.InitializeAsync();

#pragma warning disable CS4014
            listener.BindServiceNameAsync(port.ToString());
#pragma warning restore CS4014
        }

        public void Dispose()
        {
            listener.Dispose();
        }

        /// <summary>
        /// Process the incoming request
        /// </summary>
        /// <param name="socket"></param>
        private async void processRequestAsync(StreamSocket socket)
        {
            try
            {
                StringBuilder request = new StringBuilder();
                using (IInputStream input = socket.InputStream)
                {
                    // Convert the request bytes to a string that we understand
                    byte[] data = new byte[BufferSize];
                    IBuffer buffer = data.AsBuffer();
                    uint dataRead = BufferSize;
                    while (dataRead == BufferSize)
                    {
                        await input.ReadAsync(buffer, BufferSize, InputStreamOptions.Partial);
                        request.Append(Encoding.UTF8.GetString(data, 0, data.Length));
                        dataRead = buffer.Length;
                    }
                }

                using (IOutputStream output = socket.OutputStream)
                {
                    // Parse the request
                    string requestMethod = request.ToString().Split('\n')[0];
                    string[] requestParts = requestMethod.Split(' ');

                    // Process the request and write a response to send back to the browser
                    if (requestParts[0] == "GET")
                        await writeResponseAsync(requestParts[1], output, socket.Information);
                    else
                        throw new InvalidDataException("HTTP method not supported: "
                                                       + requestParts[0]);
                }
            }catch(Exception ex)
            {
                Debug.WriteLine("Exception in processRequestAsync(): " + ex.Message);

                //This exception is thrown when someone clicks on a link while the current page is still loading. This isn't really an exception worth tracking as it will be thrown a lot, but doesn't affect anything.
                /*
                // Log telemetry event about this exception
                var events = new Dictionary<string, string> { { "WebServer", ex.Message } };
                App.Controller.TelemetryClient.TrackEvent("FailedToProcessRequestAsync", events);
                */
            }
        }
        
        private async Task writeResponseAsync(string request, IOutputStream os, StreamSocketInformation socketInfo)
        {
            try
            {
                string[] requestParts = request.Split('/');

                // Request for the root page, so redirect to home page
                if (request.Equals("/"))
                {
                    await redirectToPage(NavConstants.HOME_PAGE, os);
                }
                // Request for the home page
                else if(request.Contains(NavConstants.HOME_PAGE))
                {
                    // Generate the default config page
                    string html = helper.GenerateStatusPage();
                    await WebHelper.WriteToStream(html, os);
                }
                // Request for the settings page
                else if (request.Contains(NavConstants.SETTINGS_PAGE))
                {
                    // Process the GET parameters
                    if (request.Contains("?"))
                    {
                        // Format the URI with the get parameters
                        Uri uri = new Uri("http://" + socketInfo.LocalAddress + ":" + socketInfo.LocalPort + request);

                        // Take the parameters from the URL and put it into Settings
                        helper.ParseUriIntoSettings(uri);
                        await AppSettings.SaveAsync(App.Controller.XmlSettings, "Settings.xml");

                        // This is an event that lets us know what the controller is done restarting after the settings are applied
                        AutoResetEvent ase = new AutoResetEvent(false);

                        // Dispose and Initialize need to be called on UI thread because of DispatcherTimers
                        await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () =>
                        {
                            try
                            {
                                // Restart the controller to apply new settings
                                App.Controller.Dispose();
                                await App.Controller.Initialize();

                                // Create the settings page and add a confirmation message that the settings were applied successfully
                                string html = helper.GeneratePage("Security System Config", "Security System Config", helper.CreateHtmlFormFromSettings(), "<span style='color:Green'>Configuration saved!</span><br><br>");
                                await WebHelper.WriteToStream(html, os);
                            }
                            catch (Exception ex)
                            {
                                Debug.WriteLine("Error restarting controller: " + ex.Message);
                            }

                            // Signal that the restart is done
                            ase.Set();
                        });

                        // Wait for controller restart to finish
                        ase.WaitOne();
                    }
                    else
                    {
                        // Generate the default config page
                        string html = helper.GeneratePage("Security System Config", "Security System Config", helper.CreateHtmlFormFromSettings());
                        await WebHelper.WriteToStream(html, os);
                    }
                }
                // Request for the OneDrive page
                else if (request.Contains(NavConstants.ONEDRIVE_PAGE))
                {
                    // Take in the parameters and try to login to OneDrive
                    if (request.Contains("?"))
                    {
                        Uri uri = new Uri("http://" + socketInfo.LocalAddress + ":" + socketInfo.LocalPort + request);
                        await helper.ParseOneDriveUri(uri);

                        var oneDrive = App.Controller.Storage as OneDrive;
                        if (oneDrive != null)
                        {
                            if (oneDrive.IsLoggedIn())
                            {
                                // Save tokens to settings file if we successfully logged in
                                await AppSettings.SaveAsync(App.Controller.XmlSettings, "Settings.xml");
                            }
                        }
                    }

                    // Generate page and write to stream
                    string html = helper.GenerateOneDrivePage();
                    await WebHelper.WriteToStream(html, os);
                }
                // Request for gallery page
                else if(request.Contains(NavConstants.GALLERY_PAGE))
                {
                    string html = "";
                    var storageType = App.Controller.Storage.GetType();
                    // If the storage type is OneDrive, generate page with link to OneDrive
                    if (storageType == typeof(OneDrive))
                    {
                        html = helper.GeneratePage("Gallery", "Gallery", "<b>" + storageType.Name + "</b> is set as your storage provider.&nbsp;&nbsp;"
                            + "Please view your pictures on <a href='http://www.onedrive.com' target='_blank'>OneDrive</a>.<br><br>"
                            + "To view your pictures here, please select <b>" + StorageProvider.Local + "</b> as your storage provider.");
                    }
                    // Otherwise show the gallery for the files on the device
                    else
                    {
                        StorageFolder folder = KnownFolders.PicturesLibrary;
                        int page = 1;
                        int pageSize = 30;

                        // Parse GET parameters
                        if (request.Contains("?"))
                        {
                            Uri uri = new Uri("http://" + socketInfo.LocalAddress + ":" + socketInfo.LocalPort + request);
                            var parameters = helper.ParseGetParametersFromUrl(uri);
                            try
                            {
                                // Find the folder that's specified in the parameters
                                folder = await StorageFolder.GetFolderFromPathAsync(parameters["folder"]);

                                if(parameters.ContainsKey("page"))
                                {
                                    try
                                    {
                                        page = Convert.ToInt32(parameters["page"]);
                                    }
                                    catch (Exception) { }
                                }

                                if (parameters.ContainsKey("pageSize"))
                                {
                                    try
                                    {
                                        pageSize = Convert.ToInt32(parameters["pageSize"]);
                                    }
                                    catch (Exception) { }
                                }
                            }
                            catch (Exception ex)
                            {
                                Debug.WriteLine("Exception finding folder: " + ex.Message);
                                folder = await folder.GetFolderAsync(AppSettings.FolderName);

                                // Log telemetry event about this exception
                                var events = new Dictionary<string, string> { { "WebServer", ex.Message } };
                                App.Controller.TelemetryClient.TrackEvent("FailedToGetFolderFromPath", events);
                            }
                        }
                        else
                        {
                            folder = await folder.GetFolderAsync(AppSettings.FolderName);
                        }

                        // Generate gallery page and write to stream
                        string galleryHtml = await helper.GenerateGallery(folder, page, pageSize);
                        html = helper.GeneratePage("Gallery", "Gallery", galleryHtml);
                    }

                    await WebHelper.WriteToStream(html, os);
                }
                // Request for API
                else if(request.Contains("api"))
                {
                    try
                    {
                        if (requestParts.Length > 2)
                        {
                            switch (requestParts[2].ToLower())
                            {
                                // An image from the gallery was requested
                                case "gallery":
                                    var temp = request.Split(new string[] { "gallery/" }, StringSplitOptions.None);
                                    // HTML decode the file path
                                    string decodedPath = WebUtility.UrlDecode(temp[1]);

                                    // Retrieve the file
                                    StorageFile file = await StorageFile.GetFileFromPathAsync(decodedPath);

                                    // Write the file to the stream
                                    await WebHelper.WriteFileToStream(file, os);
                                    break;
                            }
                        }
                    }catch(Exception ex)
                    {
                        Debug.WriteLine("Exception in web API: " + ex.Message);

                        // Log telemetry event about this exception
                        var events = new Dictionary<string, string> { { "WebServer", ex.Message } };
                        App.Controller.TelemetryClient.TrackEvent("FailedToProcessApiRequest", events);
                    }
                }
                // Request for a file that is in the Assets\Web folder (e.g. logo, css file)
                else
                {
                    using (Stream resp = os.AsStreamForWrite())
                    {
                        bool exists = true;
                        try
                        {
                            var folder = Windows.ApplicationModel.Package.Current.InstalledLocation;

                            // Map the requested path to Assets\Web folder
                            string filePath = @"Assets\Web" + request.Replace('/', '\\');
                            
                            // Open the file and write it to the stream
                            using (Stream fs = await folder.OpenStreamForReadAsync(filePath))
                            {
                                string header = String.Format("HTTP/1.1 200 OK\r\n" +
                                                "Content-Length: {0}\r\n{1}" +
                                                "Connection: close\r\n\r\n",
                                                fs.Length,
                                                ((request.Contains("css")) ? "Content-Type: text/css\r\n" : ""));
                                byte[] headerArray = Encoding.UTF8.GetBytes(header);
                                await resp.WriteAsync(headerArray, 0, headerArray.Length);
                                await fs.CopyToAsync(resp);
                            }
                        }
                        catch (FileNotFoundException ex)
                        {
                            exists = false;

                            // Log telemetry event about this exception
                            var events = new Dictionary<string, string> { { "WebServer", ex.Message } };
                            App.Controller.TelemetryClient.TrackEvent("FailedToOpenStream", events);
                        }

                        // Send 404 not found if can't find file
                        if (!exists)
                        {
                            byte[] headerArray = Encoding.UTF8.GetBytes(
                                                  "HTTP/1.1 404 Not Found\r\n" +
                                                  "Content-Length:0\r\n" +
                                                  "Connection: close\r\n\r\n");
                            await resp.WriteAsync(headerArray, 0, headerArray.Length);
                        }

                        await resp.FlushAsync();
                    }
                }
            }catch(Exception ex)
            {
                Debug.WriteLine("Exception in writeResponseAsync(): " + ex.Message);
                Debug.WriteLine(ex.StackTrace);

                // Log telemetry event about this exception
                var events = new Dictionary<string, string> { { "WebServer", ex.Message } };
                App.Controller.TelemetryClient.TrackEvent("FailedToWriteResponse", events);

                try
                {
                    // Try to send an error page back if there was a problem servicing the request
                    string html = helper.GeneratePage("Error", "Error", "There's been an error: " + ex.Message + "<br><br>" + ex.StackTrace);
                    await WebHelper.WriteToStream(html, os);
                }
                catch (Exception e)
                {
                    App.Controller.TelemetryClient.TrackException(e);
                }
            }
        }

        /// <summary>
        /// Redirect to a page
        /// </summary>
        /// <param name="path">Relative path to page</param>
        /// <param name="os"></param>
        /// <returns></returns>
        private async Task redirectToPage(string path, IOutputStream os)
        {
            using (Stream resp = os.AsStreamForWrite())
            {
                byte[] headerArray = Encoding.UTF8.GetBytes(
                                  "HTTP/1.1 302 Found\r\n" +
                                  "Content-Length:0\r\n" +
                                  "Location: /" + path + "\r\n" +
                                  "Connection: close\r\n\r\n");
                await resp.WriteAsync(headerArray, 0, headerArray.Length);
                await resp.FlushAsync();
            }
        }
    }
}
