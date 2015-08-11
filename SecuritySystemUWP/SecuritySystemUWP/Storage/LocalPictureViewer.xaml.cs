using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Xaml.Media.Imaging;
using Windows.Storage;
// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace SecuritySystemUWP
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class LocalPictureViewer : Page
    {
        DispatcherTimer GetImageTimer;
        BitmapImage imageSource = new BitmapImage();
        IReadOnlyList<StorageFile> displayList;
        int currentIndexOfDisplay = 0;
        private static Mutex displayListMutexLock = new Mutex();
        public LocalPictureViewer()
        {
            this.InitializeComponent();
            this.NavigationCacheMode = Windows.UI.Xaml.Navigation.NavigationCacheMode.Enabled;
            this.RightTapped += SlideshowPage_RightTapped;
            this.Tapped += SlideshowPage_Tapped;
            GetImageTimer = new DispatcherTimer();
            GetImageTimer.Interval = TimeSpan.FromSeconds(30);
            GetImageTimer.Tick += GetImageTimer_Tick;
            displayListMutexLock.WaitOne();
            displayListMutexLock.ReleaseMutex();
        }
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            StartSlideShow();
        }
        private async void StartSlideShow()
        {
            await GetImages();
            GetImageTimer.Start();
            await DisplayImage();
        }
        private async void GetImageTimer_Tick(object sender, object e)
        {
            await GetImages(); // move to the next one in display queue
        }
        private void backButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            this.Frame.Navigate(typeof(MainPage));
        }

        private async void SlideshowPage_Tapped(object sender, Windows.UI.Xaml.Input.TappedRoutedEventArgs e)
        {
            if (displayList.Count > currentIndexOfDisplay)
            {
                currentIndexOfDisplay = ++currentIndexOfDisplay;
                await DisplayImage();
            }
        }

        private async void SlideshowPage_RightTapped(object sender, Windows.UI.Xaml.Input.RightTappedRoutedEventArgs e)
        {
            if (0 < currentIndexOfDisplay)
            {
                currentIndexOfDisplay = --currentIndexOfDisplay;
                await DisplayImage();
            }
        }
        private async Task DisplayImage()
        {
            displayListMutexLock.WaitOne();
            try {
                if (displayList.Count == 0)
                {
                    return;
                }
                StorageFile currentImage = displayList[currentIndexOfDisplay];
                if (currentImage.FileType.ToLower().Contains("jpg"))
                {
                    var stream = await currentImage.OpenAsync(FileAccessMode.Read);
                    imageSource = new BitmapImage();
                    await imageSource.SetSourceAsync(stream);
                    imageInstance.Source = imageSource;

                    // display picture details
                    string datecreated = currentImage.DateCreated.ToString();
                    pictureDetails.Text = datecreated;
                }
                displayListMutexLock.ReleaseMutex();
            }
            catch (Exception e)
            {
                Debug.WriteLine("Exception in displaying images " + e.Message);
                displayListMutexLock.ReleaseMutex();
            }
        }
       
        private async Task GetImages()
        {
            try
            {
                // make access to shared resource displayList<> thread safe
                displayListMutexLock.WaitOne();
                StorageFolder cacheFolder = Windows.Storage.KnownFolders.PicturesLibrary;
                cacheFolder = await cacheFolder.GetFolderAsync("securitysystem-cameradrop");
                displayList = await cacheFolder.GetFilesAsync();
                displayListMutexLock.ReleaseMutex();
            }
            catch (Exception e)
            {
                Debug.WriteLine("Exception in getting images " + e.Message);
                displayListMutexLock.ReleaseMutex();
            }
        }
    }
}
