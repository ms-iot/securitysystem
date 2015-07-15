using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Xaml.Controls;

namespace SecuritySystemUWP
{
    class StorageFactory
    {
        //TODO: ONEDRIVE
        private string clientId = "";
        private string clientSecret = "";

        //TODO: AZURE
        private string accountName = "";
        private string accountKey = "";

        private int storageType;
        public const int ONEDRIVE = 0;
        public const int AZURE = 1;
        public OneDriveHelper OneDriveHelper;
        public BlobHelper BlobHelper;

        public StorageFactory(int type)
        {
            storageType = type;
            OneDriveHelper = new OneDriveHelper(clientId, clientSecret);
            BlobHelper = new BlobHelper(accountName, accountKey);
        }

        public Type getNavigationType()
        {
            Type navigationType = typeof(MainPage);
            switch (storageType)
            {
                case ONEDRIVE:
                    navigationType = typeof(OnedriveLoginPage);
                    break;

                case AZURE:
                    navigationType = typeof(MainPage);
                    break;
            }
            return navigationType;
        }

        public async Task<bool> uploadPicture(StorageFile imageFile)
        {
            bool result = false;
            string imageName = DateTime.UtcNow.Ticks.ToString() + ".jpg";
            switch (storageType)
            {
                case ONEDRIVE:
                    try {
                        if (OneDriveHelper.isLoggedin)
                        {
                            await OneDriveHelper.UploadFile("imagecontainer", imageName, imageFile);
                            result = true;
                        }
                        else
                        {
                            result = false;
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("Exception in uploading pictures to OneDrive: " + ex.Message);
                        result = false;
                    }
                    break;

                case AZURE:
                    try
                    {
                        BlobHelper BlobHelper = new BlobHelper(accountName, accountKey);                  
                        var memStream = new MemoryStream();
                        Stream testStream = await imageFile.OpenStreamForReadAsync();
                        await testStream.CopyToAsync(memStream);
                        memStream.Position = 0;

                        if (await BlobHelper.PutBlob("imagecontainer", imageName, memStream))
                        {
                            result = true;
                        }
                        else
                        {
                            result = false;
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("Exception in uploading pictures to Azure: " + ex.Message);
                        result = false;
                    }
                    break;
            }
            return result;
        }
        public async Task<bool> deleteExpiredPictures()
        {
            bool result = false;

            switch (storageType)
            {
                case ONEDRIVE:
                    try
                    {
                        if (OneDriveHelper.isLoggedin)
                        {
                            List<string> fileList = await OneDriveHelper.ListImages("imagecontainer");
                            foreach (string file in fileList)
                            {
                                long oldestTime = DateTime.UtcNow.Ticks - TimeSpan.FromDays(7).Ticks;
                                if (file.CompareTo(oldestTime.ToString()) < 0)
                                {
                                    if (await OneDriveHelper.DeleteFile("imagecontainer", file))
                                    {
                                        Debug.WriteLine("Delete successful");
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("Exception in deleting pictures from Azure: " + ex.Message);
                        result = false;
                    }
                    break;

                case AZURE:
                    try
                    {
                        List<string> blobList = await BlobHelper.ListBlobs("imagecontainer");
                        foreach (string blob in blobList)
                        {
                            long oldestTime = DateTime.UtcNow.Ticks - TimeSpan.FromDays(7).Ticks;
                            if (blob.CompareTo(oldestTime.ToString()) < 0)
                            {
                                Debug.WriteLine("Delete blob ");
                                if (await BlobHelper.DeleteBlob("imagecontainer", blob))
                                {
                                    Debug.WriteLine("Delete successful");
                                }
                                else
                                {
                                    Debug.WriteLine("Delete failed");
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("Exception in deleting pictures from Azure: " + ex.Message);
                        result = false;
                    }
                    break;
            }
            return result;
        }
    }
}
