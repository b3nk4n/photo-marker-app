using Microsoft.Phone.Tasks;
using Microsoft.Xna.Framework.Media;
using Microsoft.Xna.Framework.Media.PhoneExtensions;
using PhoneKit.Framework.Graphics;
using System;
using System.IO;
using System.Windows.Media;

namespace PhotoNote.Model
{
    public class EditPicture : IDisposable
    {
        private RobustPicture _image;

        public EditPicture(Picture image)
        {
            _image = new RobustPicture(image);
        }

        public void Share()
        {
            var shareTask = new ShareMediaTask();
            shareTask.FilePath = ImagePath;
            shareTask.Show();
        }

        /// <summary>
        /// Disposes the image resource.
        /// </summary>
        public void Dispose()
        {
            _image.Dispose();
            _image = null;
        }

        public ImageSource ThumbnailImage
        {
            get
            {
                return _image.ThumbnailImage;
            }
        }

        public ImageSource Image
        {
            get
            {
                return _image.PreviewImage;
            }
        }

        public ImageSource FullImage
        {
            get
            {
                return _image.Image;
            }
        }

        public string ImagePath
        {
            get
            {
                return _image.InternalPicture.GetPath();
            }
        }

        public string Name
        {
            get
            {
                return Path.GetFileNameWithoutExtension(ImagePath);
            }
        }

        public string FullName
        {
            get
            {
                return Path.GetFileName(ImagePath);
            }
        }

        public string FileType
        {
            get
            {
                return Path.GetExtension(ImagePath).Replace(".", string.Empty).ToUpper();
            }
        }

        public double Width
        {
            get { return _image.Width; }
        }

        public double Height
        {
            get { return _image.Height; }
        }
    }
}
