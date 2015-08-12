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
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Blob;


namespace SecuritySystemUWP
{
    public class Azure : IStorage
    {
        private static Mutex uploadPicturesMutexLock = new Mutex();
        private CloudStorageAccount storageAccount;
        private CloudBlobClient blobClient;
        private CloudBlobContainer blobContainer;
        
        public Azure()
        {
            string connectionSettings = string.Format(Config.AzureConnectionSettings, App.Settings.AzureAccountName, App.Settings.AzureAccessKey);
            storageAccount = CloudStorageAccount.Parse(connectionSettings);
            blobClient = storageAccount.CreateCloudBlobClient();
            blobContainer = blobClient.GetContainerReference(App.Settings.FolderName);
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
                var querySubfolders = new QueryOptions();
                querySubfolders.FolderDepth = FolderDepth.Deep;

                var cacheFolder = KnownFolders.PicturesLibrary;
                cacheFolder = await cacheFolder.GetFolderAsync("securitysystem-cameradrop");
                var result = cacheFolder.CreateFileQueryWithOptions(querySubfolders);
                var count = await result.GetItemCountAsync();
                var files = await result.GetFilesAsync();

                foreach (StorageFile file in files)
                {
                    string imageName = string.Format(Config.ImageNameFormat, camera, DateTime.Now.ToString("MM_dd_yyyy/HH"), DateTime.UtcNow.Ticks.ToString());
                    if (file.IsAvailable)
                    {
                        await uploadPictureToAzure(imageName, file);                        
                        await file.DeleteAsync();
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

        public async void DeleteExpiredPictures(string camera)
        {
            try
            {
                List<string> pictures = await listPictures(App.Settings.FolderName);
                foreach (string picture in pictures)
                {
                    long oldestTime = DateTime.UtcNow.Ticks - TimeSpan.FromDays(App.Settings.StorageDuration).Ticks;
                    string picName = picture.Split('_')[3];
                    if (picName.CompareTo(oldestTime.ToString()) < 0)
                    {
                        int index = picture.LastIndexOf(App.Settings.FolderName + "/") + App.Settings.FolderName.Length + 1;
                        await deletePicture(picture.Substring(index));
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
        private async Task uploadPictureToAzure(string imageName, StorageFile imageFile)
        {
            Windows.Storage.FileProperties.BasicProperties fileProperties = await imageFile.GetBasicPropertiesAsync();
            Dictionary<string, string> properties = new Dictionary<string, string> { { "File Size", fileProperties.Size.ToString() } };
            App.TelemetryClient.TrackEvent("Azure picture upload attempt", properties);
            try
            {
                CloudBlockBlob newBlob = blobContainer.GetBlockBlobReference(imageName);
                await newBlob.UploadFromFileAsync(imageFile);
            }
            catch(Exception ex)
            {
                Debug.WriteLine("Exception in uploading pictures to Azure: " + ex.Message);
                throw;
            }
            App.TelemetryClient.TrackEvent("Azure picture upload success", properties);
        }


        private async Task<List<string>> listPictures(string folderPath)
        {
            List<string> blobList = new List<string>();
            BlobContinuationToken continuationToken = null;
            BlobResultSegment resultSegment = null;

            do
            {
                resultSegment = await blobContainer.ListBlobsSegmentedAsync("", true, BlobListingDetails.All, 10, continuationToken, null, null);
                foreach (var item in resultSegment.Results)
                {
                    string blobUri = item.StorageUri.PrimaryUri.ToString();
                    blobList.Add(blobUri);
                }
                continuationToken = resultSegment.ContinuationToken;
            }
            while (continuationToken != null);
            return blobList;
        }

        private async Task deletePicture(string imageName)
        {
            CloudBlockBlob oldBlob = blobContainer.GetBlockBlobReference(imageName);
            await oldBlob.DeleteAsync();
        }

    }
}

