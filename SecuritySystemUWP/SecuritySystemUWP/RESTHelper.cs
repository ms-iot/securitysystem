using System;
using System.Collections.Generic;
using System.Collections;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading;
using Windows.Web.Http;
using System.Xml;
using System.Xml.Linq;
using Windows.Security.Cryptography.Core;
using Windows.Security.Cryptography;
using System.Diagnostics;

namespace SecuritySystemUWP
{
    public class RESTHelper
    {
        protected bool IsTableStorage { get; set; }

        private string endpoint;
        public string Endpoint
        {
            get
            {
                return endpoint;
            }
            internal set
            {
                endpoint = value;
            }
        }

        private string storageAccount;
        public string StorageAccount
        {
            get
            {
                return storageAccount;
            }
            internal set
            {
                storageAccount = value;
            }
        }

        private string storageKey;
        public string StorageKey
        {
            get
            {
                return storageKey;
            }
            internal set
            {
                storageKey = value;
            }
        }


        public RESTHelper(string endpoint, string storageAccount, string storageKey)
        {
            this.Endpoint = endpoint;
            this.StorageAccount = storageAccount;
            this.StorageKey = storageKey;
        }


        #region REST HTTP Request Helper Methods

        // Construct and issue a REST request and return the response.

        public HttpRequestMessage CreateRESTRequest(string method, string resource, string requestBody = null, Dictionary<string, string> headers = null,
            string ifMatch = "", string md5 = "")
        {
            byte[] byteArray = null;
            DateTime now = DateTime.UtcNow;
            Uri uri = new Uri(Endpoint + resource);
            HttpMethod httpMethod = new HttpMethod(method);
            int contentLength = 0;

            var httpClient = new HttpClient();
            HttpRequestMessage request = new HttpRequestMessage(httpMethod, uri);
            request.Headers.Add("x-ms-date", now.ToString("R", System.Globalization.CultureInfo.InvariantCulture));
            request.Headers.Add("x-ms-version", "2009-09-19");
            //Debug.WriteLine(now.ToString("R", System.Globalization.CultureInfo.InvariantCulture));

            if (IsTableStorage)
            {
                request.Content.Headers.ContentType = new Windows.Web.Http.Headers.HttpMediaTypeHeaderValue("application/atom+xml");

                request.Headers.Add("DataServiceVersion", "1.0;NetFx");
                request.Headers.Add("MaxDataServiceVersion", "1.0;NetFx");
            }

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
                MemoryStream stream = new MemoryStream(byteArray);
                Windows.Storage.Streams.IInputStream streamContent = stream.AsInputStream();
                HttpStreamContent content = new HttpStreamContent(streamContent);
                request.Content = content;

                contentLength = byteArray.Length;
            }

            var authorizationHeader = AuthorizationHeader(method, now, request, contentLength, ifMatch, md5);
            request.Headers.Authorization = authorizationHeader;

            return request;
        }

