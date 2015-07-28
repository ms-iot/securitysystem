using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SecuritySystemUWP
{
    public static class Config
    {
        //TODO: Add your storgae configuration information here

        //This will be "Azure" or "OneDrive" based on the storage you are using for this project
        public static string StorageProvider = "";

        //If you are using Azure, update these values with your Azure Account Name and your Primary Access Key
        public static string AzureAccountName = ""; 
        public static string AzureAccessKey = "";

        //If you are using OneDrive, update these values with your Client ID and your Client Secret
        public static string OneDriveClientId = "";
        public static string OneDriveClientSecret = "";

        //This value is the number of days for which your pictures will be stored
        public static int StorageDuration = 7;


        //Do not change these values
        public static string AzureBlobUrl = "http://{0}.blob.core.windows.net/";

        public static string OneDriveRedirectUrl = "https://login.live.com/oauth20_desktop.srf";
        public static string OneDriveLoginUrl = "https://login.live.com/oauth20_authorize.srf?client_id={0}&scope={1}&response_type=code&redirect_uri={2}";
        public static string OneDriveLogoutUrl = "https://login.live.com/oauth20_logout.srf?client_id={0}&redirect_uri={1}";
        public static string OneDriveScope = "wl.offline_access onedrive.readwrite";
        public static string OneDriveRootUrl = "https://api.onedrive.com/v1.0/drive/root:";
        public static string OneDriveTokenUrl = "https://login.live.com/oauth20_token.srf";
        public static string OneDriveTokenContent = "client_id={0}&redirect_uri={1}&client_secret={2}&{3}={4}&grant_type={5}";

        public static int NumberOfCameras = 1;
        public static string FolderName = "imagecontainer";
        public static string ImageNameFormat = "{0}/{1}_{2}.jpg";
    }
}
