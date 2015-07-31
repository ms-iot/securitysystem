using System;
using System.Threading;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;


// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace SecuritySystemUWP
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private DispatcherTimer uploadPicturesTimer;
        private DispatcherTimer deletePicturesTimer;
        private IStorage storage;

        private bool started = false;

        private string[] cameras = new string[Config.NumberOfCameras];
        public MainPage()
        {
            this.InitializeComponent();
            Initialize();
        }

        private void Initialize()
        {
            storage = StorageFactory.Get(Config.StorageProvider);

            //Timer controlling camera pictures with motion
            uploadPicturesTimer = new DispatcherTimer();
            uploadPicturesTimer.Interval = TimeSpan.FromSeconds(10);
            uploadPicturesTimer.Tick += uploadPicturesTimer_Tick;

            //Timer controlling deletion of old pictures
            deletePicturesTimer = new DispatcherTimer();
            deletePicturesTimer.Interval = TimeSpan.FromHours(1);
            deletePicturesTimer.Tick += deletePicturesTimer_Tick;

            for (int i = 0; i < Config.NumberOfCameras; i++)
            {
                cameras[i] = "Cam" + (i + 1);
            }

        }

        private void RunningToggle_Click(object sender, RoutedEventArgs e)
        {
            if (!started)
            {
                uploadPicturesTimer.Start();
                deletePicturesTimer.Start();
                started = true;
                this.Frame.Navigate(storage.LoginType());
            }
            else
            {
                uploadPicturesTimer.Stop();
                deletePicturesTimer.Stop();
                started = false;
                this.Frame.Navigate(typeof(MainPage));
            }
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            if(!started)
            {
                RunningToggle.Content = "Start";
            }
            else
            {
                RunningToggle.Content = "Stop";
            }
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
