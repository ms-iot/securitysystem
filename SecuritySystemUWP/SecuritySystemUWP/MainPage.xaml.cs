using System;
using System.Collections.Generic;
using System.Threading;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace SecuritySystemUWP
{
    /// <summary>
    /// Main page of the app
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            string ipAddress = "";
            string instructions = "";

            this.InitializeComponent();

            // get device environment info and display it in UI
            deviceNameValueTextBlock.Text = EnvironmentSettings.GetDeviceName();
            ipAddress = EnvironmentSettings.GetIPAddress();
            ipAddressValueTextBlock.Text = ipAddress;
            appVersionValueTextBlock.Text = EnvironmentSettings.GetAppVersion();
            OSVersionValueTextBlock.Text = EnvironmentSettings.GetOSVersion();

            // instructions text with ip address
            instructions = "Setup Instructions: To configure this security system please go to URL http://" + ipAddress + ":8000 on a browser.";
            instructionsTextBlock.Text = instructions;

        }
    }
}
