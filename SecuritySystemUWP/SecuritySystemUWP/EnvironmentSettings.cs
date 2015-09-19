using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.Networking;
using Windows.Networking.Connectivity;

namespace SecuritySystemUWP
{
    /// <summary>
    /// Helper class to retrieve device environment settings
    /// </summary>
    public static class EnvironmentSettings
    {
        /// <summary>
        /// Returns the name of current device as a string
        /// </summary>
        public static string GetDeviceName()
        {
            // iterate hostnames to find device name
            var hostname = NetworkInformation.GetHostNames()
                .FirstOrDefault(x => x.Type == HostNameType.DomainName);
            if (hostname != null)
            {
                return hostname.CanonicalName;
            }
            // if not found
            return "Unknown";
        }

        public static string GetIPAddress()
        {
            // iterate hostnames to find ipv4 address
            var hostname = NetworkInformation.GetHostNames()
                .FirstOrDefault(
                x => x.IPInformation != null &&
                x.Type == HostNameType.Ipv4);
            if (hostname != null)
            {
                return hostname.DisplayName;
            }
            // if not found
            return "0.0.0.0";
        }

        /// <summary>
        /// Retrieves current operating system version
        /// </summary>
        /// <returns>OS version</returns>
        public static string GetOSVersion()
        {
            ulong version = 0;
            // grab and parse OS version
            if (ulong.TryParse(Windows.System.Profile.AnalyticsInfo.VersionInfo.DeviceFamilyVersion, out version))
            {
                return String.Format(CultureInfo.InvariantCulture, "{0}.{1}.{2}.{3}",
                    (version & 0xFFFF000000000000) >> 48,
                    (version & 0x0000FFFF00000000) >> 32,
                    (version & 0x00000000FFFF0000) >> 16,
                    version & 0x000000000000FFFF);
            }
            // if not found
            return "0.0.0.0";
        }

        /// <summary>
        /// Retrieves current application version
        /// </summary>
        /// <returns>App version</returns>
        public static string GetAppVersion()
        {
            return string.Format(CultureInfo.InvariantCulture, "{0}.{1}.{2}.{3}",
                    Package.Current.Id.Version.Major,
                    Package.Current.Id.Version.Minor,
                    Package.Current.Id.Version.Build,
                    Package.Current.Id.Version.Revision);
        }

    }
}
