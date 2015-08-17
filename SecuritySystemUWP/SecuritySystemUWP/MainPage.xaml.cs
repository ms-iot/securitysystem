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
        private string[] cameras = { "Cam1" };
        private static bool started = false;

        public MainPage()
        {
            this.InitializeComponent();
        }

        private async void RunningToggle_Click(object sender, RoutedEventArgs e)
        {
            if (!started)
            {
                await App.Controller.Initialize();
                started = true;
                App.Controller.XmlSettings = await AppSettings.RestoreAsync("Settings.xml");
                this.Frame.Navigate(App.Controller.Storage.StorageStartPage());
            }
            else
            {
                App.Controller.Dispose();
                started = false;
                this.Frame.Navigate(typeof(MainPage));
            }
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            RunningToggle.Content = started ? "Stop" : "Start";
            this.Frame.Navigate(App.Controller.Storage.StorageStartPage());
        }
    }
}
