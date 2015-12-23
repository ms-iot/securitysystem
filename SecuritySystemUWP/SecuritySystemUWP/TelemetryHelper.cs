using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights;

namespace SecuritySystemUWP
{
    internal static class TelemetryHelper
    {
        /// <summary>
        /// Allows tracking page views, exceptions and other telemetry through the Microsoft Application Insights service.
        /// </summary>
        internal static Microsoft.ApplicationInsights.TelemetryClient _TelemetryClient = new TelemetryClient();


        private static void AddCommonProperties(ref IDictionary<string, string> properties)
        {
            
            properties.Add("Custom_AppVersion", EnvironmentSettings.GetAppVersion());
            properties.Add("Custom_OSVersion", EnvironmentSettings.GetOSVersion());

#if MS_INTERNAL_ONLY // do not send this app insights telemetry data for external customers
            properties.Add("userAlias", App.Controller.XmlSettings.MicrosoftAlias);
            properties.Add("Custom_DeviceName", EnvironmentSettings.GetDeviceName());
            properties.Add("Custom_IPAddress", EnvironmentSettings.GetIPAddress());
#endif
        }

        internal static void TrackEvent(string eventName)
        {
            Dictionary<string, string> properties = new Dictionary<string, string>();
            TrackEvent(eventName, properties);
        }

        internal static void TrackEvent(string eventName, IDictionary<string, string> properties)
        {
            AddCommonProperties(ref properties);
            _TelemetryClient.TrackEvent(eventName, properties);
        }

        internal static void TrackMetric(string metricName, double value)
        {
            Dictionary<string, string> properties = new Dictionary<string, string>();
            TrackMetric(metricName, value, properties);
        }

        internal static void TrackMetric(string metricName, double value, IDictionary<string, string> properties)
        {
            AddCommonProperties(ref properties);
            _TelemetryClient.TrackMetric(metricName, value, properties);
        }

        internal static void TrackException(Exception exception)
        {
            Dictionary<string, string> properties = new Dictionary<string, string>();
            TrackException(exception, properties);
        }

        internal static void TrackException(Exception exception, IDictionary<string, string> properties)
        {
            AddCommonProperties(ref properties);
            _TelemetryClient.TrackException(exception, properties);
        }
    }
}