        public HttpRequestMessage CreateStreamRESTRequest(string method, string resource, MemoryStream requestBody = null, Dictionary<string, string> headers = null,
            string ifMatch = "", string md5 = "")
        {
            DateTime now = DateTime.UtcNow;
            Uri uri = new Uri(Endpoint + resource);
            HttpMethod httpMethod = new HttpMethod(method);
            long contentLength = 0;

            var httpClient = new HttpClient();
            HttpRequestMessage request = new HttpRequestMessage(httpMethod, uri);
            request.Headers.Add("x-ms-date", now.ToString("R", System.Globalization.CultureInfo.InvariantCulture));
            request.Headers.Add("x-ms-version", "2009-09-19");
            //Debug.WriteLine(now.ToString("R", System.Globalization.CultureInfo.InvariantCulture));

            if (IsTableStorage)
            {
                request.Content.Headers.ContentType = new Windows.Web.Http.Headers.HttpMediaTypeHeaderValue("application/atom+xml");

                request.Headers.Add("DataServiceVersion", "1.0;NetFx");
                request.Headers.Add("MaxDataServiceVersion", "1.0;NetFx");
            }

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



        // Generate an authorization header.

        public Windows.Web.Http.Headers.HttpCredentialsHeaderValue AuthorizationHeader(string method, DateTime now, HttpRequestMessage request, long contentLength, string ifMatch = "", string md5 = "")
        {
            string MessageSignature;

            if (IsTableStorage)
            {
                MessageSignature = String.Format("{0}\n\n{1}\n{2}\n{3}",
                    method,
                    "application/atom+xml",
                    now.ToString("R", System.Globalization.CultureInfo.InvariantCulture),
                    GetCanonicalizedResource(request.RequestUri, StorageAccount)
                    );
            }
            else
            {
                MessageSignature = String.Format("{0}\n\n\n{1}\n{5}\n\n\n\n{2}\n\n\n\n{3}{4}",
                    method,
                    (method == "GET" || method == "HEAD") ? String.Empty : contentLength.ToString(),
                    ifMatch,
                    GetCanonicalizedHeaders(request),
                    GetCanonicalizedResource(request.RequestUri, StorageAccount),
                    md5
                    );
            }

            //Debug.WriteLine(MessageSignature);
            var key = CryptographicBuffer.DecodeFromBase64String(StorageKey);
            var msg = CryptographicBuffer.ConvertStringToBinary(MessageSignature, BinaryStringEncoding.Utf8);

            MacAlgorithmProvider objMacProv = MacAlgorithmProvider.OpenAlgorithm(MacAlgorithmNames.HmacSha256);
            //CryptographicKey cryptKey = objMacProv.CreateKey(key);
            //var buff = CryptographicEngine.Sign(cryptKey, msg);
            CryptographicHash hash = objMacProv.CreateHash(key);
            hash.Append(msg);

            var authorizationHeader = new Windows.Web.Http.Headers.HttpCredentialsHeaderValue("SharedKey", StorageAccount + ":" + CryptographicBuffer.EncodeToBase64String(hash.GetValueAndReset()));
            //Debug.WriteLine(authorizationHeader.ToString());
            return authorizationHeader;
        }

        // Get canonicalized headers.

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

        // Get header values.

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

        // Get canonicalized resource.

        public string GetCanonicalizedResource(Uri address, string accountName)
        {
            StringBuilder str = new StringBuilder();
            StringBuilder builder = new StringBuilder("/");
            builder.Append(accountName);
            builder.Append(address.AbsolutePath);
            str.Append(builder.ToString());
            Dictionary<string, string> values2 = new Dictionary<string, string>();

            if (!IsTableStorage)
            {
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

        #endregion

        //#region Retry Delegate

        //public delegate T RetryDelegate<T>();
        //public delegate void RetryDelegate();

        //const int retryCount = 3;
        //const int retryIntervalMS = 200;

        //// Retry delegate with default retry settings.

        //public static T Retry<T>(RetryDelegate<T> del)
        //{
        //    return Retry<T>(del, retryCount, retryIntervalMS);
        //}

        //// Retry delegate.

        //public static T Retry<T>(RetryDelegate<T> del, int numberOfRetries, int msPause)
        //{
        //    int counter = 0;
        //    RetryLabel:

        //    try
        //    {
        //        counter++;
        //        return del.Invoke();
        //    }
        //    catch (Exception ex)
        //    {
        //        if (counter > numberOfRetries)
        //        {
        //            throw ex;
        //        }
        //        else
        //        {
        //            if (msPause > 0)
        //            {
        //                Thread.Sleep(msPause);
        //            }
        //            goto RetryLabel;
        //        }
        //    }
        //}


        //// Retry delegate with default retry settings.

        //public static bool Retry(RetryDelegate del)
        //{
        //    return Retry(del, retryCount, retryIntervalMS);
        //}


        //public static bool Retry(RetryDelegate del, int numberOfRetries, int msPause)
        //{
        //    int counter = 0;

        //    RetryLabel:
        //    try
        //    {
        //        counter++;
        //        del.Invoke();
        //        return true;
        //    }
        //    catch (Exception ex)
        //    {
        //        if (counter > numberOfRetries)
        //        {
        //            throw ex;
        //        }
        //        else
        //        {
        //            if (msPause > 0)
        //            {
        //                Thread.Sleep(msPause);
        //            }
        //            goto RetryLabel;
        //        }
        //    }
        //}

        //#endregion
    }
}