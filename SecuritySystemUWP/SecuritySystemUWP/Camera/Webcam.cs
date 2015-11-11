using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Windows.Devices.Gpio;
using Windows.Storage;
using System.Collections.Generic;
using Microsoft.Maker.Devices.Media.UsbCamera;
using Microsoft.Maker.Devices.Gpio.PirSensor;

namespace SecuritySystemUWP
{
    public class Webcam : ICamera
    {
        private UsbCamera webcam;
        private PirSensor pirSensor;
        private bool isCapturing;
        /*******************************************************************************************
        * PUBLIC METHODS
        *******************************************************************************************/
        public async Task Initialize()
        {
            webcam = new UsbCamera();
            try
            {
                await webcam.InitializeAsync();
            }
            catch (Exception ex)
            {
                var events = new Dictionary<string, string> { { "UsbCamera", ex.Message } };
                App.Controller.TelemetryClient.TrackEvent("FailedToInitializeMediaCapture", events);
            }

            //Initialize PIR Sensor
            pirSensor = new PirSensor(App.Controller.XmlSettings.GpioMotionPin, PirSensor.SensorType.ActiveLow);
            pirSensor.motionDetected += PirSensor_MotionDetected;

            isCapturing = false;
        }

        public void Dispose()
        {
            webcam?.Dispose();
            pirSensor?.Dispose();
        }

        /*******************************************************************************************
        * PRIVATE METHODS
        ********************************************************************************************/
        private async void PirSensor_MotionDetected(object sender, GpioPinValueChangedEventArgs e)
        {
            if (!isCapturing)
            {
                await TakePhotoAsync();
            }
        }

        private async Task TakePhotoAsync()
        {
            isCapturing = true;
            //Use current time in ticks as image name
            string imageName = DateTime.UtcNow.Ticks.ToString() + ".jpg";

            //Get folder to store images
            var cacheFolder = KnownFolders.PicturesLibrary;
            cacheFolder = await cacheFolder.GetFolderAsync("securitysystem-cameradrop");
            StorageFile temp = null;
            try
            {
                temp = await webcam.CapturePhoto();
                if (temp != null)
                {
                    await temp.RenameAsync(imageName);
                    await temp.MoveAsync(cacheFolder);
                }
            }
            catch (Exception e)
            {
                if (temp != null)
                {
                    await temp.DeleteAsync();
                }
            }
            isCapturing = false;
        }
    }
}


