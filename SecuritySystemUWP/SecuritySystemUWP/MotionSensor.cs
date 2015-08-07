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
        public bool isMotionDetected;
        private GpioPin motionSensorPin;
        private GpioPinValue pinValue;
        public void Initialize()
        {
            var gpioController = GpioController.GetDefault();
            motionSensorPin = gpioController.OpenPin(Config.GpioMotionPin);
            motionSensorPin.SetDriveMode(GpioPinDriveMode.Input);
            motionSensorPin.ValueChanged += motionDetected;
        }
        public void Dispose()
        {
            motionSensorPin.Dispose();
        }

        private void motionDetected(GpioPin s, GpioPinValueChangedEventArgs e)
        {
            pinValue = motionSensorPin.Read();
            isMotionDetected = (e.Edge == GpioPinEdge.RisingEdge);
        }
    }
}
