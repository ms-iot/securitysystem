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
        private int isCapturing;
        private bool isEnabled;
        
        public bool IsEnabled
        {
            get
            {
                return isEnabled;
            }

            set
            {
                isEnabled = value;
            }
        }

        /*******************************************************************************************
        * PUBLIC METHODS
        *******************************************************************************************/
        public async Task Initialize()
        {
            webcam = new UsbCamera();
            try
            {
                await webcam.InitializeAsync();
                this.isEnabled = true;
            }
            catch (Exception ex)
            {
                var events = new Dictionary<string, string> { { "UsbCamera", ex.Message } };
                TelemetryHelper.TrackEvent("FailedToInitializeMediaCapture", events);
            }

            //Initialize PIR Sensor
            pirSensor = new PirSensor(App.Controller.XmlSettings.GpioMotionPin, PirSensor.SensorType.ActiveLow);
            pirSensor.motionDetected += PirSensor_MotionDetected;

            Interlocked.Exchange(ref isCapturing, 0);
        }

        public async Task TriggerCapture()
        {
            await TakePhotoAsync();
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
            await TakePhotoAsync();
        }

        private async Task TakePhotoAsync()
        {
            if (!this.isEnabled)
                return;
            if (0 == Interlocked.CompareExchange(ref isCapturing, 1, 0))
            {
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
                Interlocked.Exchange(ref isCapturing, 0);
            }
            else
            {
                return;
            }
        }
    }
}


