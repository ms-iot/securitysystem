using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights.DataContracts;
using Windows.Storage;
using Windows.Storage.Search;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;

namespace SecuritySystemUWP
{
    public sealed partial class Gallery : Page
    {
        // List of images loaded
        List<Image> imageList = new List<Image>();

        // Map the image to the actual file so that we can get the file information later
        Dictionary<Image, StorageFile> imageDict = new Dictionary<Image, StorageFile>();

        // Index of the image that has currently been selected
        int currentIndex = 0;
        uint currentPage = 0;
        uint gallerySize = 20;
        uint totalPictures = 0;
        long totalPages = 1;

        StorageFolder imageFolder;

        // Keeps track of which subfolder level we're on relative to the root folder
        int folderLevel = 0;

        public Boolean buttonEnabled { get; private set; }

        public Gallery()
        {
            this.InitializeComponent();
            buttonEnabled = true;
            fullImage.PointerReleased += FullImage_PointerReleased;

            previousButton.Click += PreviousButton_Click;
            nextButton.Click += NextButton_Click;

            appBar.Visibility = Visibility.Visible;
            fullGrid.Visibility = Visibility.Collapsed;
            galleryGrid.Visibility = Visibility.Visible;

            // Event handler to update app bar buttons depending on view we're in
            appBar.Opening += AppBar_Opening;

            // Mouseover event handlers for app bar
            appBar.PointerEntered += AppBar_PointerEntered;
            appBar.PointerExited += AppBar_PointerExited;

            // Folder list event handler
            listView.ItemClick += ListView_ItemClick;
        }

        private async void ListView_ItemClick(object sender, ItemClickEventArgs e)
        {
            try
            {
                string clicked = e.ClickedItem as string;
                if (clicked != null)
                {
                    StorageFolder folder;
                    // Navigate to parent folder
                    if (clicked == "..")
                    {
                        folder = await imageFolder.GetParentAsync();
                        if (folder == null)
                        {
                            return;
                        }
                        folderLevel--;
                    }
                    // Navigate to new folder
                    else
                    {
                        folder = await imageFolder.GetFolderAsync(clicked);
                        if (folder == null)
                        {
                            return;
                        }
                        folderLevel++;
                    }

                    imageFolder = folder;

                    // Display the pictures in the new folder
                    buttonEnabled = false;
                    displayPictures(imageFolder);
                    buttonEnabled = true;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }


        private void AppBar_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            appBar.IsOpen = false;
        }

        private void AppBar_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            appBar.IsOpen = true;
        }

        private void AppBar_Opening(object sender, object e)
        {
            galleryBarPanel.Visibility = fullScreenBarPanel.Visibility = Visibility.Collapsed;

            // If the full picture is visible, app bar should have a return to gallery button
            if (fullGrid.Visibility == Visibility.Visible)
            {
                fullScreenBarPanel.Visibility = Visibility.Visible;
            }
            // If the gallery is visible, app bar should have previous and next page buttons
            else if (galleryGrid.Visibility == Visibility.Visible)
            {
                galleryBarPanel.Visibility = Visibility.Visible;
            }
        }

        // When in fullscreen view, go to the next picture in the list
        private void NextButton_Click(object sender, RoutedEventArgs e)
        {
            currentIndex++;
            if (currentIndex >= imageList.Count)
            {
                // If we're at the end, go the the first image in the list
                currentIndex = 0;
            }

            displayFullImage(imageList[currentIndex]);
        }

        // When in fullscreen view, go to the previous picture in the list
        private void PreviousButton_Click(object sender, RoutedEventArgs e)
        {
            currentIndex--;
            if (currentIndex < 0)
            {
                // If we're at the beginning, go to the last image in the list
                currentIndex = imageList.Count - 1;
            }

            displayFullImage(imageList[currentIndex]);
        }

        // Display the image that was clicked on
        private void displayFullImage(Image image)
        {
            fullImage.Source = image.Source;
            dateTextBlock.Text = imageDict[image].Name + ", " + imageDict[image].DateCreated.ToString();
            appBar.Visibility = Visibility.Visible;
        }

