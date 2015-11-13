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
        public static ICamera Get(CameraType type)
        {
            switch (type)
            {
                case CameraType.Usb: return new Webcam(); //If using webcam
                case CameraType.Ip: return new IpCamera(); //If using D-LINK DCS930 or DCS932
                default: throw new ArgumentNullException("Camera Type not supported. Set Camera Type in Settings.");
            }
        }
    }
}
