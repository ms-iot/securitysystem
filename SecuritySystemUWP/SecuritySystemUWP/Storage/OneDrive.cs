using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using Microsoft.Maker.Storage.OneDrive;
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
        private const int MaxTries = 3;
        private int numberUploaded = 0;
        private DateTime lastUploadTime = DateTime.MinValue;
        private OneDriveConnector oneDriveConnector;

        /*******************************************************************************************
* PUBLIC METHODS
*******************************************************************************************/
        public OneDrive()
        {
            oneDriveConnector = new OneDriveConnector();
            oneDriveConnector.TokensChangedEvent += SaveTokens;
        }

        public void Dispose()
        {
            oneDriveConnector.LogoutAsync();
        }

        public DateTime LastUploadTime
        {
            get
            {
                return this.lastUploadTime;
            }
        }

        public async void UploadPictures()
        {
            if (oneDriveConnector.isLoggedIn)
            {
                // Stop timer to allow time for uploading pictures in case next timer tick overlaps with this ongoing one
                AppController.uploadPicturesTimer.Stop();

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
                        try
                        {
                            await uploadWithRetry(file);
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine("UploadPictures(): " + ex.Message);

                            // Log telemetry event about this exception
                            var events = new Dictionary<string, string> { { "OneDrive", ex.Message } };
                            events.Add("File Failure", "File name: " + file.Name);
                            TelemetryHelper.TrackEvent("FailedToUploadPicture", events);
                            TelemetryHelper.TrackException(ex);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Exception in UploadPictures() " + ex.Message);

                    // Log telemetry event about this exception
                    var events = new Dictionary<string, string> { { "OneDrive", ex.Message } };
                    events.Add("Setup routine failure", "Exception thrown getting file list from pictures library");
                    TelemetryHelper.TrackEvent("FailedToUploadPicture", events);
                    TelemetryHelper.TrackException(ex);
                }
                finally
                {
                    AppController.uploadPicturesTimer.Start();
                }
            }
        }

        public async void DeleteExpiredPictures()
        {
            try
            {
                string folder = string.Format(AppSettings.ImageNameFormat, App.Controller.XmlSettings.OneDriveFolderPath,  DateTime.Now.Subtract(TimeSpan.FromDays(App.Controller.XmlSettings.StorageDuration)).ToString("yyyy_MM_dd"));
                //List pictures in old day folder
                KeyValuePair<HttpResponseMessage, IList<string>> responseItems = await oneDriveConnector.ListFilesAsync(folder);
                List<string> pictures = new List<string>(responseItems.Value);
                if (pictures != null)
                {
                    //Delete all pictures from the day
                    foreach (string picture in pictures)
                    {
                        try
                        {
                            await deleteWithRetry(picture, folder);
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine("Exception in deleteExpiredPictures() " + ex.Message);

                            // Log telemetry event about this exception
                            var events = new Dictionary<string, string> { { "OneDrive", ex.Message } };
                            events.Add("File Failure", "File name: " + picture);
                            TelemetryHelper.TrackEvent("FailedToDeletePicture", events);
                            TelemetryHelper.TrackException(ex);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Exception in deleteExpiredPictures() " + ex.Message);

                // Log telemetry event about this exception
                var events = new Dictionary<string, string> { { "OneDrive", ex.Message } };
                events.Add("Setup routine failure", "Exception thrown getting file list from OneDrive");
                TelemetryHelper.TrackEvent("FailedToDeletePicture", events);
                TelemetryHelper.TrackException(ex);
            }
        }

        public async Task Authorize(string accessCode)
        {
            await oneDriveConnector.LoginAsync(App.Controller.XmlSettings.OneDriveClientId, App.Controller.XmlSettings.OneDriveClientSecret, AppSettings.OneDriveRedirectUrl, accessCode);
        }

        public async Task AuthorizeWithRefreshToken(string refreshToken)
        {
            await oneDriveConnector.Reauthorize(App.Controller.XmlSettings.OneDriveClientId, App.Controller.XmlSettings.OneDriveClientSecret, AppSettings.OneDriveRedirectUrl, refreshToken);
        }

        public bool IsLoggedIn()
        {
            return oneDriveConnector.isLoggedIn;
        }

        public async Task Logout()
        {
            await oneDriveConnector.LogoutAsync();

            //Clear tokens
            App.Controller.XmlSettings.OneDriveAccessToken = string.Empty;
            App.Controller.XmlSettings.OneDriveRefreshToken = string.Empty;

        }

        public int GetNumberOfUploadedPictures()
        {
            return numberUploaded;
        }

        private void SaveTokens(object sender, string arg)
        {
            App.Controller.XmlSettings.OneDriveAccessToken = oneDriveConnector.accessToken;
            App.Controller.XmlSettings.OneDriveRefreshToken = oneDriveConnector.refreshToken;
        }


        /// <summary>
        /// Attempts to upload the given file, and will recursively retry until the MaxRetries limit is reached. 
        /// </summary>
        /// <param name="file">The file to upload. Assumes calling method has sole access to the file. Will delete the file after uploading</param>
        /// <param name="tryNumber">The number of the attempt being made. Should always initially be called with a value of 1</param>
        /// <returns></returns>
        private async Task uploadWithRetry(StorageFile file, int tryNumber = 1)
        {
            HttpResponseMessage response = await oneDriveConnector.UploadFileAsync(file, String.Format("{0}/{1}", App.Controller.XmlSettings.OneDriveFolderPath, DateTime.Now.ToString("yyyy_MM_dd")));
            bool success = await parseResponse(response, tryNumber);
            var events = new Dictionary<string, string>();

            if (success)
            {
                numberUploaded++;
                await file.DeleteAsync();
                this.lastUploadTime = DateTime.Now;
            }
            else if (tryNumber <= MaxTries)
            {
                events.Add("Retrying upload", tryNumber.ToString());
                TelemetryHelper.TrackEvent("FailedToUploadPicture - Next Retry Beginning", events);
                await uploadWithRetry(file, ++tryNumber);
            }
            else
            {
                events.Add("Max upload attempts reached", tryNumber.ToString());
                TelemetryHelper.TrackEvent("FailedToUploadPicture - All Retries failed", events);
            }
        }

        /// <summary>
        /// Attempts to delete the given picture from the given folder, and will recursively retry until the MaxRetries limit is reached. 
        /// </summary>
        /// <param name="picture">The name of the picture to delete</param>
        /// <param name="folder">The name and path of the folder from which to delete the picture</param>
        /// <param name="tryNumber">The number of the attempt being made. Should always initially be called with a value of 1</param>
        /// <returns></returns>
        private async Task deleteWithRetry(string picture, string folder, int tryNumber = 1)
        {
            HttpResponseMessage response = await oneDriveConnector.DeleteFileAsync(picture, folder);
            bool success = await parseResponse(response, tryNumber);
            var events = new Dictionary<string, string>();

            if (success)
            {
                //no additional action needed
            }
            else if (tryNumber <= MaxTries)
            {
                events.Add("Retrying delete", tryNumber.ToString());
                TelemetryHelper.TrackEvent("FailedToDeletePicture - Next Retry Beginning", events);
                await deleteWithRetry(picture, folder, ++tryNumber);
            }
            else
            {
                events.Add("Max delete attempts reached", tryNumber.ToString());
                TelemetryHelper.TrackEvent("FailedToDeletePicture - All Retries failed", events);
            }
        }

        /// <summary>
        /// Parses the HTTP response from a call one OneDrive, and sends telemetry about the response
        /// </summary>
        /// <param name="response">The HTTP response message from the OneDrive call</param>
        /// <param name="tryNumber">The attempt number of the call (first call is 1, etc)</param>
        /// <returns>Whether or not the reponse status from the server indicated success</returns>
        private async Task<bool> parseResponse(HttpResponseMessage response, int tryNumber)
        {
            bool successStatus;
            var events = new Dictionary<string, string>();
            events.Add("Attempt number", tryNumber.ToString());

            if (response.IsSuccessStatusCode)
            {
                successStatus = true;
                TelemetryHelper.TrackEvent("HttpSuccess", events);
            }
            else
            {
                successStatus = false;
                events.Add(response.StatusCode.ToString(), response.ReasonPhrase);
                events.Add("Original Request", response.RequestMessage.ToString());
                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    events.Add("Recovery Attempt", "AuthorizeWithRefreshToken()");
                    if (App.Controller.XmlSettings.OneDriveRefreshToken == "")
                    {
                        events.Add("App.Controller.XmlSettings.OneDriveRefreshToken", "Invalid Empty String");
                    }
                    else
                    {
                        events.Add("App.Controller.XmlSettings.OneDriveRefreshToken", "Valid String, attempting reauth");
                        await oneDriveConnector.Reauthorize();
                    }
                    TelemetryHelper.TrackEvent("HttpErrorOnRequest", events);
                }
                else if ((500 <= (int)response.StatusCode) && ((int)response.StatusCode < 600))
                {
                    events.Add("Recovery attempt", "Server side error, automatically attempting again");
                    TelemetryHelper.TrackEvent("HttpErrorOnRequest", events);
                }
                else
                {
                    events.Add("Recovery Attempt", "Unexpected HTTP response from server, error thrown");
                    TelemetryHelper.TrackEvent("HttpErrorOnRequest", events);
                    throw new System.Net.Http.HttpRequestException("UnexpectedHttpRequestError");
                }
            }
            return successStatus;
        }
    }
}