using System;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Linq;
using System.Text;
using Windows.Web.Http;
using Windows.Storage;
using Windows.Security.Cryptography.Core;
using Windows.Security.Cryptography;
using Windows.Storage.Search;


namespace SecuritySystemUWP
{
    public class Azure : IStorage
    {
        //TODO: Add your account name and acount key
        private static string storageAccount = "";
        private static string storageKey = "";

        private static string endpoint;
        private static Mutex uploadPicturesMutexLock = new Mutex();
        public Azure()
        {
            storageAccount = Config.AzureAccountName;
            storageKey = Config.AzureAccessKey;
            endpoint = string.Format(Config.AzureBlobUrl, storageAccount);
        }
        /*******************************************************************************************
        * PUBLIC METHODS
        *******************************************************************************************/

        public Type LoginType()
        {
            return typeof(MainPage);
        }
        public async void UploadPictures(string camera)
        {
            uploadPicturesMutexLock.WaitOne();

            try
            {
                QueryOptions querySubfolders = new QueryOptions();
                querySubfolders.FolderDepth = FolderDepth.Deep;

                StorageFolder cacheFolder = KnownFolders.PicturesLibrary;
                var result = cacheFolder.CreateFileQueryWithOptions(querySubfolders);
                var files = await result.GetFilesAsync();

                foreach (StorageFile file in files)
                {
                    string imageName = string.Format("{0}/{1}_{2}.jpg", camera, DateTime.Now.ToString("MM_dd_yyyy/HH"), DateTime.UtcNow.Ticks.ToString());
                    await uploadPictureToAzure(Config.FolderName, imageName, file);
                    await file.DeleteAsync();
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

        public async void DeleteExpiredPictures(string camera)
        {
            try
            {
                List<string> pictures = await listPictures(Config.FolderName);
                foreach (string picture in pictures)
                {
                    long oldestTime = DateTime.UtcNow.Ticks - TimeSpan.FromDays(Config.StorageDuration).Ticks;
                    string picName = picture.Split('_')[3];
                    if (picName.CompareTo(oldestTime.ToString()) < 0)
                    {
                        await deletePicture(Config.FolderName, picture);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Exception in deleteExpiredPictures() " + ex.Message);
            }
        }


        /*******************************************************************************************
        * PRIVATE METHODS
        ********************************************************************************************/
        private async Task uploadPictureToAzure(string folderPath, string imageName, StorageFile imageFile)
        {
            using (var memStream = new MemoryStream())
            using (Stream testStream = await imageFile.OpenStreamForReadAsync())
            {
                await testStream.CopyToAsync(memStream);
                memStream.Position = 0;

                try
                {
                    Dictionary<string, string> headers = new Dictionary<string, string>();
                    headers.Add("x-ms-blob-type", "BlockBlob");

                    using (HttpRequestMessage request = CreateStreamRESTRequest("PUT", folderPath + "/" + imageName, memStream, headers))
                    using (HttpClient httpClient = new HttpClient())
                    using (HttpResponseMessage response = await httpClient.SendRequestAsync(request))
                    {
                        if (response.StatusCode == HttpStatusCode.Created)
                        {
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
                    throw;
                }
            }
        }
        private async Task<List<string>> listPictures(string folderPath)
        {
            List<string> blobs = null;

            try
            {
                using (HttpRequestMessage request = CreateRESTRequest("GET", folderPath + "?restype=container&comp=list&include=snapshots&include=metadata"))
                using (HttpClient httpClient = new HttpClient())
                using (HttpResponseMessage response = await httpClient.SendRequestAsync(request))
                {
                    if ((int)response.StatusCode == 200)
                    {
                        blobs = new List<string>();
                        using (var inputStream = await response.Content.ReadAsInputStreamAsync())
                        using (var memStream = new MemoryStream())
                        using (Stream testStream = inputStream.AsStreamForRead())
                        {
                            await testStream.CopyToAsync(memStream);
                            memStream.Position = 0;
                            using (StreamReader reader = new StreamReader(memStream))
                            {
                                string result = reader.ReadToEnd();

                                XElement x = XElement.Parse(result);
                                foreach (XElement blob in x.Element("Blobs").Elements("Blob"))
                                {
                                    blobs.Add(blob.Element("Name").Value);
                                }
                            }
                        }
                    }
                    else
                    {
                        Debug.WriteLine("ERROR: " + response.StatusCode + " - " + response.ReasonPhrase);
                    }
                }
                return blobs;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                return null;
                throw;
            }
        }

        private async Task deletePicture(string folderPath, string imageName)
        {
            try
            {
                using (HttpRequestMessage request = CreateStreamRESTRequest("DELETE", folderPath + "/" + imageName))
                using (HttpClient httpClient = new HttpClient())
                using (HttpResponseMessage response = await httpClient.SendRequestAsync(request))
                {
                    if (response.StatusCode != HttpStatusCode.Accepted)
                    {
                        Debug.WriteLine("ERROR: " + response.StatusCode + " - " + response.ReasonPhrase);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                throw;
            }
        }

        private HttpRequestMessage CreateRESTRequest(string method, string resource, string requestBody = null, Dictionary<string, string> headers = null, string ifMatch = "", string md5 = "")
        {
            byte[] byteArray = null;
            DateTime now = DateTime.UtcNow;
            Uri uri = new Uri(endpoint + resource);
            HttpMethod httpMethod = new HttpMethod(method);
            int contentLength = 0;
         
            HttpRequestMessage request = new HttpRequestMessage(httpMethod, uri);
            request.Headers.Add("x-ms-date", now.ToString("R", System.Globalization.CultureInfo.InvariantCulture));
            request.Headers.Add("x-ms-version", "2009-09-19");

            if (headers != null)
            {
                foreach (KeyValuePair<string, string> header in headers)
                {
                    request.Headers.Add(header.Key, header.Value);
                }
            }

            if (!String.IsNullOrEmpty(requestBody))
            {
                request.Headers.Add("Accept-Charset", "UTF-8");

                byteArray = Encoding.UTF8.GetBytes(requestBody);
                using (MemoryStream stream = new MemoryStream(byteArray))
                using (Windows.Storage.Streams.IInputStream streamContent = stream.AsInputStream())
                using (HttpStreamContent content = new HttpStreamContent(streamContent))
                {
                    request.Content = content;
                    contentLength = byteArray.Length;
                }
            }

            var authorizationHeader = AuthorizationHeader(method, now, request, contentLength, ifMatch, md5);
            request.Headers.Authorization = authorizationHeader;

            return request;
        }

        private HttpRequestMessage CreateStreamRESTRequest(string method, string resource, MemoryStream requestBody = null, Dictionary<string, string> headers = null, string ifMatch = "", string md5 = "")
        {
            DateTime now = DateTime.UtcNow;
            Uri uri = new Uri(endpoint + resource);
            HttpMethod httpMethod = new HttpMethod(method);
            long contentLength = 0;

            HttpRequestMessage request = new HttpRequestMessage(httpMethod, uri);
            request.Headers.Add("x-ms-date", now.ToString("R", System.Globalization.CultureInfo.InvariantCulture));
            request.Headers.Add("x-ms-version", "2009-09-19");

            if (null != headers)
            {
                foreach (KeyValuePair<string, string> header in headers)
                {
                    request.Headers.Add(header.Key, header.Value);
                }
            }

            if (null != requestBody)
            {
                request.Headers.Add("Accept-Charset", "UTF-8");

                Windows.Storage.Streams.IInputStream streamContent = requestBody.AsInputStream();
                HttpStreamContent content = new HttpStreamContent(streamContent);
                request.Content = content;

                contentLength = requestBody.Length;
            }

            var authorizationHeader = AuthorizationHeader(method, now, request, contentLength, ifMatch, md5);
            request.Headers.Authorization = authorizationHeader;

            return request;
        }


        private Windows.Web.Http.Headers.HttpCredentialsHeaderValue AuthorizationHeader(string method, DateTime now, HttpRequestMessage request, long contentLength, string ifMatch = "", string md5 = "")
        {
            string MessageSignature = String.Format("{0}\n\n\n{1}\n{5}\n\n\n\n{2}\n\n\n\n{3}{4}",
                    method,
                    (method == "GET" || method == "HEAD") ? String.Empty : contentLength.ToString(),
                    ifMatch,
                    GetCanonicalizedHeaders(request),
                    GetCanonicalizedResource(request.RequestUri, storageAccount),
                    md5
                    );

            var key = CryptographicBuffer.DecodeFromBase64String(storageKey);
            var msg = CryptographicBuffer.ConvertStringToBinary(MessageSignature, BinaryStringEncoding.Utf8);

            MacAlgorithmProvider objMacProv = MacAlgorithmProvider.OpenAlgorithm(MacAlgorithmNames.HmacSha256);
            CryptographicHash hash = objMacProv.CreateHash(key);
            hash.Append(msg);
            var authorizationHeader = new Windows.Web.Http.Headers.HttpCredentialsHeaderValue("SharedKey", storageAccount + ":" + CryptographicBuffer.EncodeToBase64String(hash.GetValueAndReset()));
            return authorizationHeader;
        }
        public string GetCanonicalizedHeaders(HttpRequestMessage request)
        {
            List<string> headerNameList = new List<string>();
            StringBuilder sb = new StringBuilder();
            foreach (string headerName in request.Headers.Keys)
            {
                if (headerName.ToLowerInvariant().StartsWith("x-ms-", StringComparison.Ordinal))
                {
                    headerNameList.Add(headerName.ToLowerInvariant());
                }
            }
            headerNameList.Sort();
            foreach (string headerName in headerNameList)
            {
                StringBuilder builder = new StringBuilder(headerName);
                string separator = ":";
                foreach (string headerValue in GetHeaderValues(request.Headers, headerName))
                {
                    string trimmedValue = headerValue.Replace("\r\n", String.Empty);
                    builder.Append(separator);
                    builder.Append(trimmedValue);
                    separator = ",";
                }
                sb.Append(builder.ToString());
                sb.Append("\n");
            }
            return sb.ToString();
        }
        public List<string> GetHeaderValues(Windows.Web.Http.Headers.HttpRequestHeaderCollection headers, string headerName)
        {
            List<string> list = new List<string>();

            List<KeyValuePair<string, string>> headerList = headers.ToList();
            List<string> values = headerList.Where(kvp => kvp.Key == headerName).Select(kvp => kvp.Value).Distinct().ToList();
            foreach (string str in values)
            {
                list.Add(str.TrimStart(null));
            }
            return list;
        }

        public string GetCanonicalizedResource(Uri address, string accountName)
        {
            StringBuilder str = new StringBuilder();
            StringBuilder builder = new StringBuilder("/");
            builder.Append(accountName);
            builder.Append(address.AbsolutePath);
            str.Append(builder.ToString());
            Dictionary<string, string> values2 = new Dictionary<string, string>();

            List<KeyValuePair<string, string>> values = new List<KeyValuePair<string, string>>();

            // Split the address query string into components
            string[] querySegments = address.Query.Split('&');
            foreach (string segment in querySegments)
            {
                string[] parts = segment.Split('=');
                if (parts.Length > 1)
                {
                    string key = parts[0].Trim(new char[] { '?', ' ' });
                    string val = parts[1].Trim();

                    values.Add(new KeyValuePair<string, string>(key, val));
                }
            }

            foreach (string str2 in values.Select(kvp => kvp.Key).Distinct())
            {
                List<string> list = values.Where(kvp => kvp.Key == str2).Select(kvp => kvp.Value).ToList();
                list.Sort();
                StringBuilder builder2 = new StringBuilder();
                foreach (object obj2 in list)
                {
                    if (builder2.Length > 0)
                    {
                        builder2.Append(",");
                    }
                    builder2.Append(obj2.ToString());
                }
                values2.Add((str2 == null) ? str2 : str2.ToLowerInvariant(), builder2.ToString());
            }

            List<string> list2 = new List<string>(values2.Keys);
            list2.Sort();
            foreach (string str3 in list2)
            {
                StringBuilder builder3 = new StringBuilder(string.Empty);
                builder3.Append(str3);
                builder3.Append(":");
                builder3.Append(values2[str3]);
                str.Append("\n");
                str.Append(builder3.ToString());
            }
            return str.ToString();
        }
    }
}

