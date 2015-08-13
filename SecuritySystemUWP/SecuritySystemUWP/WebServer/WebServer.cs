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
        public static void Start(int serverPort)
        {
            var server = new HttpServer(serverPort);
            IAsyncAction asyncAction = ThreadPool.RunAsync(
                (s) =>
                {
                    server.StartServer();
                });
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
        }

        private async Task WriteResponseAsync(string request, IOutputStream os, StreamSocketInformation socketInfo)
        {
            try
            {
                if (request.Equals("/"))
                {
                    string html = helper.GenerateSettingsConfigPage(socketInfo);
                    await WebHelper.WriteToStream(html, os);
                }
                else if (request.Contains("?") && !request.Contains("htm"))
                {
                    Uri uri = new Uri("http://" + socketInfo.LocalAddress + ":" + socketInfo.LocalPort + request);
                    var decoder = new WwwFormUrlDecoder(uri.Query);
                    foreach (WwwFormUrlDecoderEntry entry in decoder)
                    {
                        var field = typeof(AppSettings).GetField(entry.Name);
                        if (field.FieldType == typeof(int))
                        {
                            field.SetValue(App.XmlSettings, Convert.ToInt32(entry.Value));
                        }
                        else
                        {
                            field.SetValue(App.XmlSettings, entry.Value);
                        }
                    }

                    await AppSettings.SaveAsync(App.XmlSettings, "Settings.xml");

                    string html = helper.GenerateSettingsConfigPage(socketInfo);
                    await WebHelper.WriteToStream(html, os);
                }
                else if(request.Contains("OneDrive.htm"))
                {
                    if(request.Contains("?"))
                    {
                        Uri uri = new Uri("http://" + socketInfo.LocalAddress + ":" + socketInfo.LocalPort + request);
                        await helper.ParseOneDriveUri(uri);
                    }

                    string html = helper.GenerateOneDrivePage();
                    await WebHelper.WriteToStream(html, os);
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
                                                "Content-Length: {0}\r\n" +
                                                "Connection: close\r\n\r\n",
                                                fs.Length);
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
            }
        }

    }
}
