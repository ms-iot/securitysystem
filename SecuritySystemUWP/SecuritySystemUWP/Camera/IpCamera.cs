using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SecuritySystemUWP
{
    class IpCamera : ICamera
    {
        //The recommended IP camera does not require any initilization
        public async Task Initialize()
        {
        }

        //The recommended IP camera does not have anything to dispose
        public void Dispose()
        {
        }
    }
}
