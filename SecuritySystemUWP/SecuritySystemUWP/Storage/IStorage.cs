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
        Type loginType();
        Task<bool> uploadPicture(string folderPath, string imageName, StorageFile imageFile);
        Task<List<string>> listPictures(string folderPath);
        Task<bool> deletePicture(string folderPath, string imageName);
    }
}
