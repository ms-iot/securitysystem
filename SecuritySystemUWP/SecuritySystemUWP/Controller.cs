using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.Foundation;
using Windows.Storage;
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
        /// Server that runs the web interface
        /// </summary>
        public WebServer Server;

        /// <summary>
        /// Provides status if the controller has been initialized or not
        /// </summary>
        public bool IsInitialized { get; private set; } = false;

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

        /// <summary>
        /// Initializes the controller:  Loads settings, starts web server, sets up the camera and storage providers,
        /// tries to log into OneDrive (if OneDrive is selected), and starts the file upload and deletion timers
        /// </summary>
        /// <returns></returns>
        public async Task Initialize()
        {
            try
            {
                // Load settings from file
                XmlSettings = await AppSettings.RestoreAsync("Settings.xml");

                // Start web server on port 8000
                if (!Server.IsRunning)
                    Server.Start(8000);

                // Create local storage folder if it doesn't exist
                StorageFolder folder = KnownFolders.PicturesLibrary;
                try
                {
                    await folder.GetFolderAsync(AppSettings.FolderName);
                }catch(System.IO.FileNotFoundException)
                {
                    await folder.CreateFolderAsync(AppSettings.FolderName);
                }

                Camera = CameraFactory.Get(XmlSettings.CameraType);
                Storage = StorageFactory.Get(XmlSettings.StorageProvider);

                await Camera.Initialize();

                // Try to log into OneDrive using existing Access Token in settings file
                if (Storage.GetType() == typeof(OneDrive))
                {
                    var oneDrive = App.Controller.Storage as OneDrive;

                    if (oneDrive != null)
                    {
                        if (!oneDrive.IsLoggedIn())
                        {
                            try
                            {
                                await oneDrive.AuthorizeWithRefreshToken(XmlSettings.OneDriveRefreshToken);
                            }
                            catch (Exception ex)
                            {
                                Debug.WriteLine(ex.Message);

                                // Log telemetry event about this exception
                                var events = new Dictionary<string, string> { { "Controller", ex.Message } };
                                App.Controller.TelemetryClient.TrackEvent("FailedToLoginOneDrive", events);
                            }
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

                IsInitialized = true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Controller.Initialize() Error: " + ex.Message);

                // Log telemetry event about this exception
                var events = new Dictionary<string, string> { { "Controller", ex.Message } };
                App.Controller.TelemetryClient.TrackEvent("FailedToInitialize", events);
            }
        }

        /// <summary>
        /// Disposes the file upload and deletion timers, camera, and storage
        /// </summary>
        public void Dispose()
        {
            try
            {
                uploadPicturesTimer?.Stop();
                deletePicturesTimer?.Stop();
                Camera?.Dispose();
                Storage?.Dispose();
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Controller.Dispose(): " + ex.Message);

                // Log telemetry event about this exception
                var events = new Dictionary<string, string> { { "Controller", ex.Message } };
                App.Controller.TelemetryClient.TrackEvent("FailedToDispose", events);
            }

            IsInitialized = false;
        }

        private void uploadPicturesTimer_Tick(object sender, object e)
        {
            uploadPicturesTimer.Stop();

            try
            {
                Storage.UploadPictures(cameras[0]);
            }catch(Exception ex)
            {
                Debug.WriteLine("uploadPicturesTimer_Tick() Exception: " + ex.Message);

                // Log telemetry event about this exception
                var events = new Dictionary<string, string> { { "Controller", ex.Message } };
                App.Controller.TelemetryClient.TrackEvent("FailedToUploadPicture", events);
            }

            uploadPicturesTimer.Start();
        }

        private void deletePicturesTimer_Tick(object sender, object e)
        {
            Storage.DeleteExpiredPictures(cameras[0]);
        }
    }
}
