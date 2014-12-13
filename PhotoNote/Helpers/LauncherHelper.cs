using PhoneKit.Framework.Core.Storage;
using PhotoNote.Model;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.System;

namespace PhotoNote.Helpers
{
    public static class LauncherHelper
    {
        /// <summary>
        /// Launches picture in Photo hub.
        /// </summary>
        /// <param name="fileName">The file name of the image.</param>
        /// <returns>True for success, else false.</returns>
        //public static async Task<bool> LaunchPhotoAsync(string fileName)
        //{
        //    // search index;
        //    EditPicture pic = GetImageFromFileName(fileName);

        //    var tmpName = string.Format("tmp.{0}", pic.FileType.ToLower());

        //    if (pic == null)
        //        return false;

        //    var res = StorageHelper.SaveFileFromStream(tmpName, pic.FullImageStream);

        //    // return when image not found in library.
        //    if (res)
        //    {
        //        var file = await Windows.Storage.StorageFile.GetFileFromApplicationUriAsync(new Uri(StorageHelper.APPDATA_LOCAL_SCHEME + "/" + tmpName));
        //        return await Launcher.LaunchFileAsync(file);
        //    }

        //    return false;
        //}

        /// <summary>
        /// Launches Photo-Info.
        /// </summary>
        /// <param name="fileName">The file name of the image.</param>
        /// <returns>True for success, else false.</returns>
        public static async Task<bool> LaunchPhotoInfoAsync(string fileName)
        {
            // search index;
            int index = StaticMediaLibrary.GetMediaLibIndexFromFileName(fileName);

            // return when image not found in library.
            if (index == -1)
                return false;
            
            return await LaunchPhotoInfoAsync(index);
        }

        /// <summary>
        /// Launches Photo-Info.
        /// </summary>
        /// <param name="mediaLibIndex">The file index in the library.</param>
        /// <returns>True for success, else false.</returns>
        public static async Task<bool> LaunchPhotoInfoAsync(int mediaLibIndex)
        {
            var uriString = string.Format("photoinfo:show?mediaLibIndex={0}", mediaLibIndex);
            return await Launcher.LaunchUriAsync(new Uri(uriString, UriKind.Absolute));
        }
    }
}
