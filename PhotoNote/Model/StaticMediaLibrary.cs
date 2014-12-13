using Microsoft.Xna.Framework.Media;
using System;
using System.Diagnostics;

namespace PhotoNote.Model
{
    /// <summary>
    /// Global access to the media library.
    /// </summary>
    public static class StaticMediaLibrary
    {
        private static MediaLibrary _mediaLibrary;

        /// <summary>
        /// Gets the media library
        /// </summary>
        public static MediaLibrary Instance
        {
            get
            {
                if (_mediaLibrary == null || _mediaLibrary.IsDisposed)
                    _mediaLibrary = new MediaLibrary();
                return _mediaLibrary;
            }
        }

        public static EditPicture GetImageFromToken(string token)
        {
            Picture image = null;

            try
            {
                image = StaticMediaLibrary.Instance.GetPictureFromToken(token);
            }
            catch (InvalidOperationException ioex)
            {
                Debug.WriteLine("Could not retrieve photo from library with error: " + ioex.Message);
            }

            return (image == null) ? null : new EditPicture(image);
        }

        public static EditPicture GetImageFromFileName(string fileName)
        {
            try
            {
                foreach (var pic in StaticMediaLibrary.Instance.Pictures)
                {
                    if (pic.Name == fileName)
                    {
                        return new EditPicture(pic);
                    }
                }
                Debug.WriteLine("pic: " + fileName);
            }
            catch (InvalidOperationException ioex)
            {
                Debug.WriteLine("Could not retrieve photo from library with error: " + ioex.Message);
            }

            // second try, because sometime the file extenstion was not applied.
            foreach (var pic in StaticMediaLibrary.Instance.Pictures)
            {
                var nameWithoutCounter = RemoveImageCopyCounter(fileName);
                if (pic.Name.Contains(fileName) || fileName.Contains(pic.Name) ||
                    pic.Name.Contains(nameWithoutCounter) || pic.Name.Contains(nameWithoutCounter))
                {
                    return new EditPicture(pic);
                }
            }
            return null;
        }

        public static int GetMediaLibIndexFromFileName(string fileName)
        {
            try
            {
                int i = 0;
                foreach (var pic in StaticMediaLibrary.Instance.Pictures)
                {
                    if (pic.Name == fileName)
                    {
                        return i;
                    }
                    i++;
                }
            }
            catch (InvalidOperationException ioex)
            {
                Debug.WriteLine("Could not retrieve photo from library with error: " + ioex.Message);
            }

            // second try, because sometime the file extenstion was not applied.
            int j = 0;
            foreach (var pic in StaticMediaLibrary.Instance.Pictures)
            {
                var nameWithoutCounter = RemoveImageCopyCounter(fileName);
                if (pic.Name.Contains(fileName) || fileName.Contains(pic.Name) ||
                    pic.Name.Contains(nameWithoutCounter) || pic.Name.Contains(nameWithoutCounter))
                {
                    return j;
                }
                j++;
            }
            return -1;
        }

        private static string RemoveImageCopyCounter(string fileName)
        {
            if (fileName.Length <= 3)
                return fileName;

            var bracketsStart = fileName.IndexOf('(');
            var bracketsEnd = fileName.IndexOf(')');

            if (bracketsStart != -1 && bracketsEnd != -1 && bracketsStart < bracketsEnd)
            {
                try
                {
                    return fileName.Substring(0, bracketsStart);
                }
                catch (Exception) { }
            }

            return fileName;
        }
    }
}
