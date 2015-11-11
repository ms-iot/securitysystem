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
        private AsyncSemaphore captureLock;
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

            Interlocked.Exchange(ref isCapturing, 0);
            captureLock = new AsyncSemaphore(1);
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
            if (0 == Interlocked.CompareExchange(ref isCapturing, 1, 0))
            {
                await captureLock.WaitAsync();
                try
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
                finally
                {
                    captureLock.Release();
                }
            }
            else
            {
                return;
            }
        }
    }
}


