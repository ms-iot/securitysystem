using com.microsoft.maker.SecuritySystem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.Devices.AllJoyn;
using Windows.Foundation;
using Windows.Storage;
using Windows.Storage.Search;

namespace SecuritySystemUWP
{
    class AllJoynManager : ISecuritySystemService
    {
        private AllJoynBusAttachment allJoynBusAttachment;
        private SecuritySystemProducer producer;

        private ICamera camera;
        private StorageFolder dropFolder;
        private StorageFile newestImage;
        private StorageFileQueryResult dropFolderWatcher;
        private IStorage storage;


        public async Task Initialize(ICamera camera, IStorage storage)
        {
            this.camera = camera;
            this.storage = storage;

            var cacheFolder = KnownFolders.PicturesLibrary;
            this.dropFolder = await cacheFolder.GetFolderAsync("securitysystem-cameradrop");
            this.dropFolderWatcher = dropFolder.CreateFileQuery();

            var images = await this.dropFolderWatcher.GetFilesAsync();
            var orderedImages = images.OrderByDescending(x => x.DateCreated);
            this.newestImage = orderedImages.FirstOrDefault();

            this.dropFolderWatcher.ContentsChanged += DropFolderWatcher_ContentsChanged;

            this.allJoynBusAttachment = new AllJoynBusAttachment();
            this.producer = new SecuritySystemProducer(this.allJoynBusAttachment);
            this.allJoynBusAttachment.AboutData.DefaultAppName = Package.Current.DisplayName;
            this.allJoynBusAttachment.AboutData.DefaultDescription = Package.Current.Description;
            this.allJoynBusAttachment.AboutData.DefaultManufacturer = Package.Current.Id.Publisher;
            this.allJoynBusAttachment.AboutData.SoftwareVersion = Package.Current.Id.Version.ToString();
            this.allJoynBusAttachment.AboutData.IsEnabled = true;
            this.producer.Service = this;
            this.producer.Start();
        }

        private async void DropFolderWatcher_ContentsChanged(IStorageQueryResultBase sender, object args)
        {
            var images = await this.dropFolder.GetFilesAsync();
            var orderedImages = images.OrderByDescending(x => x.DateCreated);
            var file = orderedImages.FirstOrDefault();

            if ((null != file) && (null == this.newestImage || file.Name != this.newestImage.Name))
            {
                this.newestImage = file;
                this.producer.EmitLastCaptureFileNameChanged();
                this.producer.EmitLastCaptureTimeChanged();
            }
        }

        public IAsyncOperation<SecuritySystemGetLastUploadTimeResult> GetLastUploadTimeAsync(AllJoynMessageInfo info)
        {
            return Task.Run(() =>
            {
                return SecuritySystemGetLastUploadTimeResult.CreateSuccessResult(this.storage.LastUploadTime.ToString());
            }).AsAsyncOperation();
        }

        public IAsyncOperation<SecuritySystemTriggerCaptureResult> TriggerCaptureAsync(AllJoynMessageInfo info)
        {
            return Task.Run(async () =>
            {
                await this.camera.TriggerCapture();
                return SecuritySystemTriggerCaptureResult.CreateSuccessResult();
            }).AsAsyncOperation();
        }

        public IAsyncOperation<SecuritySystemGetVersionResult> GetVersionAsync(AllJoynMessageInfo info)
        {
            return Task.Run(() =>
            {
                return SecuritySystemGetVersionResult.CreateSuccessResult(Package.Current.Id.Version.Major);
            }).AsAsyncOperation();
        }

        public IAsyncOperation<SecuritySystemGetIsEnabledResult> GetIsEnabledAsync(AllJoynMessageInfo info)
        {
            return Task.Run(() =>
            {
                return SecuritySystemGetIsEnabledResult.CreateSuccessResult(this.camera.IsEnabled);
            }).AsAsyncOperation();
        }

        public IAsyncOperation<SecuritySystemSetIsEnabledResult> SetIsEnabledAsync(AllJoynMessageInfo info, bool value)
        {
            return Task.Run(() =>
            {
                this.camera.IsEnabled = value;
                return SecuritySystemSetIsEnabledResult.CreateSuccessResult();
            }).AsAsyncOperation();
        }

        public IAsyncOperation<SecuritySystemGetLastCaptureFileNameResult> GetLastCaptureFileNameAsync(AllJoynMessageInfo info)
        {
            return Task.Run(() =>
            {
                var fileName = "";

                if (null != this.newestImage)
                {
                    fileName = this.newestImage.Name;
                }

                return SecuritySystemGetLastCaptureFileNameResult.CreateSuccessResult(fileName);
            }).AsAsyncOperation();
        }

        public IAsyncOperation<SecuritySystemGetLastCaptureTimeResult> GetLastCaptureTimeAsync(AllJoynMessageInfo info)
        {
            return Task.Run(() =>
            {
                var fileDate = "";

                if (null != this.newestImage)
                {
                    fileDate = this.newestImage.DateCreated.ToString();
                }

                return SecuritySystemGetLastCaptureTimeResult.CreateSuccessResult(fileDate);
            }).AsAsyncOperation();
        }
    }
}
