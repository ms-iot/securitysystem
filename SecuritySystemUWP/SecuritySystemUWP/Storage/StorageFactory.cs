using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Xaml.Controls;

namespace SecuritySystemUWP
{
    public static class StorageFactory
    {
        public static IStorage Get(string type, string accountId, string accountSecret)
        {
            switch (type.ToLower())
            {
                case "azure": return new Azure(accountId, accountSecret);
                case "onedrive": return new OneDrive(accountId, accountSecret);
                default: throw new ArgumentNullException("Set Protocol in config");
            }
        }
    }
}
