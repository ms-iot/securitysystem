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

namespace SecuritySystemUWP
{
    public class OneDrive : IStorage
    {
        //TODO: Add your account name and acount key
        public static string clientId = "";
        private static string clientSecret = "";

        //Obtained during onedrive login
        private static String accessToken = "";
        private static String refreshToken = "";
        internal const string scope = "wl.offline_access onedrive.readwrite";
        internal const string redirectUri = "https://login.live.com/oauth20_desktop.srf";

        private static HttpClient httpClient;
        private static CancellationTokenSource cts;
        private static bool isLoggedin = false;

        public OneDrive(string accountId, string accountSecret)
        {
            clientId = accountId;
            clientSecret = accountSecret;
        }
        /*******************************************************************************************
        * PUBLIC METHODS
        *******************************************************************************************/

        public Type loginType()
        {
            return typeof(OnedriveLoginPage);
        }
        public async Task<bool> uploadPicture(string folderName, string imageName, StorageFile imageFile)
        {
            try
            {
                if (isLoggedin)
                {
                    String url = "https://api.onedrive.com/v1.0/drive/root:" + "/Pictures/" + folderName + "/" + imageName + ":/content";

                    await SendFileAsync(
                        url,  // example: "https://api.onedrive.com/v1.0/drive/root:/Documents/test.jpg:/content"
                        imageFile,
                        Windows.Web.Http.HttpMethod.Put
                        );
                    return true;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Exception in uploading pictures to OneDrive: " + ex.Message);
            }
            return false;
        }

        public async Task<List<string>> listPictures(string folderName)
        {
            String uriString = "https://api.onedrive.com/v1.0/drive/root:" + "/Pictures/" + folderName + ":/children";
            List<string> files = new List<string>();

            try
            {
                if (isLoggedin)
                {
                    Uri uri = new Uri(uriString);
                    HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, uri);
                    HttpResponseMessage response = await httpClient.SendRequestAsync(request);

                    if (response.StatusCode == HttpStatusCode.Ok)
                    {
                        var inputStream = await response.Content.ReadAsInputStreamAsync();
                        var memStream = new MemoryStream();
                        Stream testStream = inputStream.AsStreamForRead();
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
                    else
                    {
                        Debug.WriteLine("ERROR: " + response.StatusCode + " - " + response.ReasonPhrase);
                        return null;
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
        public async Task<bool> deletePicture(string folderName, string imageName)
        {
            try
            {
                if (isLoggedin)
                {
                    String uriString = "https://api.onedrive.com/v1.0/drive/root:" + "/Pictures/" + folderName + "/" + imageName;

                    Uri uri = new Uri(uriString);
                    HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Delete, uri);
                    HttpResponseMessage response = await httpClient.SendRequestAsync(request);
                    if (response.StatusCode == HttpStatusCode.NoContent)
                    {
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
            return false;
        }
        public static async Task authorize(string accessCode)
        {
            CreateHttpClient(ref httpClient);

            await getTokens(accessCode, "authorization_code");
            SetAuthorization("Bearer", accessToken);

            cts = new CancellationTokenSource();
            isLoggedin = true;
        }
        /*******************************************************************************************
        * PRIVATE METHODS
        ********************************************************************************************/
        private static async Task getTokens(string accessCode, string grantType)
        {
            string uri = "https://login.live.com/oauth20_token.srf";
            string content = getRequestContentString(accessCode, grantType);
            HttpClient client = new HttpClient();

            HttpRequestMessage reqMessage = new HttpRequestMessage(HttpMethod.Post, new Uri(uri));
            reqMessage.Content = new HttpStringContent(content);
            reqMessage.Content.Headers.ContentType = new HttpMediaTypeHeaderValue("application/x-www-form-urlencoded");
            HttpResponseMessage responseMessage = await client.SendRequestAsync(reqMessage);

            //TODO: catch errors more cleanly
            responseMessage.EnsureSuccessStatusCode();

            string responseContentString = await responseMessage.Content.ReadAsStringAsync();
            accessToken = getAccessToken(responseContentString);
            refreshToken = getRefreshToken(responseContentString);

        }

        private static string getRequestContentString(string accessCode, string grantType)
        {
            string contentString = "";
            if (grantType.Equals("authorization_code"))
            {
                contentString = "client_id=" + clientId + "&redirect_uri=" + redirectUri + "&client_secret=" + clientSecret + "&code=" + accessCode + "&grant_type=authorization_code";
            }
            else if (grantType.Equals("refresh_token"))
            {
                contentString = "client_id=" + clientId + "&redirect_uri=" + redirectUri + "&client_secret=" + clientSecret + "&refresh_token" + refreshToken + "&grant_type=refresh_token";
            }

            return contentString;
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

            if (!isLoggedin)
            {
                return;
            }

            await getTokens("", "refresh_token");
        }

        private static async Task logout()
        {
            string uri = "https://login.live.com/oauth20_logout.srf?client_id=" + clientId + "&redirect_uri=" + redirectUri;
            HttpClient client = new HttpClient();
            await client.GetAsync(new Uri(uri));
            accessToken = "";
            refreshToken = "";
            isLoggedin = false;
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
            HttpStreamContent streamContent = null;
            try
            {
                Stream stream = await sFile.OpenStreamForReadAsync();
                streamContent = new HttpStreamContent(stream.AsInputStream());
                Debug.WriteLine("SendFileAsync() - sending: " + sFile.Path);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("SendFileAsync() - Cannot open file. Err= " + ex.Message);
                Debug.WriteLine("  File Path = " + (sFile != null ? sFile.Path : "?"));
            }
            if (streamContent == null) return;

            try
            {
                Uri resourceAddress = new Uri(url);
                HttpRequestMessage request = new HttpRequestMessage(httpMethod, resourceAddress);
                request.Content = streamContent;

                // Do an asynchronous POST.
                HttpResponseMessage response = await httpClient.SendRequestAsync(request).AsTask(cts.Token);

                await DebugTextResultAsync(response);
            }
            catch (TaskCanceledException ex)
            {
                Debug.WriteLine("SendFileAsync() - Request canceled.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine("SendFileAsync() - Error: " + ex.Message);
            }
            finally
            {
                Debug.WriteLine("SendFileAsync() - final.");
            }
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
    }
}


