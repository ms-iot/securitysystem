using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;


namespace SecuritySystemUWP
{
    public static class Config
    {
        //Do not change these values
        public static string AzureConnectionSettings = "DefaultEndpointsProtocol=https;AccountName={0};AccountKey={1}";

        public static string OneDriveRedirectUrl = "https://login.live.com/oauth20_desktop.srf";
        public static string OneDriveLoginUrl = "https://login.live.com/oauth20_authorize.srf?client_id={0}&scope={1}&response_type=code&redirect_uri={2}";
        public static string OneDriveLogoutUrl = "https://login.live.com/oauth20_logout.srf?client_id={0}&redirect_uri={1}";
        public static string OneDriveScope = "wl.offline_access onedrive.readwrite";
        public static string OneDriveRootUrl = "https://api.onedrive.com/v1.0/drive/root:";
        public static string OneDriveTokenUrl = "https://login.live.com/oauth20_token.srf";
        public static string OneDriveTokenContent = "client_id={0}&redirect_uri={1}&client_secret={2}&{3}={4}&grant_type={5}";

        public static string ImageNameFormat = "{0}/{1}_{2}.jpg";
    }
}
