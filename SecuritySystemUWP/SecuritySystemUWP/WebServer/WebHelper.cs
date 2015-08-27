using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Networking.Sockets;
using Windows.Storage;
using Windows.Storage.Search;
using Windows.Storage.Streams;

namespace SecuritySystemUWP
{
    public class WebHelper
    {
        private string picturesLibPath = "C:\\Users\\DefaultAccount\\Pictures";
        private string htmlTemplate;
        private Dictionary<string, string> links = new Dictionary<string, string>
            {
                {"Home", "/" + NavConstants.HOME_PAGE },
                {"Settings", "/" + NavConstants.SETTINGS_PAGE },
                {"Gallery", "/" + NavConstants.GALLERY_PAGE },
                {"OneDrive", "/" + NavConstants.ONEDRIVE_PAGE },
            };
        private StorageFile[] cachedFiles = null;

        /// <summary>
        /// Initializes the WebHelper with the default.htm template
        /// </summary>
        /// <returns></returns>
        public async Task InitializeAsync()
        {
            // Load the html page template
            var filePath = @"Assets\Web\default.htm";
            var folder = Windows.ApplicationModel.Package.Current.InstalledLocation;
            var file = await folder.GetFileAsync(filePath);
            htmlTemplate = await FileIO.ReadTextAsync(file);
        }
        
        /// <summary>
        /// Creates an html form for all of the editable fields in AppSettings
        /// </summary>
        /// <returns></returns>
        public string CreateHtmlFormFromSettings()
        {
            string html = "<form>";

            html += "<table>";
            foreach (FieldInfo info in typeof(AppSettings).GetFields())
            {
                // Don't allow constant/static fields or fields with no descriptions to be edited
                if (!info.IsStatic && info.GetCustomAttributes(typeof(DescriptionAttribute)).Count() > 0)
                {
                    html += "<tr>";
                    html += "<td>";
                    html += "<b>" + info.Name + "</b>    ";
                    html += "</td><td>";

                    // Create textbox for strings
                    if (info.FieldType == typeof(string))
                    {
                        html += "<input type='text' name='" + info.Name + "' value='" + info.GetValue(App.Controller.XmlSettings) + "' size='50'>";
                    }
                    // Create textbox for numbers
                    else if (info.FieldType == typeof(int))
                    {
                        html += "<input type='number' name='" + info.Name + "' value='" + info.GetValue(App.Controller.XmlSettings) + "' size='50'>";
                    }
                    // Create dropdown for enums
                    else if (info.FieldType == typeof(CameraType) || info.FieldType == typeof(StorageProvider))
                    {
                        html += "<select name='" + info.Name + "'>";
                        foreach (string type in Enum.GetNames(info.FieldType))
                        {
                            html += "<option value='" + type + "' " +
                                (info.GetValue(App.Controller.XmlSettings).Equals(Enum.Parse(info.FieldType, type)) ? "selected='selected'" : "")
                                + ">" + type + "</option>";
                        }
                        html += "</select>";
                    }

                    html += "</td></tr>";
                    html += "<tr><td colspan='2' class='subText'>";
                    foreach (DescriptionAttribute attr in info.GetCustomAttributes(typeof(DescriptionAttribute)))
                    {
                        html += attr.Description + "<br>";
                    }
                    html += "</td></tr><tr><td colspan='2'>&nbsp;</td></tr>";
                }
            }
            html += "<tr><td colspan='2'><input type='submit' value='Save & Apply'></td></tr>";
            html += "</table>";
            html += "</form>";
            return html;
        }

        /// <summary>
        /// Generates the html for the navigation bar
        /// </summary>
        /// <returns></returns>
        private string createNavBar()
        {
            // Create html for the side bar navigation using the links Dictionary
            string html = "<p>Navigation</p><ul>";
            foreach (string key in links.Keys)
            {
                if (key.Equals("OneDrive") && App.Controller.Storage.GetType() != typeof(OneDrive))
                    continue;

                html += "<li><a href='" + links[key] + "'>" + key + "</a></li>";
            }
            html += "</ul>";
            return html;
        }

