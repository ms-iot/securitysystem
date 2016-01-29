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
                            await uploadWithRetry(file, 1);
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine("UploadPictures(): " + ex.Message);

                            // Log telemetry event about this exception
                            var events = new Dictionary<string, string> { { "OneDrive", ex.Message } };
                            TelemetryHelper.TrackEvent("FailedToUploadPicture", events);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Exception in UploadPictures() " + ex.Message);

                    // Log telemetry event about this exception
                    var events = new Dictionary<string, string> { { "OneDrive", ex.Message } };
                    TelemetryHelper.TrackEvent("FailedToUploadPicture", events);
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
                        await deleteWithRetry(picture, folder, 1);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Exception in deleteExpiredPictures() " + ex.Message);

                // Log telemetry event about this exception
                var events = new Dictionary<string, string> { { "OneDrive", ex.Message } };
                TelemetryHelper.TrackEvent("FailedToDeletePicture", events);
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

        private async Task uploadWithRetry(StorageFile file, int tryNumber)
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
                TelemetryHelper.TrackEvent("FailedToUploadPicture", events);
                await uploadWithRetry(file, ++tryNumber);
            }
            else
            {
                events.Add("Max upload attempts reached", tryNumber.ToString());
                TelemetryHelper.TrackEvent("FailedToUploadPicture - total failure", events);
            }
        }

        private async Task deleteWithRetry(string picture, string folder, int tryNumber)
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
                TelemetryHelper.TrackEvent("FailedToDeletePicture", events);
                await deleteWithRetry(picture, folder, ++tryNumber);
            }
            else
            {
                events.Add("Max delete attempts reached", tryNumber.ToString());
                TelemetryHelper.TrackEvent("FailedToDeletePicture - total failure", events);
            }
        }

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