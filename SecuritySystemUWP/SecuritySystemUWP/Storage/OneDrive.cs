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
        private static HttpClient httpClient;
        private static CancellationTokenSource cts;
        private static bool isLoggedIn = false;
        private static Mutex uploadPicturesMutexLock = new Mutex();
        private DispatcherTimer refreshTimer;
                
        private static int numberUploaded = 0;


        /*******************************************************************************************
        * PUBLIC METHODS
        *******************************************************************************************/
        public OneDrive()
        {
            refreshTimer = new DispatcherTimer();
            refreshTimer.Interval = TimeSpan.FromMinutes(25);
            refreshTimer.Tick += refreshTimer_Tick;
            refreshTimer.Start();
        }

        public Type StorageStartPage()
        {
            return typeof(OnedriveLoginPage);
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
                    cacheFolder = await cacheFolder.GetFolderAsync(App.XmlSettings.FolderName);
                var result = cacheFolder.CreateFileQueryWithOptions(querySubfolders);
                var files = await result.GetFilesAsync();

                foreach (StorageFile file in files)
                {
                    string imageName = string.Format(AppSettings.ImageNameFormat, camera, DateTime.Now.ToString("MM_dd_yyyy/HH"), DateTime.UtcNow.Ticks.ToString());
                        try
                        {
                            await uploadPictureToOneDrive(App.XmlSettings.FolderName, imageName, file);
                            numberUploaded++;

                            // uploadPictureToOnedrive should throw an exception if it fails, so it's safe to delete
                    await file.DeleteAsync();
                }
                        catch (Exception e)
                        {
                            Debug.WriteLine(e.Message);
            }
                    }
                }
            catch (Exception ex)
            {
                Debug.WriteLine("Exception in uploadPictures() " + ex.Message);
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
                string folder = string.Format("{0}/{1}/{2}", App.XmlSettings.FolderName, camera, DateTime.Now.Subtract(TimeSpan.FromDays(App.XmlSettings.StorageDuration)).ToString("MM_dd_yyyy"));
                List<string> pictures = await listPictures(folder);
                foreach (string picture in pictures)
                {
                   await deletePicture(folder, picture);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Exception in deleteExpiredPictures() " + ex.Message);
            }
        }

        public static async Task Authorize(string accessCode)
        {
            CreateHttpClient(ref httpClient);
            await getTokens(accessCode, "code", "authorization_code");
            SetAuthorization("Bearer", App.XmlSettings.OneDriveAccessToken);
            cts = new CancellationTokenSource();
            isLoggedIn = true;
        }

        public static async Task AuthorizeWithRefreshToken(string refreshToken)
        {
            CreateHttpClient(ref httpClient);
            await getTokens(refreshToken, "refresh_token", "refresh_token");
            SetAuthorization("Bearer", App.XmlSettings.OneDriveAccessToken);
            cts = new CancellationTokenSource();
            isLoggedIn = true;
        }

        public static bool IsLoggedIn()
        {
            return isLoggedIn;
        }

        public static async Task Logout()
        {
            string uri = string.Format(AppSettings.OneDriveLogoutUrl, App.XmlSettings.OneDriveClientId, AppSettings.OneDriveRedirectUrl);
            await httpClient.GetAsync(new Uri(uri));
            App.XmlSettings.OneDriveAccessToken = "";
            App.XmlSettings.OneDriveRefreshToken = "";
            isLoggedIn = false;
            httpClient.Dispose();
        }

        public static int GetNumberOfUploadedPictures()
        {
            return numberUploaded;
        }

        /*******************************************************************************************
        * PRIVATE METHODS
        ********************************************************************************************/
        private async Task uploadPictureToOneDrive(string folderName, string imageName, StorageFile imageFile)
            {
            if (isLoggedIn)
                {
                    String uriString = string.Format("{0}/Pictures/{1}/{2}:/content", AppSettings.OneDriveRootUrl, folderName, imageName);

                    await SendFileAsync(
                        uriString, 
                        imageFile,
                        Windows.Web.Http.HttpMethod.Put
                        );
                }
            else
            {
                throw new Exception("Not logged into OneDrive");
            }
        }

        private async Task<List<string>> listPictures(string folderName)
        {
            String uriString = string.Format("{0}/Pictures/{1}:/children", AppSettings.OneDriveRootUrl, folderName);
            List<string> files = null;
            try
            {
                if (isLoggedIn)
                {
                    Uri uri = new Uri(uriString);
                    using (HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, uri))
                    using (HttpResponseMessage response = await httpClient.SendRequestAsync(request))
                    {
                        if (response.StatusCode == HttpStatusCode.Ok)
                        {
                            files = new List<string>();
                            using (var inputStream = await response.Content.ReadAsInputStreamAsync())
                            using (var memStream = new MemoryStream())
                            using (Stream testStream = inputStream.AsStreamForRead())
                            {
                                await testStream.CopyToAsync(memStream);
                                memStream.Position = 0;
                                using (StreamReader reader = new StreamReader(memStream))
                                {
                                    string result = reader.ReadToEnd();
                                    string[] parts = result.Split('"');
                                    foreach (string part in parts)
                                    {
                                        if (part.Contains(".jpg"))
                                        {
                                            files.Add(part);
                                        }
                                    }
                                }
                            }
                        }
                        else
                        {
                            Debug.WriteLine("ERROR: " + response.StatusCode + " - " + response.ReasonPhrase);
                        }
                    }
                    return files;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
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
                    using (HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Delete, uri))
                    using (HttpResponseMessage response = await httpClient.SendRequestAsync(request))
                    {
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
            }
        }

        private static async Task getTokens(string accessCodeOrRefreshToken, string requestType, string grantType)
        {
            
            string uri = AppSettings.OneDriveTokenUrl;
            string content = string.Format(AppSettings.OneDriveTokenContent, App.XmlSettings.OneDriveClientId, AppSettings.OneDriveRedirectUrl, App.XmlSettings.OneDriveClientSecret, requestType, accessCodeOrRefreshToken, grantType);
            using (HttpClient client = new HttpClient())
            using (HttpRequestMessage reqMessage = new HttpRequestMessage(HttpMethod.Post, new Uri(uri)))
            {
                reqMessage.Content = new HttpStringContent(content);
                reqMessage.Content.Headers.ContentType = new HttpMediaTypeHeaderValue("application/x-www-form-urlencoded");
                using (HttpResponseMessage responseMessage = await client.SendRequestAsync(reqMessage))
                {
                    responseMessage.EnsureSuccessStatusCode();

                    string responseContentString = await responseMessage.Content.ReadAsStringAsync();
                    App.XmlSettings.OneDriveAccessToken = getAccessToken(responseContentString);
                    App.XmlSettings.OneDriveRefreshToken = getRefreshToken(responseContentString);
                }
            }
        }

        private static string getAccessToken(string responseContent)
        {
            string identifier = "\"access_token\":\"";
            int startIndex = responseContent.IndexOf(identifier) + identifier.Length;
            int endIndex = responseContent.IndexOf("\"", startIndex);
            return responseContent.Substring(startIndex, endIndex - startIndex);
        }

        private static string getRefreshToken(string responseContentString)
        {
            string identifier = "\"refresh_token\":\"";
            int startIndex = responseContentString.IndexOf(identifier) + identifier.Length;
            int endIndex = responseContentString.IndexOf("\"", startIndex);
            return responseContentString.Substring(startIndex, endIndex - startIndex);
        }

        /*
        Reauthorizes the application with the User's onedrive.
        The initially obtained access token can expire, so it is safe to refresh for a new token before attempting to upload
        */
        private static async Task reauthorize()
        {
            if (!isLoggedIn)
            {
                return;
            }

            await getTokens(App.XmlSettings.OneDriveRefreshToken, "refresh_token", "refresh_token");
        }

        private static void CreateHttpClient(ref HttpClient httpClient)
        {
            if (httpClient != null) httpClient.Dispose();
            var filter = new HttpBaseProtocolFilter();
            filter.CacheControl.ReadBehavior = HttpCacheReadBehavior.MostRecent;
            filter.CacheControl.WriteBehavior = HttpCacheWriteBehavior.NoCache;
            httpClient = new HttpClient(filter);
        }

        private static void SetAuthorization(String scheme, String token)
        {
            if (httpClient == null) return;
            httpClient.DefaultRequestHeaders.Authorization = new HttpCredentialsHeaderValue(scheme, token);
        }

        private static async Task SendFileAsync(String url, StorageFile sFile, HttpMethod httpMethod)
        {
            Windows.Storage.FileProperties.BasicProperties fileProperties = await sFile.GetBasicPropertiesAsync();
            Dictionary<string, string> properties = new Dictionary<string, string> { { "File Size", fileProperties.Size.ToString() } };
            App.TelemetryClient.TrackEvent("OneDrive picture upload attempt", properties);
            HttpStreamContent streamContent = null;

            try
            {
                Stream stream = await sFile.OpenStreamForReadAsync();
                streamContent = new HttpStreamContent(stream.AsInputStream());
                Debug.WriteLine("SendFileAsync() - sending: " + sFile.Path);
            }
            catch (Exception ex)
            {
                throw new Exception("SendFileAsync() - Cannot open file. Err= " + ex.Message);
            }

            if (streamContent == null)
            {
                Debug.WriteLine("  File Path = " + (sFile != null ? sFile.Path : "?"));
                streamContent.Dispose();
                throw new Exception("SendFileAsync() - Cannot open file.");
            }

            try
            {
                Uri resourceAddress = new Uri(url);
                using (HttpRequestMessage request = new HttpRequestMessage(httpMethod, resourceAddress))
                {
                    request.Content = streamContent;

                    // Do an asynchronous POST.
                    using (HttpResponseMessage response = await httpClient.SendRequestAsync(request).AsTask(cts.Token))
                    {
                        await DebugTextResultAsync(response);
                    }
                }
            }
            catch (TaskCanceledException ex)
            {
                throw ex;
            }
            catch (Exception ex)
            {
                throw new Exception("SendFileAsync() - Error: " + ex.Message);
            }
            finally
            {
                streamContent.Dispose();
                Debug.WriteLine("SendFileAsync() - final.");
            }

            App.TelemetryClient.TrackEvent("OneDrive picture upload success", properties);
        }

        internal static async Task DebugTextResultAsync(HttpResponseMessage response)
        {
            string Text = SerializeHeaders(response);
            string responseBodyAsText = await response.Content.ReadAsStringAsync().AsTask(cts.Token);
            cts.Token.ThrowIfCancellationRequested();
            responseBodyAsText = responseBodyAsText.Replace("<br>", Environment.NewLine); // Insert new lines.

            Debug.WriteLine("--------------------");
            Debug.WriteLine(Text);
            Debug.WriteLine(responseBodyAsText);
        }

        internal static string SerializeHeaders(HttpResponseMessage response)
        {
            StringBuilder output = new StringBuilder();
            output.Append(((int)response.StatusCode) + " " + response.ReasonPhrase + "\r\n");

            SerializeHeaderCollection(response.Headers, output);
            SerializeHeaderCollection(response.Content.Headers, output);
            output.Append("\r\n");
            return output.ToString();
        }

        internal static void SerializeHeaderCollection(IEnumerable<KeyValuePair<string, string>> headers, StringBuilder output)
        {
            foreach (var header in headers)
            {
                output.Append(header.Key + ": " + header.Value + "\r\n");
            }
        }

        private async void refreshTimer_Tick(object sender, object e)
        {
            await reauthorize();
        }

    }
}


