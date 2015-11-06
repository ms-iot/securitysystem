using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using Windows.Web.Http;
using Windows.Web.Http.Filters;
using Windows.Web.Http.Headers;
using Windows.Storage;
using Windows.Storage.Search;
using Windows.UI.Xaml;

namespace SecuritySystemUWP
{
    public class OneDrive : IStorage
    {
        private HttpClient httpClient;
        private CancellationTokenSource cts;
        private bool isLoggedIn = false;
        private Mutex uploadPicturesMutexLock = new Mutex();
        private DispatcherTimer refreshTimer;
        private int numberUploaded = 0;
        private DateTime lastUploadTime = DateTime.MinValue;

        /*******************************************************************************************
* PUBLIC METHODS
*******************************************************************************************/
        public OneDrive()
        {
            //Set up timer to reauthenticate OneDrive login
            refreshTimer = new DispatcherTimer();
            refreshTimer.Interval = TimeSpan.FromMinutes(25);
            refreshTimer.Tick += refreshTimer_Tick;
            refreshTimer.Start();
            //Create Http client for OneDrive requests
            CreateHttpClient(ref httpClient);
            cts = new CancellationTokenSource();
        }

        public void Dispose()
        {
            refreshTimer.Stop();
            cts.Dispose();
            httpClient.Dispose();
        }

        public DateTime LastUploadTime
        {
            get
            {
                return this.lastUploadTime;
            }
        }

