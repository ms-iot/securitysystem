using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace SecuritySystemUWP
{
    public interface IStorage
    {
        Type StorageStartPage();
        void UploadPictures(string camera);
        void DeleteExpiredPictures(string camera);
    }
}
