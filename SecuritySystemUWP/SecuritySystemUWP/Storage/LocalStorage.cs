using System;
using System.Collections.Generic;
using System.Diagnostics;
using Windows.Storage;
using Windows.Storage.Search;

namespace SecuritySystemUWP
{
    public class LocalStorage : IStorage
    {
        // Local storage never uploads
        private DateTime lastUploadTime = DateTime.MinValue;

        /*******************************************************************************************
* PUBLIC METHODS
*******************************************************************************************/
        public DateTime LastUploadTime
        {
            get
            {
                return this.lastUploadTime;
            }
        }

        public void UploadPictures(string camera)
        {
            //The pictures are automatically stored in the local storage during image capture.
            return;
        }

        public async void DeleteExpiredPictures(string camera)
        {
            //Delete older images
            try
            {
                var querySubfolders = new QueryOptions();
                querySubfolders.FolderDepth = FolderDepth.Deep;

                var cacheFolder = KnownFolders.PicturesLibrary;
                cacheFolder = await cacheFolder.GetFolderAsync(AppSettings.FolderName);
                var result = cacheFolder.CreateFileQueryWithOptions(querySubfolders);
                var files = await result.GetFilesAsync();

                foreach (StorageFile file in files)
                {
                    //Caluclate oldest time in ticks using the user selected storage duration 
                    long oldestTime = DateTime.UtcNow.Ticks - TimeSpan.FromDays(App.Controller.XmlSettings.StorageDuration).Ticks;
                    long picCreated = file.DateCreated.Ticks;
                    if (picCreated < oldestTime)
                    {
                        await file.DeleteAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Exception in deleteExpiredPictures() " + ex.Message);

                // Log telemetry event about this exception
                var events = new Dictionary<string, string> { { "LocalStorage", ex.Message } };
                App.Controller.TelemetryClient.TrackEvent("FailedToDeletePicture", events);
            }
        }

        public void Dispose()
        {
            return;
        }
    }
}
