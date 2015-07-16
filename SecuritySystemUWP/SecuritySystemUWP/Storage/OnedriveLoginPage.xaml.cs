using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.Web.Http;
using Windows.Web.Http.Headers;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace SecuritySystemUWP
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class OnedriveLoginPage : Page
    {
        public OnedriveLoginPage()
        {
            this.InitializeComponent();
            this.NavigationCacheMode = Windows.UI.Xaml.Navigation.NavigationCacheMode.Enabled;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            getAccessCode();

        }

        private void backButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            this.Frame.Navigate(typeof(MainPage));
        }

        private async void browser_NavigationCompleted(WebView sender, WebViewNavigationCompletedEventArgs args)
        {
             if (!args.Uri.AbsoluteUri.Contains("code="))
            {
                return;
            }

            string response = args.Uri.AbsoluteUri;
            int index = response.IndexOf("code=") + 5;
            string accessCode = response.Substring(index);
            await OneDrive.authorize(accessCode);
            this.Frame.Navigate(typeof(MainPage));
        }

        private void getAccessCode()
        {
            string uri = "https://login.live.com/oauth20_authorize.srf?client_id=" + OneDrive.clientId + "&scope=" + OneDrive.scope + "&response_type=code&redirect_uri=" + OneDrive.redirectUri;
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, new Uri(uri));
            browser.NavigateWithHttpRequestMessage(request);
        }

    }
}
