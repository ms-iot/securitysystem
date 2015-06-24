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
        //TODO: Input account name and account key in variables below
        private string accountName = "";
        private string accountKey = "";
        private string blobType = "BlockBlob";
        private string sharedKeyAuthorizationScheme = "SharedKey";

        private DispatcherTimer deleteImageTimer;
        private DispatcherTimer uploadPicturesTimer;
        private readonly TimeSpan uploadPicturesIntervalDuration = new TimeSpan(0, 0, 10);
        private static Mutex uploadPicturesMutexLock = new Mutex();

        public MainPage()
        {
            this.InitializeComponent();
            Initialize();
        }

        private void Initialize()
        {
            //Timer controlling camera pictures with motion
            uploadPicturesTimer = new DispatcherTimer();
            uploadPicturesTimer.Interval = uploadPicturesIntervalDuration;
            uploadPicturesTimer.Tick += uploadPicturesTimer_Tick;
            uploadPicturesTimer.Start();

            //Timer controlling deletion of old pictures
            deleteImageTimer = new DispatcherTimer();
            deleteImageTimer.Interval = TimeSpan.FromMilliseconds(TimeSpan.FromHours(1).TotalMilliseconds);
            deleteImageTimer.Tick += deletePicturesTimer_Tick;
        }


        private async void uploadPicturesTimer_Tick(object sender, object e)
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

        private async void deletePicturesTimer_Tick(object sender, object e)
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