        /// <summary>
        /// Generates the html for the OneDrive login page
        /// </summary>
        /// <returns></returns>
        public string GenerateOneDrivePage()
        {
            bool isOneDriveLoggedIn = false;
            var oneDrive = App.Controller.Storage as OneDrive;
            if(oneDrive != null)
            {
                isOneDriveLoggedIn = oneDrive.IsLoggedIn();
            }

            string html = "";

            // Display login status
            html += "<b>OneDrive Status:&nbsp;&nbsp;</b>" + (isOneDriveLoggedIn ? "<span style='color:Green'>Logged In" : "<span style='color:Red'>Not Logged In") + "</span><br>";

            // Create OneDrive URL for logging in
            string uri = string.Format(AppSettings.OneDriveLoginUrl, App.Controller.XmlSettings.OneDriveClientId, AppSettings.OneDriveScope, AppSettings.OneDriveRedirectUrl);
            html += "<p class='sectionHeader'>Log into OneDrive:</p>";
            html += "<ol>";
            html += "<li>Click on this link:  <a href='" + uri + "' target='_blank'>OneDrive Login</a><br>" +
                "A new window will open.  Log into OneDrive.<br><br></li>";
            html += "<li>After you're done, you should arrive at a blank page.<br>" +
                "Copy the URL, paste it into this box, and click Submit.<br>" +
                "The URL will look something like this: https://login.live.com/oauth20_desktop.srf?code=M6b0ce71e-8961-1395-2435-f78db54f82ae&lc=1033 <br>" +
                " <form><input type='text' name='codeUrl' size='50'>  <input type='submit' value='Submit'></form></li>";
            html += "</ol><br><br>";

            // Create a logout button if we're logged in
            if (isOneDriveLoggedIn)
            {
                html += "<p class='sectionHeader'>Log out of OneDrive:</p>";
                html += "<form><button type='submit' name='logout'>Logout</button></form>";
            }

            return GeneratePage("OneDrive Config", "OneDrive Config", html);
        }

        /// <summary>
        /// Generates the html for the home page (status page)
        /// </summary>
        /// <returns></returns>
        public string GenerateStatusPage()
        {
            string html = "";

            // Show camera type on status page
            html += "<b>Camera Type:&nbsp;&nbsp;</b>" + App.Controller.Camera.GetType().Name + "<br>";

            // Show storage type on status page
            html += "<b>Storage Type:&nbsp;&nbsp;</b>" + App.Controller.Storage.GetType().Name + "<br><br>";

            // Show controller status
            html += "<b>Status:&nbsp;&nbsp;</b>" + ((App.Controller.IsInitialized) ? "<span style='color:Green'>Running" : "<span style='color:Red'>Not Running") + "</span><br>";

            // Show OneDrive status if the Storage Provider selected is OneDrive
            if(App.Controller.Storage.GetType() == typeof(OneDrive))
            {
                var oneDrive = App.Controller.Storage as OneDrive;
                html += "<b>OneDrive Status:&nbsp;&nbsp;</b>" + (oneDrive.IsLoggedIn() ? "<span style='color:Green'>Logged In" : "<span style='color:Red'>Not Logged In") + "</span><br>";
            }

            return GeneratePage("Security System", "Home", html);
        }

        /// <summary>
        /// Generates html for the gallery
        /// </summary>
        /// <param name="folder">Folder to read the pictures from</param>
        /// <param name="pageNumber">Page starting from 1</param>
        /// <param name="pageSize">Number of pictures on each page</param>
        /// <returns></returns>
        public async Task<string> GenerateGallery(StorageFolder folder, int pageNumber, int pageSize)
        {
            // Don't allow negatives
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1) pageSize = 1;

            var subFolders = await folder.GetFoldersAsync();
            var parentFolder = await folder.GetParentAsync();

            // JavaScript code for toggling the subfolder list
            string html = "";
            html += "<script type='text/javascript'>" +
                "function toggleSubfolderList(){"+
                "var folderNavPane = document.getElementById('folder_nav_pane');" +
                "if(folderNavPane.style.display == 'block') folderNavPane.style.display = 'none';" +
                "else folderNavPane.style.display = 'block'" +
                "}" +
                "</script>";

