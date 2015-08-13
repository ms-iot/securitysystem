using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Xaml.Controls;

namespace SecuritySystemUWP
{
    public static class CameraFactory
    {
        public static ICamera Get(string type)
        {
            if(object.ReferenceEquals(type, null))
            {
                throw new NotSupportedException("Set CameraType in Settings.");
            }

            switch (type.ToLower())
            {
                case "usb": return new UsbCamera();
                case "ip": return new IpCamera();
                default: throw new ArgumentNullException("Camera Type not supported. Set Camera Type in Settings.");
            }
        }
    }
}
