using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights;

namespace SecuritySystemUWP
{
    /// <summary>
    /// Helper class for app insights telemetry events.
    /// Allows tracking page views, exceptions and other telemetry through the Microsoft Application Insights service.
    /// </summary>
    internal static class TelemetryHelper
    {
        // app insights telemetry object
        internal static Microsoft.ApplicationInsights.TelemetryClient _TelemetryClient = new TelemetryClient();

        /// <summary>
        /// Helper private method to add additional common properties to all telemetry events via ref param
        /// </summary>
        /// <param name="properties">original properties to add to</param>
        private static void AddCommonProperties(ref IDictionary<string, string> properties)
        {
            // add common properties as long as they don't already exist in the original properties passed in
            if (!properties.ContainsKey("Custom_AppVersion"))
            {
                properties.Add("Custom_AppVersion", EnvironmentSettings.GetAppVersion());
            }
            if (!properties.ContainsKey("Custom_OSVersion"))
            {
                properties.Add("Custom_OSVersion", EnvironmentSettings.GetOSVersion());
            }
#if MS_INTERNAL_ONLY // Do not send this app insights telemetry data for external customers. Microsoft only.
            if (!properties.ContainsKey("userAlias"))
            {
                properties.Add("userAlias", App.Controller.XmlSettings.MicrosoftAlias);
            }
            if (!properties.ContainsKey("Custom_DeviceName"))
            {
                properties.Add("Custom_DeviceName", EnvironmentSettings.GetDeviceName());
            }
            if (!properties.ContainsKey("Custom_IPAddress"))
            {
                properties.Add("Custom_IPAddress", EnvironmentSettings.GetIPAddress());
            }
#endif
        }

        /// <summary>
        /// Log and track custom app insights event with global common properities
        /// See https://azure.microsoft.com/en-us/documentation/articles/app-insights-api-custom-events-metrics/#api-summary
        /// </summary>
        /// <param name="eventName"></param>
        internal static void TrackEvent(string eventName)
        {
            Dictionary<string, string> properties = new Dictionary<string, string>();
            // call overloaded internal helper method
            TrackEvent(eventName, properties);
        }

        /// <summary>
        /// Log and track custom app insights event with global common properities
        /// See https://azure.microsoft.com/en-us/documentation/articles/app-insights-api-custom-events-metrics/#api-summary
        /// </summary>
        /// <param name="eventName"></param>
        /// <param name="properties"></param>
        internal static void TrackEvent(string eventName, IDictionary<string, string> properties)
        {
            // add common properties
            AddCommonProperties(ref properties);
            _TelemetryClient.TrackEvent(eventName, properties);
        }

        /// <summary>
        /// Log and track custom app insights metrics with global common properities
        /// See https://azure.microsoft.com/en-us/documentation/articles/app-insights-api-custom-events-metrics/#api-summary
        /// </summary>
        /// <param name="metricName"></param>
        /// <param name="value"></param>
        internal static void TrackMetric(string metricName, double value)
        {
            Dictionary<string, string> properties = new Dictionary<string, string>();
            // call overloaded internal helper method
            TrackMetric(metricName, value, properties);
        }

        /// <summary>
        /// Log and track custom app insights metrics with global common properities
        /// See https://azure.microsoft.com/en-us/documentation/articles/app-insights-api-custom-events-metrics/#api-summary
        /// </summary>
        /// <param name="metricName"></param>
        /// <param name="value"></param>
        /// <param name="properties"></param>
        internal static void TrackMetric(string metricName, double value, IDictionary<string, string> properties)
        {
            // add common properties
            AddCommonProperties(ref properties);
            _TelemetryClient.TrackMetric(metricName, value, properties);
        }

        /// <summary>
        /// Log and track custom app insights exception event with global common properities
        /// See https://azure.microsoft.com/en-us/documentation/articles/app-insights-api-custom-events-metrics/#api-summary
        /// </summary>
        /// <param name="exception"></param>
        internal static void TrackException(Exception exception)
        {
            Dictionary<string, string> properties = new Dictionary<string, string>();
            // call overloaded internal helper method
            TrackException(exception, properties);
        }

        /// <summary>
        /// Log and track custom app insights exception event with global common properities
        /// See https://azure.microsoft.com/en-us/documentation/articles/app-insights-api-custom-events-metrics/#api-summary
        /// </summary>
        /// <param name="exception"></param>
        /// <param name="properties"></param>
        internal static void TrackException(Exception exception, IDictionary<string, string> properties)
        {
            // add common properties
            AddCommonProperties(ref properties);
            _TelemetryClient.TrackException(exception, properties);
        }
    }
}
