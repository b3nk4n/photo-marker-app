using Microsoft.Phone.Tasks;
using PhoneKit.Framework.Core.MVVM;
using PhotoNote.Helpers;
using PhotoNote.Model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace PhotoNote.ViewModel
{
    /// <summary>
    /// The view model of the main page.
    /// </summary>
    public class MainViewModel : ViewModelBase
    {
        public IList<EditPicture> _editPictureList = new ObservableCollection<EditPicture>();

        private DelegateCommand<string> _noteTileSelectedCommand;
        private DelegateCommand<string> _shareCommand;

        public MainViewModel()
        {
            InitializeCommands();
        }

        private void InitializeCommands()
        {
            _noteTileSelectedCommand = new DelegateCommand<string>(async (fileName) =>
            {
                if (!await LauncherHelper.LaunchPhotoAsync(fileName))
                {
                    // TODO error handling...
                }
            });

            _shareCommand = new DelegateCommand<string>((indexString) =>
            {
                var index = int.Parse(indexString);

                if (_editPictureList.Count <= index)
                    return;

                var picture = _editPictureList[index];

                // share media
                var shareTask = new ShareMediaTask();
                shareTask.FilePath = picture.ImagePath;
                shareTask.Show();
            });
        }

        public bool CheckHasAnyPicture()
        {
            foreach (var picture in StaticMediaLibrary.Instance.Pictures)
            {
                if (picture.Name.StartsWith(AppConstants.IMAGE_PREFIX))
                {
                    return true;
                }
            }

            return false;
        }

        public void Update()
        {
            EditPictureList.Clear();

            foreach (var picture in StaticMediaLibrary.Instance.Pictures)
            {
                if (picture.Name.StartsWith(AppConstants.IMAGE_PREFIX))
                {
                    EditPictureList.Insert(0, new EditPicture(picture));

                    // stop after 10 photos ... thats enough for the start page
                    if (EditPictureList.Count == 10)
                        break;
                }
            }

            NotifyPropertyChanged("Picture1");
            NotifyPropertyChanged("Picture2");
            NotifyPropertyChanged("Picture3");
            NotifyPropertyChanged("Picture4");
            NotifyPropertyChanged("Picture5");
            NotifyPropertyChanged("Picture6");
            NotifyPropertyChanged("PictureName1");
            NotifyPropertyChanged("PictureName2");
            NotifyPropertyChanged("PictureName3");
            NotifyPropertyChanged("PictureName4");
            NotifyPropertyChanged("PictureName5");
            NotifyPropertyChanged("PictureName6");
        }

        public IList<EditPicture> EditPictureList
        {
            get { return _editPictureList; }
        }

        public ImageSource Picture1
        {
            get { return GetPicture(0, false); }
        }

        public ImageSource Picture2
        {
            get { return GetPicture(1, false); }
        }

        public ImageSource Picture3
        {
            get { return GetPicture(2); }
        }

        public ImageSource Picture4
        {
            get { return GetPicture(3); }
        }

        public ImageSource Picture5
        {
            get { return GetPicture(4); }
        }

        public ImageSource Picture6
        {
            get { return GetPicture(5); }
        }

        private ImageSource GetPicture(int index, bool lowQuality = true) {
            if (index < 0 || index >= _editPictureList.Count)
                return null;

            BitmapImage image = new BitmapImage();
            var stream = (lowQuality) ? _editPictureList[index].ThumbnailImageStream : _editPictureList[index].ImageStream;
            using (var imageStream = _editPictureList[index].ImageStream)
            {
                if (imageStream == null)
                    return null;

                image.SetSource(imageStream);
                return image;
            }
        }

        public string PictureName1
        {
            get { return GetName(0); }
        }

        public string PictureName2
        {
            get { return GetName(1); }
        }

        public string PictureName3
        {
            get { return GetName(2); }
        }

        public string PictureName4
        {
            get { return GetName(3); }
        }

        public string PictureName5
        {
            get { return GetName(4); }
        }

        public string PictureName6
        {
            get { return GetName(5); }
        }

        private string GetName(int index)
        {
            if (index < 0 || index >= _editPictureList.Count)
                return null;

            return _editPictureList[index].Name;
        }

        public ICommand NoteTileSelectedCommand
        {
            get { return _noteTileSelectedCommand; }
        }

        public ICommand ShareCommand
        {
            get { return _shareCommand; }
        }
    }
}
