using System;
using System.Collections.Generic;
using System.Threading;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using Windows.Networking.Connectivity;
using Windows.UI.Core;

namespace SecuritySystemUWP
{
    /// <summary>
    /// Main page of the app
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private CoreDispatcher _MainPageDispatcher = null;
        

        public MainPage()
        {
            this.InitializeComponent();

            _MainPageDispatcher = Window.Current.Dispatcher;

            // network status change event
            NetworkInformation.NetworkStatusChanged += NetworkInformation_NetworkStatusChanged;

            // get static device environment info and display it in UI           
            appVersionValueTextBlock.Text = EnvironmentSettings.GetAppVersion();
            OSVersionValueTextBlock.Text = EnvironmentSettings.GetOSVersion();

            UpdateNetworkInfo();

        }

        /// <summary>
        /// On network status change update network info and display on main page
        /// </summary>
        /// <param name="sender"></param>
        private async void NetworkInformation_NetworkStatusChanged(object sender)
        {
            await _MainPageDispatcher.RunAsync(CoreDispatcherPriority.Low, () =>
            {
                UpdateNetworkInfo();
            });
        }

        /// <summary>
        /// Get dynamic network information about the device and display it
        /// </summary>
        private void UpdateNetworkInfo()
        {
            string ipAddress = EnvironmentSettings.GetIPAddress();
            string instructions = "";
            // check if ip address is valid
            if (ipAddress == "0.0.0.0")
            {
                ipAddress = "Invalid IP Address: 0.0.0.0";
                instructions = "Setup Instructions: Please ensure your device has a valid ip address first.";
            }
            else {
                instructions = "Setup Instructions: To configure this security system please go to URL http://" + ipAddress + ":8000 on a browser.";
            }

            // update UI
            deviceNameValueTextBlock.Text = EnvironmentSettings.GetDeviceName();
            ipAddressValueTextBlock.Text = ipAddress;
            instructionsTextBlock.Text = instructions;
        }
    }
}
