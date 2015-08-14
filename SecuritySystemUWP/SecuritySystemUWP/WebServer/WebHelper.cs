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
using Windows.Storage.Streams;

namespace SecuritySystemUWP
{
    public class WebHelper
    {
        private string htmlTemplate;

        public async Task InitializeAsync()
        {
            var filePath = @"Assets\Web\default.htm";
            var folder = Windows.ApplicationModel.Package.Current.InstalledLocation;
            var file = await folder.GetFileAsync(filePath);
            htmlTemplate = await FileIO.ReadTextAsync(file);
        }

        public string CreateHtmlFormFromSettings(StreamSocketInformation socketInfo)
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
                    if (info.FieldType == typeof(string))
                    {
                        html += "<input type='text' name='" + info.Name + "' value='" + info.GetValue(App.XmlSettings) + "' size='50'>";
                    }
                    else if (info.FieldType == typeof(int))
                    {
                        html += "<input type='number' name='" + info.Name + "' value='" + info.GetValue(App.XmlSettings) + "' size='50'>";
                    }
                    else if (info.FieldType == typeof(CameraType) || info.FieldType == typeof(StorageProvider))
                    {
                        html += "<select name='" + info.Name + "'>";
                        foreach(string type in Enum.GetNames(info.FieldType))
                        {
                            html += "<option value='" + type + "' " +
                                (info.GetValue(App.XmlSettings).Equals(Enum.Parse(info.FieldType, type)) ? "selected='selected'" : "")
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
            html += "<tr><td colspan='2'><input type='submit' value='Save'></td></tr>";
            html += "</table>";
            html += "</form>";
            return html;
        }

        private string createNavBar()
        {
            Dictionary<string, string> links = new Dictionary<string, string>
            {
                {"Config", "/" },
                {"OneDrive", "/OneDrive.htm" },
            };

            string html = "<p>Navigation</p><ul>";
            foreach(string key in links.Keys)
            {
                html += "<li><a href='" + links[key] + "'>" + key + "</a></li>";
            }
            html += "</ul>";
            return html;
        }

        public string GenerateOneDrivePage()
        {
            string html = "";
            html += "OneDrive Status:  " + (OneDrive.IsLoggedIn() ? "<span style='color:Green'>Logged In" : "<span style='color:Red'>Not Logged In") + "</span><br><br>";
            string uri = string.Format(AppSettings.OneDriveLoginUrl, App.XmlSettings.OneDriveClientId, AppSettings.OneDriveScope, AppSettings.OneDriveRedirectUrl);
            html += "<p class='sectionHeader'>Log into OneDrive:</p>";
            html += "<ol>";
            html += "<li>Click on this link:  <a href='" + uri + "' target='_blank'>OneDrive Login</a><br>"+
                "A new window will open.  Log into OneDrive.<br><br></li>";
            html += "<li>After you're done, you should arrive at a blank page.<br>" + 
                "Copy the URL, paste it into this box, and click Submit.<br>" +
                "The URL will look something like this: https://login.live.com/oauth20_desktop.srf?code=M6b0ce71e-8961-1395-2435-f78db54f82ae&lc=1033 <br>" +
                " <form><input type='text' name='codeUrl' size='50'>  <input type='submit' value='Submit'></form></li>";
            html += "</ol><br><br>";

            if (OneDrive.IsLoggedIn())
            {
                html += "<p class='sectionHeader'>Log out of OneDrive:</p>";
                html += "<form><button type='submit' name='logout'>Logout</button></form>";
            }

            return GeneratePage("OneDrive Config", "OneDrive Config", html);
        }
        
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

        public string GeneratePage(string title, string titleBar, string content)
        {
            return GeneratePage(title, titleBar, content, "");
        }

        public async Task ParseOneDriveUri(Uri uri)
        {
            try
            {
                var decoder = new WwwFormUrlDecoder(uri.Query);
                foreach (WwwFormUrlDecoderEntry entry in decoder)
                {
                    if (entry.Name.Equals("codeUrl"))
                    {
                        string codeUrl = WebUtility.UrlDecode(entry.Value);
                        var codeUri = new Uri(codeUrl);
                        var codeDecoder = new WwwFormUrlDecoder(codeUri.Query);
                        foreach (WwwFormUrlDecoderEntry subEntry in codeDecoder)
                        {
                            if (subEntry.Name.Equals("code"))
                            {
                                await OneDrive.Authorize(subEntry.Value);
                                break;
                            }
                        }
                        break;
                    }
                    else if (entry.Name.Equals("logout"))
                    {
                        await OneDrive.Logout();
                    }
                }
            }
            catch (Exception)
            { }
        }

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
                        field.SetValue(App.XmlSettings, Convert.ToInt32(entry.Value));
                    }
                    else if(field.FieldType == typeof(CameraType) ||
                        field.FieldType == typeof(StorageProvider))
                    {
                        field.SetValue(App.XmlSettings, Enum.Parse(field.FieldType, entry.Value));
                    }
                    else
                    {
                        field.SetValue(App.XmlSettings, entry.Value);
                    }
                }
                catch (Exception e)
                {
                    Debug.WriteLine(e.Message);
                }
            }
        }

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
    }
}