        // Toggle app bar when full image is clicked
        private void FullImage_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            appBar.IsOpen = !appBar.IsOpen;
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            imageFolder = KnownFolders.PicturesLibrary;
            imageFolder = await imageFolder.GetFolderAsync("securitysystem-cameradrop");
            buttonEnabled = false;
            displayPictures(imageFolder);
            buttonEnabled = true;
        }

        // Displays the pictures of the folder in the gallery
        private async void displayPictures(StorageFolder folder)
        {
            if (folder == null)
            {
                return;
            }

            // Clear everything so user knows that something happened
            stackPanel.Children.Clear();
            listView.Items.Clear();
            loadingText.Visibility = Visibility.Visible;

            imageFolder = folder;

            // Create query to get number of pictures in current folder and subfolders
            var queryOptions = new QueryOptions();
            queryOptions.FolderDepth = FolderDepth.Deep;
            var query = imageFolder.CreateFileQueryWithOptions(queryOptions);

            totalPictures = await query.GetItemCountAsync();

            // Calculate total pages for gallery
            totalPages = Math.Max(1, (int)Math.Ceiling((float)totalPictures / gallerySize));

            // Set current page to 0
            currentPage = 0;

            // Generate the gallery thumbnails
            generateGallery(currentPage);

            // Display the subfolders in the list
            displaySubFolders(imageFolder);
        }

        // Display the sub folders of current folder in the list
        private async void displaySubFolders(StorageFolder folder)
        {
            if (folder != null)
            {
                // Display current folder
                folderPathText.Text = folder.Name;

                var folders = await folder.GetFoldersAsync();
                listView.Items.Clear();

                // Create an entry to go up a level from current folder if we're not at the root path
                if (folderLevel > 0)
                {
                    listView.Items.Add("..");
                }

                // Enumerate the subfolders
                foreach (StorageFolder f in folders)
                {
                    listView.Items.Add(f.Name);
                }
            }
        }

        private async Task<List<StorageFile>> getFiles(uint page)
        {
            // By default the query returns everything from oldest to newest, so we have to calculate which group of pictures,
            // starting from the end, to query, so that when we sort it by newest to oldest, everything makes sense

            int index = 0;
            if (totalPictures > gallerySize)
            {
                index = (int)(page * gallerySize);
            }

            // Get the files in current folder and subfolders
            var queryOptions = new QueryOptions();
            queryOptions.FolderDepth = FolderDepth.Deep;

            var results = imageFolder.CreateFileQueryWithOptions(queryOptions);

            // Only get the number of files that we need, since getting the entire folder would be slow
            var files = await results.GetFilesAsync((uint)index, gallerySize);

            // Sort the list files by date
            IEnumerable<StorageFile> sortedFiles = files.OrderByDescending((x) => x.DateCreated);
            files = sortedFiles.ToList();

            return files.ToList();
        }

        private async void generateGallery(uint page)
        {
            loadingText.Visibility = Visibility.Visible;
            stackPanel.Children.Clear();

            try
            {
                GridView gridView = new GridView();

                // Get files in folder
                var files = await getFiles(page);

                // If there are no files, display a message 
                if (files.Count == 0)
                {
                    stackPanel.Children.Add(new TextBlock { Text = "No pictures found." });
                    titleTextBlock.Text = "Gallery";
                }
                else
                {
                    foreach (StorageFile file in files)
                    {
                        // Only pick out image files
                        if (file.FileType.Contains("jpg"))
                        {
                            var canvas = new Canvas();
                            canvas.Width = (scrollViewer.ActualWidth - 50) / 5;
                            canvas.Height = (scrollViewer.ActualHeight - 50) / 4;

                            // Create image thumbnail to display in gallery
                            var image = await CreateImage((scrollViewer.ActualWidth - 50) / 5, (scrollViewer.ActualHeight - 50) / 4, file);
                            imageList.Add(image);
                            imageDict.Add(image, file);

                            Canvas.SetLeft(image, 0);
                            Canvas.SetTop(image, 0);
                            canvas.Children.Add(image);                         

                            // Add image to the GridView
                            gridView.Items.Add(canvas);
                        }
                    }

                    stackPanel.Children.Add(gridView);
                    titleTextBlock.Text = "Gallery (" + (currentPage + 1) + "/" + totalPages + ")";
                }
            }
            catch (Exception err)
            {
                // Log telemetry event about this exception
                App.TelemetryClient.TrackException(err);

                // Print exception if there is one
                stackPanel.Children.Clear();
                stackPanel.Children.Add(new TextBlock { Text = "There was a problem loading the gallery. (" + err.Message + ")" });
            }

            loadingText.Visibility = Visibility.Collapsed;
        }

