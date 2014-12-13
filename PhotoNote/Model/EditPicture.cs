using Microsoft.Phone;
using Microsoft.Phone.Tasks;
using Microsoft.Xna.Framework.Media;
using Microsoft.Xna.Framework.Media.PhoneExtensions;
using System.IO;
using System.Windows.Media;

namespace PhotoNote.Model
{
    public class EditPicture
    {
        private Picture _image;

        public EditPicture(Picture image)
        {
            _image = image;
        }

        public void Share()
        {
            var shareTask = new ShareMediaTask();
            shareTask.FilePath = ImagePath;
            shareTask.Show();
        }

        public Stream ThumbnailImageStream
        {
            get
            {
                return _image.GetThumbnail();
            }
        }

        public Stream ImageStream
        {
            get
            {
                return _image.GetPreviewImage();
            }
        }

        public Stream FullImageStream
        {
            get
            {
                return _image.GetImage();
            }
        }

        public ImageSource ThumbnailImage
        {
            get
            {
                return PictureDecoder.DecodeJpeg(_image.GetThumbnail());
            }
        }

        public ImageSource Image
        {
            get
            {
                return PictureDecoder.DecodeJpeg(_image.GetPreviewImage());
            }
        }

        public ImageSource FullImage
        {
            get
            {
                return PictureDecoder.DecodeJpeg(_image.GetImage());
            }
        }

        public string ImagePath
        {
            get
            {
                return _image.GetPath();
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
                return Path.GetExtension(_image.GetPath()).Replace(".", string.Empty).ToUpper();
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
