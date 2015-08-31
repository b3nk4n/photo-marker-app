using PhotoNote.Model;
using System;
using System.Threading.Tasks;
using Windows.System;

namespace PhotoNote.Helpers
{
    public static class LauncherHelper
    {
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
