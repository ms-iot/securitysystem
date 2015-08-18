using System;
using System.Diagnostics;
using Windows.Storage;
using Windows.Storage.Search;

namespace SecuritySystemUWP
{
    public class LocalStorage : IStorage
    {
        /*******************************************************************************************
        * PUBLIC METHODS
        *******************************************************************************************/
        public void UploadPictures(string camera)
        {
            return;
        }
        public async void DeleteExpiredPictures(string camera)
        {
            try
            {
                var querySubfolders = new QueryOptions();
                querySubfolders.FolderDepth = FolderDepth.Deep;

                var cacheFolder = KnownFolders.PicturesLibrary;
                cacheFolder = await cacheFolder.GetFolderAsync(AppSettings.FolderName);
                var result = cacheFolder.CreateFileQueryWithOptions(querySubfolders);
                var count = await result.GetItemCountAsync();
                var files = await result.GetFilesAsync();

                foreach (StorageFile file in files)
                {
                    long oldestTime = DateTime.UtcNow.Ticks - TimeSpan.FromDays(App.Controller.XmlSettings.StorageDuration).Ticks;
                    string picName = file.DisplayName.Split('_')[5];
                    if (picName.CompareTo(oldestTime.ToString()) < 0)
                    {
                        await file.DeleteAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Exception in deleteExpiredPictures() " + ex.Message);
            }
        }
    }
}
