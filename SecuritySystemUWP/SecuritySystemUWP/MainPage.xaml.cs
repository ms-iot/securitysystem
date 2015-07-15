using Windows.UI.Xaml.Controls;
using System.Threading.Tasks;
using System.Diagnostics;
using System;
using Windows.Web.Http;
using Windows.Devices.Gpio;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Navigation;
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
        //TODO: Select storage type: ONEDRIVE for OneDrive, AZURE for Azure
        private byte storageType = StorageFactory.ONEDRIVE;

        //TODO: If Azure, input account name and account key in variables below


        private DispatcherTimer uploadPicturesTimer;
        private DispatcherTimer deletePicturesTimer;
        private static Mutex uploadPicturesMutexLock = new Mutex();
        private StorageFactory storageFactory;

        public MainPage()
        {
            this.InitializeComponent();
            Initialize();
        }

        private void Initialize()
        {
            storageFactory = new StorageFactory(storageType);

            //Timer controlling camera pictures with motion
            uploadPicturesTimer = new DispatcherTimer();
            uploadPicturesTimer.Interval = TimeSpan.FromSeconds(10);
            uploadPicturesTimer.Tick += uploadPicturesTimer_Tick;
            uploadPicturesTimer.Start();

            //Timer controlling deletion of old pictures
            deletePicturesTimer = new DispatcherTimer();
            deletePicturesTimer.Interval = TimeSpan.FromHours(1);
            deletePicturesTimer.Tick += deletePicturesTimer_Tick;
            deletePicturesTimer.Start();

        }

        private void Start_Click(object sender, RoutedEventArgs e)
        {
            this.Frame.Navigate(storageFactory.getNavigationType());
        }

        private async void uploadPicturesTimer_Tick(object sender, object e)
        {
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
                    if(await storageFactory.uploadPicture(file))
                    {
                        Debug.WriteLine("Image uploaded");
                        await file.DeleteAsync();
                    }
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
            await storageFactory.deleteExpiredPictures();
        }
    }
}
