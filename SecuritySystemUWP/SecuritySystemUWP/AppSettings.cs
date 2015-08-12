using System;
using System.IO;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Windows.Storage;
using Windows.Storage.Streams;

namespace SecuritySystemUWP
{
    public class AppSettings
    {
        public string MicrosoftAlias;
        public int NumberOfCameras = 1;
        public string CameraType = "dlink";
        public string StorageProvider = "OneDrive";
        public string AzureAccountName = "SecuritySystemPictures";
        public string AzureAccessKey = "****";
        public string OneDriveClientId = "****";
        public string OneDriveClientSecret = "****";
        public int StorageDuration = 7;
        public string FolderName = "imagecontainer";

        public static async Task SaveAsync(AppSettings settings, string filename)
        {
            StorageFile sessionFile = await KnownFolders.DocumentsLibrary.CreateFileAsync(filename, CreationCollisionOption.ReplaceExisting);
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
                StorageFile sessionFile = await KnownFolders.DocumentsLibrary.CreateFileAsync(filename, CreationCollisionOption.OpenIfExists);
                if (sessionFile == null)
                {
                    return new AppSettings();
                }
                IInputStream sessionInputStream = await sessionFile.OpenReadAsync();
                var serializer = new XmlSerializer(typeof(AppSettings), new Type[] { typeof(AppSettings) });
                AppSettings temp = (AppSettings)serializer.Deserialize(sessionInputStream.AsStreamForRead());
                sessionInputStream.Dispose();

                return temp;
            }
            catch (Exception)
            {
                // If settings.xml file is corrupted and cannot be read - behave as if it does not exist.
                return new AppSettings();
            }
        }
    }
}