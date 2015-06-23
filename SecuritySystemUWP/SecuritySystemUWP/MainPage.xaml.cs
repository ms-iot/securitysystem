using Windows.UI.Xaml.Controls;
using DeviceProviders;
using System.Threading.Tasks;
using System.Diagnostics;
using System;
using Windows.Web.Http;
using Windows.Devices.Gpio;
using System.IO;
using Windows.UI.Xaml.Media.Imaging;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System.Collections.Generic;
using System.Linq;
using Windows.UI.Xaml;
using Windows.Storage.Search;
using Windows.Storage;
using System.Threading;
// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace SecuritySystemUWP
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        AllJoynProvider provider;
        private string accountName = "securitysystemstorage";
        private string accountKey = "BzbVAphWQwVAk9/ooBS14G5zzKABfIAEqAhaDC6MU1Be0ReBTyymqB3ibZmm0VXniMP7Uw6Y5bewWib6tuKqPw==";
        private string blobType = "BlockBlob";
        private string sharedKeyAuthorizationScheme = "SharedKey";

        private const int GPIO_NUM_MOTIONSENSOR = 18;
        private GpioPin motionSensorPin;
        private GpioPinValue pinValue;
        private bool isMotionDetected;

        private DispatcherTimer uploadPicturesTimer;
        private readonly TimeSpan uploadPicturesIntervalDuration = new TimeSpan(0, 0, 10);
        private static Mutex uploadPicturesMutexLock = new Mutex();

        public MainPage()
        {
            this.InitializeComponent();
            Initialize();
        }

        private async void Initialize()
        {

            startMotionSensor();

            uploadPicturesTimer = new DispatcherTimer();
            uploadPicturesTimer.Interval = uploadPicturesIntervalDuration;
            uploadPicturesTimer.Tick += uploadPicturesTimer_Tick;
            uploadPicturesTimer.Start();


            // Create and register the AllJoyn provider
            provider = new AllJoynProvider();
            provider.Services.VectorChanged += Services_VectorChanged;
            provider.Start();
            Debug.WriteLine("AllJoynProvider Started");

            Uri imageUrl = new Uri("http://www.microsoft.com/global/en-us/news/publishingimages/logos/MSFT_logo_Web.jpg");

            var httpClient = new HttpClient();
            Windows.Storage.Streams.IInputStream stream = await httpClient.GetInputStreamAsync(imageUrl);
            //var memStream = new MemoryStream();
            //Stream testStream = stream.AsStreamForRead();
            //await testStream.CopyToAsync(memStream);
            //memStream.Position = 0;
            //var bitmap = new BitmapImage();
            //bitmap.SetSource(memStream.AsRandomAccessStream());
            //image.Source = bitmap;

            //// Retrieve storage account from connection string
            //CloudStorageAccount storageAccount = CloudStorageAccount.Parse("DefaultEndpointsProtocol=https;AccountName=securitysystemstorage;AccountKey=BzbVAphWQwVAk9/ooBS14G5zzKABfIAEqAhaDC6MU1Be0ReBTyymqB3ibZmm0VXniMP7Uw6Y5bewWib6tuKqPw==");
            //// Create the blob client
            //CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();
            //// Retrieve a reference to a container
            //CloudBlobContainer blobContainer = blobClient.GetContainerReference("imageContainer");
            //// Create the container if it doesn't already exist
            //await blobContainer.CreateIfNotExistsAsync();
            //// Retrieve reference to a blob named "myblob".
            //CloudBlockBlob blockBlob = blobContainer.GetBlockBlobReference("msft_logo.jpg");
            //// Create or overwrite the "myblob" blob with contents from a local file.
            //await blockBlob.UploadFromStreamAsync(stream);

            MakeAzureBLOBRequest();
        }

        private static void Separator()
        {
            Debug.WriteLine("----------------------------------------");
        }

        private async void MakeAzureBLOBRequest()
        {
            BlobHelper BlobHelper = new BlobHelper(accountName, accountKey);
            
            Separator();
            Uri imageUrl2 = new Uri("http://www.microsoft.com/global/en-us/news/publishingimages/logos/MSFT_logo_Web.jpg");
            var httpClient2 = new HttpClient();
            Windows.Storage.Streams.IInputStream stream2 = await httpClient2.GetInputStreamAsync(imageUrl2);
            var memStream2 = new MemoryStream();
            Stream testStream2 = stream2.AsStreamForRead();
            await testStream2.CopyToAsync(memStream2);
            memStream2.Position = 0;

            Debug.WriteLine("Put blob ");

            string imageName = DateTime.UtcNow.Ticks.ToString() + ".jpg";
            Debug.WriteLine(imageName);
            if (await BlobHelper.PutBlob("imagecontainer", imageName, memStream2))
            {
                Debug.WriteLine("true");
            }
            else
            {
                Debug.WriteLine("false");
            }
        }

        private void Services_VectorChanged(Windows.Foundation.Collections.IObservableVector<IService> sender, Windows.Foundation.Collections.IVectorChangedEventArgs @event)
        {
            Debug.WriteLine("----------------------------------------------");
            Debug.WriteLine("AllJoyn Provider Services Changed");
            Debug.WriteLine("Services: " + sender.Count.ToString());
            foreach (var service in sender)
            {
                Debug.WriteLine("Service Name: " + service.Name);
                Debug.WriteLine("Service About: " + service.AboutData);
                Debug.WriteLine("BusObjects: " + service.Objects.Count.ToString());
                // BUG: This portion will cause it to timeout
                //service.Objects.VectorChanged += Objects_VectorChanged;
                //foreach (var busObject in service.Objects)
                //{
                //    Debug.WriteLine("Interfaces: " + busObject.Interfaces.Count.ToString());
                //    busObject.Interfaces.VectorChanged += Interfaces_VectorChanged;
                //    foreach (var iface in busObject.Interfaces)
                //    {
                //        Debug.WriteLine(iface.IntrospectXml.ToString());
                //    }
                //}
            }
        }

        private void Objects_VectorChanged(Windows.Foundation.Collections.IObservableVector<IBusObject> sender, Windows.Foundation.Collections.IVectorChangedEventArgs @event)
        {
            foreach (var busObject in sender)
            {
                Debug.WriteLine("Interfaces: " + busObject.Interfaces.Count.ToString());
                busObject.Interfaces.VectorChanged += Interfaces_VectorChanged;
            }
        }

        private void Interfaces_VectorChanged(Windows.Foundation.Collections.IObservableVector<IInterface> sender, Windows.Foundation.Collections.IVectorChangedEventArgs @event)
        {
            foreach (var iface in sender)
            {
                Debug.WriteLine(iface.IntrospectXml.ToString());
            }
        }

        private static async void uploadPicturesTimer_Tick(object sender, object e)
        {
            uploadPicturesMutexLock.WaitOne();

            try
            {
                QueryOptions querySubfolders = new QueryOptions();
                querySubfolders.FolderDepth = FolderDepth.Deep;
               
                StorageFolder cacheFolder = KnownFolders.PicturesLibrary;
                var result = cacheFolder.CreateFileQueryWithOptions(querySubfolders);
                var files = await result.GetFilesAsync();

                foreach (StorageFile file in files)
                {

                    //send photo to Azure

                    await file.DeleteAsync();
                }

            }
            catch (Exception ex)
            {
                Debug.WriteLine("Exception in uploadPicturesTimer_Tick() " + ex.Message);
            }
            finally
            {
                uploadPicturesMutexLock.ReleaseMutex();
            }
        }

        private async void startMotionSensor()
        {
            var gpioController = GpioController.GetDefault();

            motionSensorPin = gpioController.OpenPin(GPIO_NUM_MOTIONSENSOR);
            motionSensorPin.SetDriveMode(GpioPinDriveMode.Input);
            motionSensorPin.ValueChanged += (s, e) =>
            {
                pinValue = motionSensorPin.Read();
                isMotionDetected = (e.Edge == GpioPinEdge.RisingEdge);
            };

        }
    }
}
