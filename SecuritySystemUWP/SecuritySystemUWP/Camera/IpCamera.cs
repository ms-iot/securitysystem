using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SecuritySystemUWP
{
    class IpCamera : ICamera
    {
        public bool IsEnabled
        {
            get
            {
                // We are unable to disable the IPCamera
                return true;
            }

            set
            {
            }
        }

        //The recommended IP camera does not require any initilization
        public async Task Initialize()
        {
        }

        //The recommended IP camera does not have anything to dispose
        public void Dispose()
        {
        }

        // We are unable to manually trigger the IPCamera
        public async Task TriggerCapture()
        {
            return;
        }
    }
}
