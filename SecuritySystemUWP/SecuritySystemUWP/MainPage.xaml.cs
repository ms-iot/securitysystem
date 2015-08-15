using System;
using System.Collections.Generic;
using System.Threading;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
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
        private static IStorage storage;
        private static ICamera camera;
        private string[] cameras = { "Cam1" };
        private static DispatcherTimer uploadPicturesTimer;
        private static DispatcherTimer deletePicturesTimer;
        private const int uploadInterval = 10; //Value in seconds
        private const int deleteInterval = 1; //Value in hours

        private static bool started = false;

        public MainPage()
        {
            this.InitializeComponent();
        }

        private async Task Initialize()
        {

            camera = CameraFactory.Get(App.XmlSettings.CameraType);
            storage = StorageFactory.Get(App.XmlSettings.StorageProvider);

            await camera.Initialize();

            // Try to login using existing Access Token in settings file
            if (App.Storage.GetType() == typeof(OneDrive))
            {
                var oneDriveStorage = ((OneDrive)App.Storage);
                if (!OneDrive.IsLoggedIn())
                {
                    await OneDrive.AuthorizeWithRefreshToken(App.XmlSettings.OneDriveRefreshToken);
                }
            }

            //Timer controlling camera pictures with motion
            uploadPicturesTimer = new DispatcherTimer();
            uploadPicturesTimer.Interval = TimeSpan.FromSeconds(uploadInterval);
            uploadPicturesTimer.Tick += uploadPicturesTimer_Tick;
            uploadPicturesTimer.Start();

            //Timer controlling deletion of old pictures
            deletePicturesTimer = new DispatcherTimer();
            deletePicturesTimer.Interval = TimeSpan.FromHours(deleteInterval);
            deletePicturesTimer.Tick += deletePicturesTimer_Tick;
            deletePicturesTimer.Start();
        }

        private void Dispose()
        {
            uploadPicturesTimer.Stop();
            deletePicturesTimer.Stop();
            camera.Dispose();
        }

        private async void RunningToggle_Click(object sender, RoutedEventArgs e)
        {
            if (!started)
            {
                await Initialize();
                started = true;
                App.XmlSettings = await AppSettings.RestoreAsync("Settings.xml");
                this.Frame.Navigate(storage.StorageStartPage());
            }
            else
            {
                Dispose();
                started = false;
                this.Frame.Navigate(typeof(MainPage));
            }
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            RunningToggle.Content = started ? "Stop" : "Start";
            App.XmlSettings = await AppSettings.RestoreAsync("Settings.xml");
            this.Frame.Navigate(App.Storage.StorageStartPage());
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
