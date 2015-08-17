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
    /// Main page of the app
    /// </summary>
    public sealed partial class MainPage : Page
    {
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
            App.Camera = CameraFactory.Get(App.XmlSettings.CameraType);
            App.Storage = StorageFactory.Get(App.XmlSettings.StorageProvider);

            await App.Camera.Initialize();

            // Try to login using existing Access Token in settings file
            if (App.Storage.GetType() == typeof(OneDrive))
            {
                var oneDriveStorage = ((OneDrive)App.Storage);
                if (!OneDrive.IsLoggedIn())
                {
                    try
                    {
                        await OneDrive.AuthorizeWithRefreshToken(App.XmlSettings.OneDriveRefreshToken);
                    }
                    catch (Exception e)
                    {
                        Debug.WriteLine(e.Message);
                    }
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
            App.Camera.Dispose();
        }

        private async void RunningToggle_Click(object sender, RoutedEventArgs e)
        {
            if (!started)
            {
                await Initialize();
                started = true;
                App.XmlSettings = await AppSettings.RestoreAsync("Settings.xml");
                this.Frame.Navigate(App.Storage.StorageStartPage());
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
            await Initialize();
            //RunningToggle.Content = started ? "Stop" : "Start";
            //this.Frame.Navigate(App.Storage.StorageStartPage());
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
