using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.Media.Capture;
using Windows.Devices.Enumeration;


// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace SecuritySystemUWP
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private IStorage storage;
        private ICamera camera;
        private DispatcherTimer uploadPicturesTimer;
        private DispatcherTimer deletePicturesTimer;
        private static bool isInitialized = false;
        private string[] cameras = new string[Config.NumberOfCameras];
        public MainPage()
        {
            this.InitializeComponent();
            if (!isInitialized)
            {
                Initialize();
                isInitialized = true;
            }
        }

        private void Initialize()
        {
            camera = CameraFactory.Get(Config.CameraType);
            storage = StorageFactory.Get(Config.StorageProvider);

            camera.Initialize();

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

            for (int i = 0; i < Config.NumberOfCameras; i++)
            {
                cameras[i] = "Cam" + (i + 1);
            }
        }



        private void Start_Click(object sender, RoutedEventArgs e)
        {
            this.Frame.Navigate(storage.LoginType());
        }

        private void uploadPicturesTimer_Tick(object sender, object e)
        {
            storage.UploadPictures(cameras[0]);
        }

        private void deletePicturesTimer_Tick(object sender, object e)
        {
            storage.DeleteExpiredPictures(cameras[0]);
        }
    }
}
