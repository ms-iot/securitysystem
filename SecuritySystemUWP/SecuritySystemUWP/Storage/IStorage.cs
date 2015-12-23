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
        void UploadPictures();
        DateTime LastUploadTime { get; }
        void DeleteExpiredPictures();
        void Dispose();
    }
}
