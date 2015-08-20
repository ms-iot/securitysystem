using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Gpio;

namespace SecuritySystemUWP
{
    public class MotionSensor
    {
        public GpioPin motionSensorPin;
        public event EventHandler<GpioPinValueChangedEventArgs> OnChanged;
        public MotionSensor()
        {
            var gpioController = GpioController.GetDefault();
            motionSensorPin = gpioController.OpenPin(App.Controller.XmlSettings.GpioMotionPin);
            motionSensorPin.SetDriveMode(GpioPinDriveMode.Input);
            motionSensorPin.ValueChanged += MotionSensorPin_ValueChanged;
        }
        public void Dispose()
        {
            motionSensorPin.Dispose();
        }
        private void MotionSensorPin_ValueChanged(GpioPin sender, GpioPinValueChangedEventArgs args)
        {
            if (OnChanged != null)
            {
                OnChanged(this, args);
            }
        }
    }
}
