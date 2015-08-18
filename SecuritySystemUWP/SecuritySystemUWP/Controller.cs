using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.Foundation;
using Windows.UI.Xaml;

namespace SecuritySystemUWP
{
    public class AppController
    {
        /// <summary>
        /// Allows tracking page views, exceptions and other telemetry through the Microsoft Application Insights service.
        /// </summary>
        public Microsoft.ApplicationInsights.TelemetryClient TelemetryClient;

        /// <summary>
        /// Configuration settings for app
        /// </summary>
        public AppSettings XmlSettings;

        /// <summary>
        /// Storage type
        /// </summary>
        public IStorage Storage;

        /// <summary>
        /// Camera type
        /// </summary>
        public ICamera Camera;

        /// <summary>
        /// Web interface
        /// </summary>
        public WebServer Server;
                
        private string[] cameras = { "Cam1" };
        private static DispatcherTimer uploadPicturesTimer;
        private static DispatcherTimer deletePicturesTimer;
        private const int uploadInterval = 10; //Value in seconds
        private const int deleteInterval = 1; //Value in hours

        public AppController()
        {
            TelemetryClient = new Microsoft.ApplicationInsights.TelemetryClient();
            Server = new WebServer();
            XmlSettings = new AppSettings();
        }

        public IAsyncAction Initialize()
        {
            return CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () =>
            {
                try
                {
                    XmlSettings = await AppSettings.RestoreAsync("Settings.xml");

                    if (!Server.IsRunning)
                        Server.Start(8000);

                    Camera = CameraFactory.Get(XmlSettings.CameraType);
                    Storage = StorageFactory.Get(XmlSettings.StorageProvider);

                    await Camera.Initialize();

                    // Try to login using existing Access Token in settings file
                    if (Storage.GetType() == typeof(OneDrive))
                    {
                        var oneDriveStorage = ((OneDrive)Storage);
                        if (!OneDrive.IsLoggedIn())
                        {
                            try
                            {
                                await OneDrive.AuthorizeWithRefreshToken(XmlSettings.OneDriveRefreshToken);
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
                }catch(Exception e)
                {
                    Debug.WriteLine(e.Message);
                }
            });
        }

        public IAsyncAction Dispose()
        {
            return CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                try
                {
                    if (uploadPicturesTimer != null)
                    {
                        uploadPicturesTimer.Stop();
                    }

                    if (deletePicturesTimer != null)
                    {
                        deletePicturesTimer.Stop();
                    }

                    if (Camera != null)
                    {
                        Camera.Dispose();
                    }
                }catch(Exception e)
                {
                    Debug.WriteLine("Controller.Dispose(): " + e.Message);
                }
            });
        }

        private void uploadPicturesTimer_Tick(object sender, object e)
        {
            uploadPicturesTimer.Stop();

            Storage.UploadPictures(cameras[0]);

            uploadPicturesTimer.Start();
        }

        private void deletePicturesTimer_Tick(object sender, object e)
        {
            Storage.DeleteExpiredPictures(cameras[0]);
        }
    }
}
