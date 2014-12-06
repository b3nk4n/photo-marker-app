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
        }

        public void Update()
        {
            EditPictureList.Clear();

            foreach (var picture in StaticMediaLibrary.Instance.Pictures)
            {
                if (picture.Name.StartsWith(AppConstants.IMAGE_PREFIX))
                {
                    EditPictureList.Add(new EditPicture(picture));

                    // stop after 10 photos ... thats enough for the start page
                    if (EditPictureList.Count == 10)
                        break;
                }
            }

            NotifyPropertyChanged("IsEditPictureListEmpty");
            NotifyPropertyChanged("Picture1");
            NotifyPropertyChanged("Picture2");
            NotifyPropertyChanged("Picture3");
            NotifyPropertyChanged("Picture4");
            NotifyPropertyChanged("Picture5");
            NotifyPropertyChanged("Picture6");
            NotifyPropertyChanged("Picture7");
            NotifyPropertyChanged("Picture8");
            NotifyPropertyChanged("Picture9");
            NotifyPropertyChanged("Picture10");
            NotifyPropertyChanged("PictureName1");
            NotifyPropertyChanged("PictureName2");
            NotifyPropertyChanged("PictureName3");
            NotifyPropertyChanged("PictureName4");
            NotifyPropertyChanged("PictureName5");
            NotifyPropertyChanged("PictureName6");
            NotifyPropertyChanged("PictureName7");
            NotifyPropertyChanged("PictureName8");
            NotifyPropertyChanged("PictureName9");
            NotifyPropertyChanged("PictureName10");
        }

        public IList<EditPicture> EditPictureList
        {
            get { return _editPictureList; }
        }

        public bool IsEditPictureListEmpty
        {
            get { return _editPictureList.Count == 0; }
        }

        public ImageSource Picture1
        {
            get { return GetPicture(0); }
        }

        public ImageSource Picture2
        {
            get { return GetPicture(1); }
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

        public ImageSource Picture7
        {
            get { return GetPicture(6); }
        }

        public ImageSource Picture8
        {
            get { return GetPicture(7); }
        }

        public ImageSource Picture9
        {
            get { return GetPicture(8); }
        }

        public ImageSource Picture10
        {
            get { return GetPicture(9); }
        }

        private ImageSource GetPicture(int index) {
            if (index < 0 || index >= _editPictureList.Count)
                return null;

            BitmapImage image = new BitmapImage();
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

        public string PictureName7
        {
            get { return GetName(6); }
        }

        public string PictureName8
        {
            get { return GetName(7); }
        }

        public string PictureName9
        {
            get { return GetName(8); }
        }

        public string PictureName10
        {
            get { return GetName(9); }
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
    }
}