            html += "<table>";
            html += "<tr><td>";
            // Create breadcrumbs for folder nav
            var temp = folder;
            string breadcrumbs = "<b>"+ ((subFolders.Count > 0) ? "<a onclick='toggleSubfolderList()' href='javascript:void(0);'>" + temp.Name + "</a>" : temp.Name) + "</b>";
            while(!temp.Path.Equals(picturesLibPath, StringComparison.OrdinalIgnoreCase))
            {
                temp = await temp.GetParentAsync();
                string hyperlink = MakeHyperlink(temp.Name, "/gallery.htm?folder=" + WebUtility.UrlEncode(temp.Path), false);
                breadcrumbs = ((!isPicturesFolder(temp)) ? hyperlink : temp.Name) + " > " + breadcrumbs;
            }
            html += breadcrumbs + "<br>";

            if (subFolders.Count > 0)
            {
                // Generate subfolder navigation pane
                html += "<div id='folder_nav_pane' style='display:none'>";
                html += "<ul>";
                foreach (StorageFolder subFolder in subFolders)
                {
                    html += "<li><a href='/gallery.htm?folder=" + WebUtility.UrlEncode(subFolder.Path) + "'>" + subFolder.Name + "</a></li>";
                }
                html += "</ul></div>";
            }

            html += "<br></td></tr>";

            // Get the files in current folder and subfolders
            var queryOptions = new QueryOptions();
            queryOptions.FolderDepth = FolderDepth.Deep;

            var results = folder.CreateFileQueryWithOptions(queryOptions);
            
            StorageFile[] sortedFiles = null;

            // Use cached files if we already got the files and we're navigating to the first page
            if (cachedFiles != null && pageNumber != 1)
            {
                sortedFiles = cachedFiles;
            }
            else
            {
                var files = await results.GetFilesAsync();
                sortedFiles = files.OrderByDescending((x) => x.DateCreated).ToArray();
                cachedFiles = sortedFiles;
            }

            if (sortedFiles.Length > 0)
            {
                // Create pages
                string pagesHtml = "<form>";
                html += "<tr><td>";
                int totalPages = (int)Math.Ceiling((double)sortedFiles.Length / pageSize);
                pagesHtml += "Pages: ";

                pagesHtml += "<select name='page' onchange='this.form.submit()'>";

                for (int i = 1; i <= totalPages; i++)
                {
                    pagesHtml += "<option value='" + i + "' " + ((i == pageNumber) ? "selected='selected'" : "") + ">" + i + "</option>";
                }
                pagesHtml += "</select>";
                pagesHtml += "<input type='hidden' name='folder' value='" + folder.Path + "' />";
                pagesHtml += "<input type='hidden' name='pageSize' value='30' />";
                pagesHtml += "</form>";

                html += pagesHtml;
                html += "<br></td></tr>";

                html += "<tr><td>";

                // Pick out the subset of files we need based on page
                int startIndex = (pageNumber - 1) * pageSize;
                for (int i = startIndex; i < startIndex + pageSize; i++)
                {
                    if (i > 0 && i < sortedFiles.Length)
                    {
                        StorageFile file = sortedFiles[i];
                        html += "<div class='img'>";
                        html += "<a target='_blank' href='/api/gallery/" + WebUtility.UrlEncode(file.Path) + "'>";
                        html += "<img src='/api/gallery/" + WebUtility.UrlEncode(file.Path) + "' alt='" + file.Name + "' width='190'>";
                        html += "<div class='desc'><b>File Name:</b> " + file.Name + "<br><b>Date Created:</b> " + file.DateCreated + "</div>";
                        html += "</a>";
                        html += "</div>";
                    }
                }

                html += "</td></tr>";

                // Create pages
                html += "<tr><td>";
                html += "<br>" + pagesHtml;
                html += "</td></tr>";
            }
            else
            {
                html += "No pictures found in " + folder.Path;
            }

            html += "</table>";

            return html;
        }

