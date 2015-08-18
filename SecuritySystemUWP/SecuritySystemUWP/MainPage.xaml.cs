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

        public MainPage()
        {
            this.InitializeComponent();
        }
    }
}
