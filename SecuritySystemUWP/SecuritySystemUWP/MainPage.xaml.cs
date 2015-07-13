using Windows.UI.Xaml.Controls;
using DeviceProviders;
using System.Threading.Tasks;
using System.Diagnostics;
using System;
using Windows.Web.Http;
using Windows.Devices.Gpio;
using System.IO;
using Windows.UI.Xaml.Media.Imaging;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System.Collections.Generic;
using System.Linq;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Controls;
using Windows.Storage.Search;
using Windows.Storage;
using System.Threading;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace SecuritySystemUWP
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        //TODO: Select storage type: 0 for OneDrive, 1 for Azure
        public byte storageType = 0;

        //TODO: If Azure, input account name and account key in variables below
        private string accountName = "";
        private string accountKey = "";

        private string blobType = "BlockBlob";
        private string sharedKeyAuthorizationScheme = "SharedKey";
        static readonly UInt32 reloadContentFileCount = 10;
        private DispatcherTimer uploadPicturesTimer;
        private DispatcherTimer deletePicturesTimer;
        private static Mutex uploadPicturesMutexLock = new Mutex();

        public MainPage()
        {
            this.InitializeComponent();
            Initialize();
        }

        private void Initialize()
        {
            if (storageType == 1)
            {
                //Timer controlling camera pictures with motion
                uploadPicturesTimer = new DispatcherTimer();
                uploadPicturesTimer.Interval = TimeSpan.FromSeconds(10);
                uploadPicturesTimer.Tick += Azure_uploadPicturesTimer_Tick;
                uploadPicturesTimer.Start();

                //Timer controlling deletion of old pictures
                deletePicturesTimer = new DispatcherTimer();
                deletePicturesTimer.Interval = TimeSpan.FromHours(1);
                deletePicturesTimer.Tick += Azure_deletePicturesTimer_Tick;
                deletePicturesTimer.Start();
            }
            else
            {
                //Timer controlling camera pictures with motion
                uploadPicturesTimer = new DispatcherTimer();
                uploadPicturesTimer.Interval = TimeSpan.FromSeconds(10);
                uploadPicturesTimer.Tick += OneDrive_uploadPicturesTimer_Tick;
                uploadPicturesTimer.Start();
            }
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            if (storageType == 0)
            {
                if (OneDriveHelper.isLoggedin)
                {
                    OnedriveLogin.Content = "Logout from OneDrive";
                }
                else
                {
                    OnedriveLogin.Content = "Login to OneDrive";
                }
            }
        }

        private async void OnedriveLogin_Click(object sender, RoutedEventArgs e)
        {
            if (OneDriveHelper.isLoggedin)
            {
                await OneDriveHelper.logout();
                OnedriveLogin.Content = "Login to OneDrive";
            }
            else
            {
                this.Frame.Navigate(typeof(OnedriveLoginPage));
            }

        }
        private async void OneDrive_uploadPicturesTimer_Tick(object sender, object e)
        {
            // enter mutex critical section to make this thread-safe
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
                    var imageName = DateTime.UtcNow.Ticks.ToString() + ".jpg";
                    if (OneDriveHelper.isLoggedin)
                    {
                        await OneDriveHelper.UploadFile(file, imageName);
                        await file.DeleteAsync();
                    }                  
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Exception in processPictures(): " + ex.Message);
            }
            finally
            {
                uploadPicturesMutexLock.ReleaseMutex();
            }
        }


        private async void Azure_uploadPicturesTimer_Tick(object sender, object e)
        {
            uploadPicturesMutexLock.WaitOne();
            BlobHelper BlobHelper = new BlobHelper(accountName, accountKey);
            try
            {
                QueryOptions querySubfolders = new QueryOptions();
                querySubfolders.FolderDepth = FolderDepth.Deep;
               
                StorageFolder cacheFolder = KnownFolders.PicturesLibrary;
                var result = cacheFolder.CreateFileQueryWithOptions(querySubfolders);
                var files = await result.GetFilesAsync();

                foreach (StorageFile file in files)
                {
                    var memStream = new MemoryStream();
                    Stream testStream = await file.OpenStreamForReadAsync();
                    await testStream.CopyToAsync(memStream);
                    memStream.Position = 0;

                    string imageName = DateTime.UtcNow.Ticks.ToString() + ".jpg";
                    Debug.WriteLine(imageName);
                    if (await BlobHelper.PutBlob("imagecontainer", imageName, memStream))
                    {
                        Debug.WriteLine("true");
                    }
                    else
                    {
                        Debug.WriteLine("false");

                    }
                    await file.DeleteAsync();
                }

            }
            catch (Exception ex)
            {
                Debug.WriteLine("Exception in uploadPicturesTimer_Tick() " + ex.Message);
            }
            finally
            {
                uploadPicturesMutexLock.ReleaseMutex();
            }
        }

        private async void Azure_deletePicturesTimer_Tick(object sender, object e)
        {
            BlobHelper BlobHelper = new BlobHelper(accountName, accountKey);

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
    }
}