        /// <summary>
        /// Helper function to generate page
        /// </summary>
        /// <param name="title">Title that appears on the window</param>
        /// <param name="titleBar">Title that appears on the header bar of the page</param>
        /// <param name="content">Content for the body of the page</param>
        /// <param name="message">A status message that will appear above the content</param>
        /// <returns></returns>
        public string GeneratePage(string title, string titleBar, string content, string message)
        {
            string html = htmlTemplate;
            html = html.Replace("#content#", content);
            html = html.Replace("#title#", title);
            html = html.Replace("#titleBar#", titleBar);
            html = html.Replace("#navBar#", createNavBar());
            html = html.Replace("#message#", message);

            return html;
        }

        /// <summary>
        /// Helper function to generate page
        /// </summary>
        /// <param name="title">Title that appears on the window</param>
        /// <param name="titleBar">Title that appears on the header bar of the page</param>
        /// <param name="content">Content for the body of the page</param>
        /// <returns></returns>
        public string GeneratePage(string title, string titleBar, string content)
        {
            return GeneratePage(title, titleBar, content, "");
        }

        /// <summary>
        /// Parses the GET parameters from the URL and then uses them to log into OneDrive
        /// </summary>
        /// <param name="uri"></param>
        /// <returns></returns>
        public async Task ParseOneDriveUri(Uri uri)
        {
            var oneDrive = App.Controller.Storage as OneDrive;
            if (oneDrive == null)
                return;

            try
            {
                var decoder = new WwwFormUrlDecoder(uri.Query);
                foreach (WwwFormUrlDecoderEntry entry in decoder)
                {
                    // codeUrl is the parameter that contains the URL that was pasted into the textbox on the OneDrive page
                    if (entry.Name.Equals("codeUrl"))
                    {
                        string codeUrl = WebUtility.UrlDecode(entry.Value);
                        var codeUri = new Uri(codeUrl);
                        var codeDecoder = new WwwFormUrlDecoder(codeUri.Query);
                        foreach (WwwFormUrlDecoderEntry subEntry in codeDecoder)
                        {
                            if (subEntry.Name.Equals("code"))
                            {
                                await oneDrive.Authorize(subEntry.Value);
                                break;
                            }
                        }
                        break;
                    }
                    else if (entry.Name.Equals("logout"))
                    {
                        await oneDrive.Logout();
                    }
                }
            }
            catch (Exception ex)
            {
                // Log telemetry event about this exception
                var events = new Dictionary<string, string> { { "WebHelper", ex.Message } };
                App.Controller.TelemetryClient.TrackEvent("FailedToParseOneDriveUri", events);
            }

        }

        /// <summary>
        /// Parses the GET parameters from the URL and returns the parameters and values in a Dictionary
        /// </summary>
        /// <param name="uri"></param>
        /// <returns></returns>
        public Dictionary<string, string> ParseGetParametersFromUrl(Uri uri)
        {
            Dictionary<string, string> parameters = new Dictionary<string, string>();

            var decoder = new WwwFormUrlDecoder(uri.Query);
            foreach (WwwFormUrlDecoderEntry entry in decoder)
            {
                parameters.Add(entry.Name, entry.Value);
            }

            return parameters;
        }

        /// <summary>
        /// Parses the GET parameters from the URL and loads them into the settings
        /// </summary>
        /// <param name="uri"></param>
        public void ParseUriIntoSettings(Uri uri)
        {
            var decoder = new WwwFormUrlDecoder(uri.Query);

            // Take the parameters from the URL and put it into Settings
            foreach (WwwFormUrlDecoderEntry entry in decoder)
            {
                try
                {
                    var field = typeof(AppSettings).GetField(entry.Name);
                    if (field.FieldType == typeof(int))
                    {
                        field.SetValue(App.Controller.XmlSettings, Convert.ToInt32(entry.Value));
                    }
                    else if(field.FieldType == typeof(CameraType) ||
                            field.FieldType == typeof(StorageProvider))
                    {
                        field.SetValue(App.Controller.XmlSettings, Enum.Parse(field.FieldType, entry.Value));
                    }
                    else
                    {
                        //if the field being saved is the alias, and the alias has changed, send a telemetry event
                        if(0 == field.Name.CompareTo("MicrosoftAlias") &&
                           0 != entry.Value.CompareTo(App.Controller.XmlSettings.MicrosoftAlias))
                        {
                            Dictionary<string, string> properties = new Dictionary<string, string> { { "Alias", entry.Value } };
                            App.Controller.TelemetryClient.TrackEvent("Alias Changed", properties);
                        }
                        field.SetValue(App.Controller.XmlSettings, entry.Value);
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message);

                    // Log telemetry event about this exception
                    var events = new Dictionary<string, string> { { "WebHelper", ex.Message } };
                    App.Controller.TelemetryClient.TrackEvent("FailedToParseUriIntoSettings", events);
                }
            }
        }

