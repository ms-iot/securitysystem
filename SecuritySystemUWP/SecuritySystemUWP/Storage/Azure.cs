using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Search;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;

namespace SecuritySystemUWP
{
    public class Azure : IStorage
    {
        private CloudStorageAccount storageAccount;
        private CloudBlobClient blobClient;
        private CloudBlobContainer blobContainer;
        private DateTime lastUploadTime = DateTime.MinValue;

        public Azure()
        {
            //Get the connection settings information using account name and key
            string connectionSettings = string.Format(AppSettings.AzureConnectionSettings, App.Controller.XmlSettings.AzureAccountName, App.Controller.XmlSettings.AzureAccessKey);
            storageAccount = CloudStorageAccount.Parse(connectionSettings);

            //Create client to access blob storage
            blobClient = storageAccount.CreateCloudBlobClient();

            //Get the container from Azure. Container name must match the folder name.
            blobContainer = blobClient.GetContainerReference(AppSettings.FolderName);
        }
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

        public async void UploadPictures(string camera)
        {
            // Stop timer to allow time for uploading pictures in case next timer tick overlaps with this ongoing one
            AppController.uploadPicturesTimer.Stop();

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
                    //Image name contains creation time
                    string imageName = string.Format(AppSettings.ImageNameFormat, camera, DateTime.Now.ToString("MM_dd_yyyy/HH"), DateTime.UtcNow.Ticks.ToString());
                    if (file.IsAvailable)
                    {
                        //Upload image to blob storage
                        await uploadPictureToAzure(imageName, file);   
                        //Delete image from local storage after a successful upload                     
                        await file.DeleteAsync();
                    }
                }

                this.lastUploadTime = DateTime.Now;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Exception in uploadPictures() " + ex.Message);

                // Log telemetry event about this exception
                var events = new Dictionary<string, string> { { "Azure", ex.Message } };
                App.Controller.TelemetryClient.TrackEvent("FailedToUploadPicture", events);
            }
            finally
            {
                AppController.uploadPicturesTimer.Start();
            }
        }

        public async void DeleteExpiredPictures(string camera)
        {
            try
            {
                List<string> pictures = await listPictures(AppSettings.FolderName);
                foreach (string picture in pictures)
                {
                    //Calculate oldest time in ticks using the user selected storage duration 
                    long oldestTime = DateTime.UtcNow.Ticks - TimeSpan.FromDays(App.Controller.XmlSettings.StorageDuration).Ticks;
                    //Get the time of image creation in ticks
                    string picName = picture.Split('_')[3];
                    if (picName.CompareTo(oldestTime.ToString()) < 0)
                    {
                        int index = picture.LastIndexOf(AppSettings.FolderName + "/") + AppSettings.FolderName.Length + 1;
                        await deletePicture(picture.Substring(index));
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Exception in deleteExpiredPictures() " + ex.Message);

                // Log telemetry event about this exception
                var events = new Dictionary<string, string> { { "Azure", ex.Message } };
                App.Controller.TelemetryClient.TrackEvent("FailedToDeletePicture", events);
            }
        }

        public void Dispose()
        {
            //Clear connection settings
            string connectionSettings = "";
        }
        
        /*******************************************************************************************
        * PRIVATE METHODS
        ********************************************************************************************/
        private async Task uploadPictureToAzure(string imageName, StorageFile imageFile)
        {
            //Log data for upload attempt
            Windows.Storage.FileProperties.BasicProperties fileProperties = await imageFile.GetBasicPropertiesAsync();
            Dictionary<string, string> properties = new Dictionary<string, string> { { "File Size", fileProperties.Size.ToString() } };
            App.Controller.TelemetryClient.TrackEvent("Azure picture upload attempt", properties);
            try
            {
                //Create a blank blob
                CloudBlockBlob newBlob = blobContainer.GetBlockBlobReference(imageName);

                //Add image data to blob
                await newBlob.UploadFromFileAsync(imageFile);
            }
            catch(Exception ex)
            {
                //This failure will be logged in telemetry in the enclosing UploadPictures function. We don't want this to be recorded twice.
                throw new Exception("Exception in uploading pictures to Azure: " + ex.Message);
            }
            //Log successful upload event
            App.Controller.TelemetryClient.TrackEvent("Azure picture upload success", properties);
        }

        private async Task<List<string>> listPictures(string folderPath)
        {
            List<string> blobList = new List<string>();
            BlobContinuationToken continuationToken = null;
            BlobResultSegment resultSegment = null;
            do
            {
                resultSegment = await blobContainer.ListBlobsSegmentedAsync(
                    "", //Prefix for listed images
                    true, //Flat listing of blobs, not hierarchical
                    BlobListingDetails.All, //List all items in folder
                    10, //List 10 items at a time
                    continuationToken, //Continue till all items listed
                    null, //Object for additional options
                    null //Object for current operation context
                    );
                foreach (var item in resultSegment.Results)
                {
                    string blobUri = item.StorageUri.PrimaryUri.ToString();
                    blobList.Add(blobUri);
                }
                //Update continuation token
                continuationToken = resultSegment.ContinuationToken;
            }
            while (continuationToken != null);
            return blobList;
        }

        private async Task deletePicture(string imageName)
        {
            //Get blob to delete
            CloudBlockBlob oldBlob = blobContainer.GetBlockBlobReference(imageName);
            //Delete blob
            await oldBlob.DeleteAsync();
        }

    }
}

