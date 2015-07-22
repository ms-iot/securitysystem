using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SecuritySystemUWP
{
    public static class Config
    {
        public static string StorageProvider = "Azure"; //This will be "Azure" or "OneDrive" based on the storage you are using for this project

        //If you are using Azure, update these values with your Azure Account Name and your Primary Access Key
        public static string AzureAccountName = "securitysystemstorage"; 
        public static string AzureAccessKey = "4M0uJQHmFIqSLPca7vO1aCjX6UGoCbgTbyKhEB88peLhqfceUVD6MgA8KqXd9euhnFmmpnbUbT3C+2LgByzsIA==";

        //If you are using OneDrive, update these values with your Client ID and your Client Secret
        public static string OneDriveClientId = "000000004015C31B";
        public static string OneDriveClientSecret = "V7JaAb-9Kxd8vW3NQSzmE2ZM2aXWOlCk";

        //This value is the number of days for which your pictures will be stored
        public static int StorageDuration = 7;

        public static int NumberOfCameras = 1;
        public static string FolderName = "imagecontainer";
    }
}
