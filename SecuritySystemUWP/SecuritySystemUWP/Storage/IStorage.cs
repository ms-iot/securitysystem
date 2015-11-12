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
        void UploadPictures(string camera);
        DateTime LastUploadTime { get; }
        void DeleteExpiredPictures(string camera);
        void Dispose();
    }
}