        public async Task<Image> CreateImage(double width, double height, StorageFile file)
        {
            // If we're reading from LocalState folder, render images this way
            if (imageFolder.Equals(ApplicationData.Current.LocalFolder))
            {
                return CreateImage(width, height, file.Path);
            }
            // Otherwise render them using streams
            else
            {
                Image image = new Image();
                image.Width = width;
                image.Height = height;
                image.Stretch = Stretch.UniformToFill;
                image.Name = file.Path;
                image.PointerReleased += Image_PointerReleased;

                using (Windows.Storage.Streams.IRandomAccessStream fileStream = await file.OpenAsync(Windows.Storage.FileAccessMode.Read))
                {
                    // Set the image source to the selected bitmap.
                    Windows.UI.Xaml.Media.Imaging.BitmapImage bitmapImage = new Windows.UI.Xaml.Media.Imaging.BitmapImage();

                    bitmapImage.SetSource(fileStream);
                    image.Source = bitmapImage;
                }

                return image;
            }
        }
        public Image CreateImage(double width, double height, string filePath)
        {
            Image image = new Image();
            image.Width = width;
            image.Height = height;
            image.Source = new BitmapImage(new Uri(filePath, UriKind.Absolute));
            image.Stretch = Stretch.UniformToFill;
            image.Name = filePath;
            image.PointerReleased += Image_PointerReleased;
            return image;
        }

        // When the image is clicked in the gallery, open up a fullscreen view of the image
        private void Image_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            Image image = sender as Image;
            if (image != null)
            {
                currentIndex = imageList.IndexOf(image);
                displayFullImage(imageList[currentIndex]);
                galleryGrid.Visibility = Visibility.Collapsed;
                fullGrid.Visibility = Visibility.Visible;
            }
        }

        // Go back to main page when home button is clicked
        private void appBarButton_Click(object sender, RoutedEventArgs e)
        {
            this.Frame.Navigate(typeof(MainPage));
        }

        // Go back to gallery from full screen image mode
        private void appBarBackButton_Click(object sender, RoutedEventArgs e)
        {
            fullGrid.Visibility = Visibility.Collapsed;
            galleryGrid.Visibility = Visibility.Visible;
            appBar.IsOpen = false;
        }

        // Go to previous page in gallery
        private void previousPageButton_Click(object sender, RoutedEventArgs e)
        {
            currentPage = (currentPage + (uint)totalPages - 1) % (uint)totalPages;
            buttonEnabled = false;
            generateGallery(currentPage);
            buttonEnabled = true;
        }

        // Go to next page in gallery
        private void nextPageButton_Click(object sender, RoutedEventArgs e)
        {
            currentPage = (currentPage + (uint)totalPages + 1) % (uint)totalPages;
            buttonEnabled = false;
            generateGallery(currentPage);
            buttonEnabled = true;
        }

        private void refreshButton_Click(object sender, RoutedEventArgs e)
        {
            // Refresh the images in the gallery
            buttonEnabled = false;
            displayPictures(imageFolder);
            buttonEnabled = true;
        }

        private void homeButton_Click(object sender, RoutedEventArgs e)
        {
            // Set up some properties:
            var properties = new Dictionary<string, string>
            { {"signalSource", "Gallery Page"}};


            // Navigate back to main page
            this.Frame.Navigate(typeof(MainPage));
        }

        private void titleTextBlock_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            // Open app bar if the title is clicked
            appBar.IsOpen = !appBar.IsOpen;
        }
    }
}