        /// <summary>
        /// Writes html data to the stream
        /// </summary>
        /// <param name="data"></param>
        /// <param name="os"></param>
        /// <returns></returns>
        public static async Task WriteToStream(string data, IOutputStream os)
        {
            using (Stream resp = os.AsStreamForWrite())
            {
                // Look in the Data subdirectory of the app package
                byte[] bodyArray = Encoding.UTF8.GetBytes(data);
                MemoryStream stream = new MemoryStream(bodyArray);
                string header = String.Format("HTTP/1.1 200 OK\r\n" +
                                  "Content-Length: {0}\r\n" +
                                  "Connection: close\r\n\r\n",
                                  stream.Length);
                byte[] headerArray = Encoding.UTF8.GetBytes(header);
                await resp.WriteAsync(headerArray, 0, headerArray.Length);
                await stream.CopyToAsync(resp);
                await resp.FlushAsync();
            }
        }

        /// <summary>
        /// Writes a file to the stream
        /// </summary>
        /// <param name="file"></param>
        /// <param name="os"></param>
        /// <returns></returns>
        public static async Task WriteFileToStream(StorageFile file, IOutputStream os)
        {
            using (Stream resp = os.AsStreamForWrite())
            {
                bool exists = true;
                try
                {
                    using (Stream fs = await file.OpenStreamForReadAsync())
                    {
                        string header = String.Format("HTTP/1.1 200 OK\r\n" +
                                        "Content-Length: {0}\r\n" +
                                        "Connection: close\r\n\r\n",
                                        fs.Length);
                        byte[] headerArray = Encoding.UTF8.GetBytes(header);
                        await resp.WriteAsync(headerArray, 0, headerArray.Length);
                        await fs.CopyToAsync(resp);
                    }
                }
                catch (FileNotFoundException ex)
                {
                    exists = false;

                    // Log telemetry event about this exception
                    var events = new Dictionary<string, string> { { "WebHelper", ex.Message } };
                    App.Controller.TelemetryClient.TrackEvent("FailedToWriteFileToStream", events);
                }

                if (!exists)
                {
                    byte[] headerArray = Encoding.UTF8.GetBytes(
                                          "HTTP/1.1 404 Not Found\r\n" +
                                          "Content-Length:0\r\n" +
                                          "Connection: close\r\n\r\n");
                    await resp.WriteAsync(headerArray, 0, headerArray.Length);
                }

                await resp.FlushAsync();
            }
        }

        /// <summary>
        /// Makes a html hyperlink
        /// </summary>
        /// <param name="text">Hyperlink text</param>
        /// <param name="url">Hyperlink URL</param>
        /// <param name="newWindow">Should the link open in a new window</param>
        /// <returns></returns>
        public static string MakeHyperlink(string text, string url, bool newWindow)
        {
            return "<a href='" + url + "' " + ((newWindow) ? "target='_blank'" : "") + ">" + text + "</a>";
        }

        /// <summary>
        /// Checks if the folder is the Pictures library folder
        /// </summary>
        /// <param name="folder"></param>
        /// <returns></returns>
        private bool isPicturesFolder(StorageFolder folder)
        {
            return folder.Path.Equals(picturesLibPath, StringComparison.OrdinalIgnoreCase);
        }
    }
}
