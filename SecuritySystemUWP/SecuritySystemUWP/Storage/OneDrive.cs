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

        public async void UploadPictures(string cameraName)
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
                            await oneDriveConnector.UploadFileAsync(file, String.Format("{0}/{1}", App.Controller.XmlSettings.OneDriveFolderPath, DateTime.Now.ToString("MM_dd_yyyy")));
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
                    AppController.uploadPicturesTimer.Start();
                }
            }
        }

        public async void DeleteExpiredPictures(string camera)
        {
            try
            {
                string folder = string.Format("{0}/{1}", App.Controller.XmlSettings.OneDriveFolderPath,  DateTime.Now.Subtract(TimeSpan.FromDays(App.Controller.XmlSettings.StorageDuration)).ToString("MM_dd_yyyy"));
                //List pictures in old day folder
                List<string> pictures = new List<string>(await oneDriveConnector.ListFilesAsync(folder));
                if (pictures != null)
                {
                    //Delete all pictures from the day
                    foreach (string picture in pictures)
                    {
                        await oneDriveConnector.DeleteFileAsync(picture, folder);
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
            App.Controller.XmlSettings.OneDriveAccessToken = "";
            App.Controller.XmlSettings.OneDriveRefreshToken = "";

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
    }
}