using System;
using System.Threading;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;


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

        private static bool started = false;

        private string[] cameras = new string[App.XmlSettings.NumberOfCameras];

        public MainPage()
        {
            this.InitializeComponent();
            Initialize();
             
        }

        private void Initialize()
        {
            storage = StorageFactory.Get(App.XmlSettings.StorageProvider);

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

            for (int i = 0; i < App.XmlSettings.NumberOfCameras; i++)
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

        private async void Start_Click(object sender, RoutedEventArgs e)
        {
            RunningToggle.Content = started ? "Stop" : "Start";  
            App.XmlSettings = await AppSettings.RestoreAsync("Settings.xml");
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
