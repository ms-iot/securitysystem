using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.AppService;
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

    public sealed class HttpServer : IDisposable
    {
        private const uint BufferSize = 8192;
        private int port = 8000;
        private readonly StreamSocketListener listener;
        private WebHelper helper;

        public HttpServer(int serverPort)
        {
            helper = new WebHelper();
            listener = new StreamSocketListener();
            port = serverPort;
            listener.ConnectionReceived += (s, e) => ProcessRequestAsync(e.Socket);
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

        private async void ProcessRequestAsync(StreamSocket socket)
        {
            try
            {
                // this works for text only
                StringBuilder request = new StringBuilder();
                using (IInputStream input = socket.InputStream)
                {
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
                    string requestMethod = request.ToString().Split('\n')[0];
                    string[] requestParts = requestMethod.Split(' ');

                    if (requestParts[0] == "GET")
                        await WriteResponseAsync(requestParts[1], output, socket.Information);
                    else
                        throw new InvalidDataException("HTTP method not supported: "
                                                       + requestParts[0]);
                }
            }catch(Exception e)
            {
                Debug.WriteLine(e.Message);
            }
        }

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

        private async Task WriteResponseAsync(string request, IOutputStream os, StreamSocketInformation socketInfo)
        {
            try
            {
                string[] requestParts = request.Split('/');

                if (request.Equals("/"))
                {
                    await redirectToPage(NavConstants.HOME_PAGE, os);
                }
                else if(request.Contains(NavConstants.HOME_PAGE))
                {
                    // Generate the default config page
                    string html = helper.GenerateStatusPage();
                    await WebHelper.WriteToStream(html, os);
                }
                else if (request.Contains(NavConstants.SETTINGS_PAGE))
                {
                    if (request.Contains("?"))
                    {
                        Uri uri = new Uri("http://" + socketInfo.LocalAddress + ":" + socketInfo.LocalPort + request);

                        // Take the parameters from the URL and put it into Settings
                        helper.ParseUriIntoSettings(uri);
                        await AppSettings.SaveAsync(App.Controller.XmlSettings, "Settings.xml");
                        await App.Controller.Initialize();

                        string html = helper.GeneratePage("Security System Config", "Security System Config", helper.CreateHtmlFormFromSettings(socketInfo), "<span style='color:Green'>Configuration saved!</span><br><br>");
                        await WebHelper.WriteToStream(html, os);
                    }
                    else
                    {
                        // Generate the default config page
                        string html = helper.GeneratePage("Security System Config", "Security System Config", helper.CreateHtmlFormFromSettings(socketInfo));
                        await WebHelper.WriteToStream(html, os);
                    }
                }
                else if (request.Contains(NavConstants.ONEDRIVE_PAGE))
                {
                    // Take in the parameters and try to login to OneDrive
                    if (request.Contains("?"))
                    {
                        Uri uri = new Uri("http://" + socketInfo.LocalAddress + ":" + socketInfo.LocalPort + request);
                        await helper.ParseOneDriveUri(uri);

                        if (OneDrive.IsLoggedIn())
                        {
                            // Save tokens to settings file
                            await AppSettings.SaveAsync(App.Controller.XmlSettings, "Settings.xml");
                        }
                    }

                    string html = helper.GenerateOneDrivePage();
                    await WebHelper.WriteToStream(html, os);
                }
                else if(request.Contains(NavConstants.GALLERY_PAGE))
                {
                    if(App.Controller.Storage.GetType() == typeof(OneDrive))
                    {
                        string html = helper.GeneratePage("Gallery", "Gallery", "OneDrive is enabled.  Please view your pictures on <a href='http://www.onedrive.com' target='_blank'>OneDrive</a>.<br>");
                        await WebHelper.WriteToStream(html, os);
                    }
                    else
                    {
                        StorageFolder folder = KnownFolders.PicturesLibrary;
                        if (request.Contains("?"))
                        {
                            Uri uri = new Uri("http://" + socketInfo.LocalAddress + ":" + socketInfo.LocalPort + request);
                            var parameters = helper.ParseGetParametersFromUrl(uri);
                            try
                            {
                                folder = await StorageFolder.GetFolderFromPathAsync(parameters["folder"]);
                            }
                            catch (Exception e)
                            {
                                Debug.WriteLine(e.Message);
                                folder = await folder.GetFolderAsync(AppSettings.FolderName);
                            }
                        }
                        else
                        {
                            folder = await folder.GetFolderAsync(AppSettings.FolderName);
                        }

                        string galleryHtml = await helper.GenerateGallery(folder);
                        string html = helper.GeneratePage("Gallery", "Gallery", galleryHtml);
                        await WebHelper.WriteToStream(html, os);
                    }
                }
                else if(request.Contains("api"))
                {
                    try
                    {
                        if (requestParts.Length > 2)
                        {
                            switch (requestParts[2].ToLower())
                            {
                                case "reloadapp":
                                    await App.Controller.Initialize();
                                    await redirectToPage(NavConstants.HOME_PAGE, os);
                                    break;
                                case "gallery":
                                    var temp = request.Split(new string[] { "gallery/" }, StringSplitOptions.None);
                                    string decodedPath = WebUtility.UrlDecode(temp[1]);
                                    StorageFile file = await StorageFile.GetFileFromPathAsync(decodedPath);
                                    await WebHelper.WriteFileToStream(file, os);
                                    break;
                            }
                        }
                    }catch(Exception e)
                    {
                        Debug.WriteLine(e.Message);
                    }
                }
                else
                {
                    using (Stream resp = os.AsStreamForWrite())
                    {
                        bool exists = true;
                        try
                        {
                            var folder = Windows.ApplicationModel.Package.Current.InstalledLocation;
                            string filePath = @"Assets\Web" + request.Replace('/', '\\');
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
                        catch (FileNotFoundException)
                        {
                            exists = false;
                        }

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
            }catch(Exception e)
            {
                Debug.WriteLine(e.Message);
                Debug.WriteLine(e.StackTrace);

                try
                {
                    string html = helper.GeneratePage("Error", "Error", "There's been an error: " + e.Message + "<br><br>" + e.StackTrace);
                    await WebHelper.WriteToStream(html, os);
                }
                catch (Exception) { }
            }
        }
    }
}
