using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Windows.Storage;
using Windows.Storage.Streams;

namespace SecuritySystemUWP
{
    public enum StorageProvider
    {
        OneDrive,
        Azure,
        Local
    }

    public enum CameraType
    {
        Ip,
        Usb
    }

    public class AppSettings
    {
        // Any fields without descriptions will not appear in the web interface

        [Description("The Microsoft alias of the user")]
        public string MicrosoftAlias = "";

        // Future add-on, should remain unchanged for now.
        public int NumberOfCameras = 1;

        [Description("Type of camera you're using (e.g. IP, USB)")]
        public CameraType CameraType = CameraType.Usb;

        [Description("This is the storage provider that you will use to store your photos")]
        public StorageProvider StorageProvider = StorageProvider.Local;

        [Description("Azure Account Name - Only required if you selected Azure as the Storage Provider")]
        public string AzureAccountName = "SecuritySystemPictures";

        [Description("Azure Access Key - Only required if you selected Azure as the Storage Provider")]
        public string AzureAccessKey = "****";

        [Description("OneDrive Client ID - Only required if you selected OneDrive as the Storage Provider")]
        public string OneDriveClientId = "****";

        [Description("OneDrive Secret - Only required if you selected OneDrive as the Storage Provider")]
        public string OneDriveClientSecret = "****";

        [Description("Number of days to store your pictures before they are deleted")]
        public int StorageDuration = 7;
        
        [Description("GPIO input pin for the motion sensor signal - Only required if you are using a motion sensor")]
        public int GpioMotionPin = 4;

        // Obtained from OneDrive Login
        public string OneDriveAccessToken = "";
        public string OneDriveRefreshToken = "";

        //The following values are not changed, and not read in from the xml file
        public const string FolderName = "securitysystem-cameradrop";
        public const string AzureConnectionSettings = "DefaultEndpointsProtocol=https;AccountName={0};AccountKey={1}";
        public const string OneDriveRedirectUrl = "https://login.live.com/oauth20_desktop.srf";
        public const string OneDriveLoginUrl = "https://login.live.com/oauth20_authorize.srf?client_id={0}&scope={1}&response_type=code&redirect_uri={2}";
        public const string OneDriveLogoutUrl = "https://login.live.com/oauth20_logout.srf?client_id={0}&redirect_uri={1}";
        public const string OneDriveScope = "wl.offline_access onedrive.readwrite";
        public const string OneDriveRootUrl = "https://api.onedrive.com/v1.0/drive/root:";
        public const string OneDriveTokenUrl = "https://login.live.com/oauth20_token.srf";
        public const string OneDriveTokenContent = "client_id={0}&redirect_uri={1}&client_secret={2}&{3}={4}&grant_type={5}";
        public const string ImageNameFormat = "{0}/{1}_{2}.jpg";

        public static readonly StorageFolder SettingsFolder = ApplicationData.Current.LocalFolder;

        public static async Task SaveAsync(AppSettings settings, string filename)
        {
            StorageFile sessionFile = await SettingsFolder.CreateFileAsync(filename, CreationCollisionOption.ReplaceExisting);
            IRandomAccessStream sessionRandomAccess = await sessionFile.OpenAsync(FileAccessMode.ReadWrite);
            IOutputStream sessionOutputStream = sessionRandomAccess.GetOutputStreamAt(0);
            var serializer = new XmlSerializer(typeof(AppSettings), new Type[] { typeof(AppSettings) });
            serializer.Serialize(sessionOutputStream.AsStreamForWrite(), settings);
            sessionRandomAccess.Dispose();
            await sessionOutputStream.FlushAsync();
            sessionOutputStream.Dispose();
        }

        // Deserialize app settings from XML format asynchronously; leave settings be default if file does not exist.
        public static async Task<AppSettings> RestoreAsync(string filename)
        {
            try
            {
                StorageFile sessionFile = await SettingsFolder.CreateFileAsync(filename, CreationCollisionOption.OpenIfExists);
                if (sessionFile == null)
                {
                    return new AppSettings();
                }
                IInputStream sessionInputStream = await sessionFile.OpenReadAsync();
                var serializer = new XmlSerializer(typeof(AppSettings));
                AppSettings temp = (AppSettings)serializer.Deserialize(sessionInputStream.AsStreamForRead());
                sessionInputStream.Dispose();

                return temp;
            }
            catch (Exception e)
            {
                Debug.WriteLine("AppSettings.RestoreAsync(): " + e.Message);

                // If settings.xml file is corrupted and cannot be read - behave as if it does not exist.
                return new AppSettings();
            }
        }
    }

    [AttributeUsage(AttributeTargets.All)]
    public class DescriptionAttribute : Attribute
    {
        public DescriptionAttribute(string description)
        {
            Description = description;
        }

        public string Description { get; private set; }
    }
}