        public async void UploadPictures(string camera)
        {
            if (isLoggedIn)
            {
                uploadPicturesMutexLock.WaitOne();

                try
                {
                    QueryOptions querySubfolders = new QueryOptions();
                    querySubfolders.FolderDepth = FolderDepth.Deep;

                    StorageFolder cacheFolder = KnownFolders.PicturesLibrary;
                    cacheFolder = await cacheFolder.GetFolderAsync(AppSettings.FolderName);
                    var result = cacheFolder.CreateFileQueryWithOptions(querySubfolders);
                    var files = await result.GetFilesAsync();

                    foreach (StorageFile file in files)
                    {
                        string imageName = string.Format(AppSettings.ImageNameFormat, camera, DateTime.Now.ToString("MM_dd_yyyy/HH"), DateTime.UtcNow.Ticks.ToString());
                        try
                        {
                            await uploadPictureToOneDrive(AppSettings.FolderName, imageName, file);
                            numberUploaded++;

                            // uploadPictureToOnedrive should throw an exception if it fails, so it's safe to delete
                            await file.DeleteAsync();
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine("UploadPictures(): " + ex.Message);

                            // Log telemetry event about this exception
                            var events = new Dictionary<string, string> { { "OneDrive", ex.Message } };
                            App.Controller.TelemetryClient.TrackEvent("FailedToUploadPicture", events);
                        }
                        this.lastUploadTime = DateTime.Now;
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Exception in UploadPictures() " + ex.Message);

                    // Log telemetry event about this exception
                    var events = new Dictionary<string, string> { { "OneDrive", ex.Message } };
                    App.Controller.TelemetryClient.TrackEvent("FailedToUploadPicture", events);
                }
                finally
                {
                    uploadPicturesMutexLock.ReleaseMutex();
                }
            }
        }

        public async void DeleteExpiredPictures(string camera)
        {
            try
            {
                string folder = string.Format("{0}/{1}/{2}", AppSettings.FolderName, camera, DateTime.Now.Subtract(TimeSpan.FromDays(App.Controller.XmlSettings.StorageDuration)).ToString("MM_dd_yyyy"));
                //List pictures in old day folder
                List<string> pictures = await listPictures(folder);
                if (pictures != null)
                {
                    //Delete all pictures from the day
                    foreach (string picture in pictures)
                    {
                        await deletePicture(folder, picture);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Exception in deleteExpiredPictures() " + ex.Message);

                // Log telemetry event about this exception
                var events = new Dictionary<string, string> { { "OneDrive", ex.Message } };
                App.Controller.TelemetryClient.TrackEvent("FailedToDeletePicture", events);
            }
        }

        public async Task Authorize(string accessCode)
        {
            //Authorize OneDrive login for the first time
            await getTokens(accessCode, "code", "authorization_code");
            SetAuthorization("Bearer", App.Controller.XmlSettings.OneDriveAccessToken);
            isLoggedIn = true;
        }

        public async Task AuthorizeWithRefreshToken(string refreshToken)
        {
            //Reauthorize OneDrive regularly after initial login
            CreateHttpClient(ref httpClient);
            await getTokens(refreshToken, "refresh_token", "refresh_token");
            SetAuthorization("Bearer", App.Controller.XmlSettings.OneDriveAccessToken);
            isLoggedIn = true;
        }

        public bool IsLoggedIn()
        {
            return isLoggedIn;
        }

        public async Task Logout()
        {
            //Create and send logout request
            string uri = string.Format(AppSettings.OneDriveLogoutUrl, App.Controller.XmlSettings.OneDriveClientId, AppSettings.OneDriveRedirectUrl);
            await httpClient.GetAsync(new Uri(uri));
            //Clear tokens
            App.Controller.XmlSettings.OneDriveAccessToken = "";
            App.Controller.XmlSettings.OneDriveRefreshToken = "";
            isLoggedIn = false;
            httpClient.DefaultRequestHeaders.Clear();
        }

        public int GetNumberOfUploadedPictures()
        {
            return numberUploaded;
        }

        /*******************************************************************************************
        * PRIVATE METHODS
        ********************************************************************************************/
        private async Task uploadPictureToOneDrive(string folderName, string imageName, StorageFile imageFile)
        {
            if (!isLoggedIn)
            {
                throw new Exception("Not logged into OneDrive");
            }

            String uriString = string.Format("{0}/Pictures/{1}/{2}:/content", AppSettings.OneDriveRootUrl, folderName, imageName);

            await SendFileAsync(
                uriString,
                imageFile,
                Windows.Web.Http.HttpMethod.Put
                );
        }

        private async Task<List<string>> listPictures(string folderName)
        {
            String uriString = string.Format("{0}/Pictures/{1}:/children", AppSettings.OneDriveRootUrl, folderName);
            //List to store names of files in the given folder
            List<string> files = null;

            if (isLoggedIn)
            {
                Uri uri = new Uri(uriString);
                try
                {
                    //Create request to list pictures
                    using (HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, uri))
                    using (HttpResponseMessage response = await httpClient.SendRequestAsync(request))
                    {
                        if (response.StatusCode == HttpStatusCode.Ok)
                        {
                            files = new List<string>();
                            //Convert file to stream
                            using (var inputStream = await response.Content.ReadAsInputStreamAsync())
                            using (var memStream = new MemoryStream())
                            using (Stream testStream = inputStream.AsStreamForRead())
                            {
                                await testStream.CopyToAsync(memStream);
                                memStream.Position = 0;
                                using (StreamReader reader = new StreamReader(memStream))
                                {
                                    //Get file name
                                    string result = reader.ReadToEnd();
                                    string[] parts = result.Split('"');
                                    foreach (string part in parts)
                                    {
                                        //Check for image file
                                        if (part.Contains(".jpg"))
                                        {
                                            files.Add(part);
                                        }
                                    }
                                }
                            }
                            return files;
                        }
                        else
                        {
                            Debug.WriteLine("ERROR: " + response.StatusCode + " - " + response.ReasonPhrase);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message);

                    // Log telemetry event about this exception
                    var events = new Dictionary<string, string> { { "OneDrive", ex.Message } };
                    App.Controller.TelemetryClient.TrackEvent("FailedToListPictures", events);
                }
            }
            return null;
        }

        private async Task deletePicture(string folderName, string imageName)
        {
            try
            {
                if (isLoggedIn)
                {
                    String uriString = string.Format("{0}/Pictures/{1}/{2}", AppSettings.OneDriveRootUrl, folderName, imageName);

                    Uri uri = new Uri(uriString);
                    //Create request to delete file
                    using (HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Delete, uri))
                    using (HttpResponseMessage response = await httpClient.SendRequestAsync(request))
                    {
                        //Response status is NoContent after file is deleted successfully
                        if (response.StatusCode != HttpStatusCode.NoContent)
                        {
                            Debug.WriteLine("ERROR: " + response.StatusCode + " - " + response.ReasonPhrase);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);

                // Log telemetry event about this exception
                var events = new Dictionary<string, string> { { "OneDrive", ex.Message } };
                App.Controller.TelemetryClient.TrackEvent("FailedToDeletePicture", events);
            }
        }

        private async Task getTokens(string accessCodeOrRefreshToken, string requestType, string grantType)
        {
            //Get access or refresh tokens for OneDrive authorization
            string uri = AppSettings.OneDriveTokenUrl;
            string content = string.Format(AppSettings.OneDriveTokenContent, App.Controller.XmlSettings.OneDriveClientId, AppSettings.OneDriveRedirectUrl, App.Controller.XmlSettings.OneDriveClientSecret, requestType, accessCodeOrRefreshToken, grantType);
            using (HttpRequestMessage reqMessage = new HttpRequestMessage(HttpMethod.Post, new Uri(uri)))
            {
                reqMessage.Content = new HttpStringContent(content);
                reqMessage.Content.Headers.ContentType = new HttpMediaTypeHeaderValue("application/x-www-form-urlencoded");
                using (HttpResponseMessage responseMessage = await httpClient.SendRequestAsync(reqMessage))
                {
                    responseMessage.EnsureSuccessStatusCode();

                    string responseContentString = await responseMessage.Content.ReadAsStringAsync();
                    App.Controller.XmlSettings.OneDriveAccessToken = getAccessToken(responseContentString);
                    App.Controller.XmlSettings.OneDriveRefreshToken = getRefreshToken(responseContentString);
                }
            }
        }

        private string getAccessToken(string responseContent)
        {
            //Find access token in response and set variable
            string identifier = "\"access_token\":\"";
            int startIndex = responseContent.IndexOf(identifier) + identifier.Length;
            int endIndex = responseContent.IndexOf("\"", startIndex);
            return responseContent.Substring(startIndex, endIndex - startIndex);
        }

        private string getRefreshToken(string responseContentString)
        {
            //Find refresh token in response and set variable
            string identifier = "\"refresh_token\":\"";
            int startIndex = responseContentString.IndexOf(identifier) + identifier.Length;
            int endIndex = responseContentString.IndexOf("\"", startIndex);
            return responseContentString.Substring(startIndex, endIndex - startIndex);
        }

        private void CreateHttpClient(ref HttpClient httpClient)
        {
            ///Create the http client for OneDrive requests
            if (httpClient != null) httpClient.Dispose();
            var filter = new HttpBaseProtocolFilter();
            //Clear cache
            filter.CacheControl.ReadBehavior = HttpCacheReadBehavior.MostRecent;
            filter.CacheControl.WriteBehavior = HttpCacheWriteBehavior.NoCache;
            httpClient = new HttpClient(filter);
        }

        private void SetAuthorization(String scheme, String token)
        {
            //Set the http credentials using authorization tokens
            httpClient.DefaultRequestHeaders.Authorization = new HttpCredentialsHeaderValue(scheme, token);
        }

        private async Task SendFileAsync(String url, StorageFile sFile, HttpMethod httpMethod)
        {
            //Log data for upload attempt
            Windows.Storage.FileProperties.BasicProperties fileProperties = await sFile.GetBasicPropertiesAsync();
            Dictionary<string, string> properties = new Dictionary<string, string> { { "File Size", fileProperties.Size.ToString() } };
            App.Controller.TelemetryClient.TrackEvent("OneDrive picture upload attempt", properties);
            HttpStreamContent streamContent = null;

            try
            {
                //Open file to send as stream
                Stream stream = await sFile.OpenStreamForReadAsync();
                streamContent = new HttpStreamContent(stream.AsInputStream());
                Debug.WriteLine("SendFileAsync() - sending: " + sFile.Path);
            }
            catch (FileNotFoundException ex)
            {
                Debug.WriteLine(ex.Message);

                // Log telemetry event about this exception
                var events = new Dictionary<string, string> { { "OneDrive", ex.Message } };
                App.Controller.TelemetryClient.TrackEvent("FailedToOpenFile", events);
            }
            catch (Exception ex)
            {
                // Log telemetry event about this exception
                var events = new Dictionary<string, string> { { "OneDrive", ex.Message } };
                App.Controller.TelemetryClient.TrackEvent("FailedToOpenFile", events);

                throw new Exception("SendFileAsync() - Cannot open file. Err= " + ex.Message);
            }

            if (streamContent == null)
            {
                //Throw exception if stream is not created
                Debug.WriteLine("  File Path = " + (sFile != null ? sFile.Path : "?"));
                throw new Exception("SendFileAsync() - Cannot open file.");
            }

            try
            {
                Uri resourceAddress = new Uri(url);
                //Create requst to upload file
                using (HttpRequestMessage request = new HttpRequestMessage(httpMethod, resourceAddress))
                {
                    request.Content = streamContent;

                    // Do an asynchronous POST.
                    using (HttpResponseMessage response = await httpClient.SendRequestAsync(request).AsTask(cts.Token))
                    {
                        await DebugTextResultAsync(response);
                        if (response.StatusCode != HttpStatusCode.Created)
                        {
                            throw new Exception("SendFileAsync() - " + response.StatusCode);
                        }

                    }
                }
            }
            catch (TaskCanceledException ex)
            {
                // Log telemetry event about this exception
                var events = new Dictionary<string, string> { { "OneDrive", ex.Message } };
                App.Controller.TelemetryClient.TrackEvent("CancelledFileUpload", events);

                throw new Exception("SendFileAsync() - " + ex.Message);
            }
            catch (Exception ex)
            {
                // This failure will already be logged in telemetry in the enclosing UploadPictures function. We don't want this to be recorded twice.

                throw new Exception("SendFileAsync() - Error: " + ex.Message);
            }
            finally
            {
                streamContent.Dispose();
                Debug.WriteLine("SendFileAsync() - final.");
            }

            App.Controller.TelemetryClient.TrackEvent("OneDrive picture upload success", properties);
        }

        internal async Task DebugTextResultAsync(HttpResponseMessage response)
        {
            //Debug statements
            string Text = SerializeHeaders(response);
            string responseBodyAsText = await response.Content.ReadAsStringAsync().AsTask(cts.Token);
            cts.Token.ThrowIfCancellationRequested();
            responseBodyAsText = responseBodyAsText.Replace("<br>", Environment.NewLine); // Insert new lines.

            Debug.WriteLine("--------------------");
            Debug.WriteLine(Text);
            Debug.WriteLine(responseBodyAsText);
        }

        internal string SerializeHeaders(HttpResponseMessage response)
        {
            //Get headers for debug statements
            StringBuilder output = new StringBuilder();
            output.Append(((int)response.StatusCode) + " " + response.ReasonPhrase + "\r\n");

            SerializeHeaderCollection(response.Headers, output);
            SerializeHeaderCollection(response.Content.Headers, output);
            output.Append("\r\n");
            return output.ToString();
        }

        internal void SerializeHeaderCollection(IEnumerable<KeyValuePair<string, string>> headers, StringBuilder output)
        {
            foreach (var header in headers)
            {
                output.Append(header.Key + ": " + header.Value + "\r\n");
            }
        }

        private async void refreshTimer_Tick(object sender, object e)
        {
            //Refresh OneDrive login tokens every 25 minutes if logged in
            if (isLoggedIn)
            {
                await AuthorizeWithRefreshToken(App.Controller.XmlSettings.OneDriveRefreshToken);

            }
        }
    }
}


