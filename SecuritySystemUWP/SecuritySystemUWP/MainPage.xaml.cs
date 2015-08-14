using System;
using System.Collections.Generic;
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

        private static bool started = false;

        private string[] cameras;

        public MainPage()
        {
            this.InitializeComponent();             
        }

        protected async override void OnNavigatedTo(NavigationEventArgs e)
        {
            App.XmlSettings = await AppSettings.RestoreAsync("Settings.xml");
            WebServer.Start(8000);

            Dictionary<string, string> properties = new Dictionary<string, string> { { "Alias", App.XmlSettings.MicrosoftAlias } };
            App.TelemetryClient.TrackTrace("Start Info", properties);
            cameras = new string[App.XmlSettings.NumberOfCameras]; 

            Initialize();
        }

        private async void Initialize()
        {
            App.Storage = StorageFactory.Get(App.XmlSettings.StorageProvider);

            // Try to login using existing Access Token in settings file
            if(App.Storage.GetType() == typeof(OneDrive))
            {
                var oneDriveStorage = ((OneDrive)App.Storage);
                if(!OneDrive.IsLoggedIn())
                {
                    await OneDrive.AuthorizeWithRefreshToken(App.XmlSettings.OneDriveRefreshToken);
                }
            }

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
                this.Frame.Navigate(App.Storage.LoginType());
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
            this.Frame.Navigate(App.Storage.LoginType());
        }

        private void uploadPicturesTimer_Tick(object sender, object e)
        {
            App.Storage.UploadPictures(cameras[0]);
        }

        private void deletePicturesTimer_Tick(object sender, object e)
        {
            App.Storage.DeleteExpiredPictures(cameras[0]);
        }
    }
}
