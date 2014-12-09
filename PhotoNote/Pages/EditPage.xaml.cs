using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using PhoneKit.Framework.Core.Storage;
using PhoneKit.Framework.Core.Tile;
using Microsoft.Xna.Framework.Media;
using System.Diagnostics;
using PhotoNote.Model;
using System.Windows.Media.Imaging;
using System.Windows.Input;
using System.Windows.Ink;
using System.Windows.Media;
using PhoneKit.Framework.Core.Graphics;
using System.IO;
using PhotoNote.Resources;
using PhotoNote.Helpers;

namespace PhotoNote.Pages
{
    public partial class EditPage : PhoneApplicationPage
    {
        private string currentImageName;

        private Random rand = new Random();

        public EditPage()
        {
            InitializeComponent();
            BuildLocalizedApplicationBar();
            //SetBoundary();
        }

        /// <summary>
        /// Builds the localized app bar.
        /// </summary>
        private void BuildLocalizedApplicationBar()
        {
            // ApplicationBar der Seite einer neuen Instanz von ApplicationBar zuweisen
            ApplicationBar = new ApplicationBar();
            ApplicationBar.Opacity = 0.99;

            // save tile
            ApplicationBarIconButton appBarTileButton = new ApplicationBarIconButton(new Uri("/Assets/AppBar/appbar.save.png", UriKind.Relative));
            appBarTileButton.Text = AppResources.AppBarSave;
            appBarTileButton.Click += (s, e) =>
            {
                Save();
            };
            ApplicationBar.Buttons.Add(appBarTileButton);

            // image info (photo info)
            ApplicationBarMenuItem appBarPhotoInfoMenuItem = new ApplicationBarMenuItem(AppResources.ShowPhotoInfo);
            appBarPhotoInfoMenuItem.Click += async (s, e) =>
            {
                if (!await LauncherHelper.LaunchPhotoInfoAsync(currentImageName))
                {
                    MessageBox.Show(AppResources.MessageBoxNoInfo, AppResources.MessageBoxWarning, MessageBoxButton.OK);
                }
            };
            ApplicationBar.MenuItems.Add(appBarPhotoInfoMenuItem);
        }

        private void Save()
        {
            using (var memStream = new MemoryStream())
            {
                var gfx = GraphicsHelper.Create(EditImageContainer);
                gfx.SaveJpeg(memStream, (int)EditImageContainer.ActualWidth, (int)EditImageContainer.ActualHeight, 0, 100);
                memStream.Seek(0, SeekOrigin.Begin);

                using (var media = StaticMediaLibrary.Instance)
                {
                    var nameWithoutExtension = Path.GetFileNameWithoutExtension(currentImageName);

                    // prepend image prefix
                    if (!nameWithoutExtension.StartsWith(AppConstants.IMAGE_PREFIX))
                    {
                        nameWithoutExtension = AppConstants.IMAGE_PREFIX + nameWithoutExtension;
                    }
                    else
                    {
                        // remove "_XXXX" postfix of previously save photo note
                        nameWithoutExtension = nameWithoutExtension.Substring(0, nameWithoutExtension.Length - 5);
                    }

                    // save
                    media.SavePicture(string.Format("{0}_{1:0000}.jpg", nameWithoutExtension, rand.Next(9999)), memStream);
                }
            }
            
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            // query string lookup
            bool success = false;
            if (NavigationContext.QueryString != null)
            {
                if (e.NavigationMode == NavigationMode.Back)
                {
                    BackOrTerminate();
                }

                if (NavigationContext.QueryString.ContainsKey(AppConstants.PARAM_FILE_TOKEN))
                {
                    var token = NavigationContext.QueryString[AppConstants.PARAM_FILE_TOKEN];

                    var image = GetImageFromToken(token);
                    if (image != null)
                    {
                        if (UpdatePicture(image))
                        {
                            success = true;
                        }
                    }
                }

                if (NavigationContext.QueryString.ContainsKey(AppConstants.PARAM_SELECTED_FILE_NAME))
                {
                    var selectedFileName = NavigationContext.QueryString[AppConstants.PARAM_SELECTED_FILE_NAME];

                    var image = GetImageFromFileName(selectedFileName);
                    if (image != null)
                    {
                        if (UpdatePicture(image))
                        {
                            success = true;
                        }
                    }
                }

                if (NavigationContext.QueryString.ContainsKey(AppConstants.PARAM_FILE_NAME))
                {
                    var fileName = NavigationContext.QueryString[AppConstants.PARAM_FILE_NAME];
                    //var imagePath = string.Format("{0}{1}", LiveTileHelper.SHARED_SHELL_CONTENT_PATH, fileName);

                    //if (StorageHelper.FileExists(imagePath))
                    //{
                    //    var file = await Windows.Storage.StorageFile.GetFileFromApplicationUriAsync(new Uri(StorageHelper.APPDATA_LOCAL_SCHEME + imagePath));
                    //    await Windows.System.Launcher.LaunchFileAsync(file);
                    //}
                }

                // error handling - warning and go back or exit
                if (!success)
                {
                    //MessageBox.Show(AppResources.MessageBoxNoImageFound, AppResources.MessageBoxWarning, MessageBoxButton.OK);
                    BackOrTerminate();
                    return;
                }
            }
        }

        private bool UpdatePicture(EditPicture pic)
        {
            currentImageName = pic.Name;
            BitmapImage img = new BitmapImage();
            using (var imageStream = pic.ImageStream)
            {
                // in case of a not successfully saved image
                if (imageStream == null)
                {
                    EditImageControl.Source = null;
                    EditImageContainer.Visibility = System.Windows.Visibility.Collapsed;
                    return false;
                }

                img.SetSource(imageStream);
                EditImageControl.Source = img;
            }
            return true;
        }

        private EditPicture GetImageFromToken(string token)
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

        private EditPicture GetImageFromFileName(string fileName)
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
            // TODO: check if still necessary?!?
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

        /// <summary>
        /// Replacement for Path.GetFileNameWithoutExtension(), which throw "ArgumentException: Illegal characters in path".
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        private string ExtractFileExtension(string fileName)
        {
            var extensionStartIndex = fileName.LastIndexOf('.');

            if (extensionStartIndex != -1)
            {
                fileName = fileName.Substring(0, Math.Max(1, extensionStartIndex));
            }

            return fileName;
        }

        /// <summary>
        /// Goes back or terminates the app when the back stack is empty.
        /// </summary>
        private void BackOrTerminate()
        {
            if (NavigationService.CanGoBack)
                NavigationService.GoBack();
            else
                App.Current.Terminate();
        }

        #region  INK REGION

        Stroke NewStroke;

        //A new stroke object named MyStroke is created. MyStroke is added to the StrokeCollection of the InkPresenter named MyIP
        private void MyIP_MouseLeftButtonDown(object sender, MouseEventArgs e)
        {
            MyIP.CaptureMouse();
            StylusPointCollection MyStylusPointCollection = new StylusPointCollection();
            MyStylusPointCollection.Add(e.StylusDevice.GetStylusPoints(MyIP));
            NewStroke = new Stroke(MyStylusPointCollection);
            NewStroke.DrawingAttributes = new DrawingAttributes
            {
                Color = Color.FromArgb(50, 255, 255, 0),
                Height = 12,
                Width = 12,
            };
            MyIP.Strokes.Add(NewStroke);
        }

        //StylusPoint objects are collected from the MouseEventArgs and added to MyStroke. 
        private void MyIP_MouseMove(object sender, MouseEventArgs e)
        {
            if (NewStroke != null)
                NewStroke.StylusPoints.Add(e.StylusDevice.GetStylusPoints(MyIP));
        }

        //MyStroke is completed
        private void MyIP_LostMouseCapture(object sender, MouseEventArgs e)
        {
            NewStroke = null;

        }

        //Set the Clip property of the inkpresenter so that the strokes
        //are contained within the boundary of the inkpresenter
        private void SetBoundary()
        {
            RectangleGeometry MyRectangleGeometry = new RectangleGeometry();
            MyRectangleGeometry.Rect = new Rect(0, 0, MyIP.ActualWidth, MyIP.ActualHeight);
            MyIP.Clip = MyRectangleGeometry;
        }

        #endregion

        #region Orientation Events

        protected override void OnOrientationChanged(OrientationChangedEventArgs e)
        {
            base.OnOrientationChanged(e);

            if (e.Orientation == PageOrientation.Portrait ||
                e.Orientation == PageOrientation.PortraitDown ||
                e.Orientation == PageOrientation.PortraitUp)
            {
                VisualStateManager.GoToState(this, "Portrait", true);
            } 
            else if (e.Orientation == PageOrientation.Landscape ||
                e.Orientation == PageOrientation.LandscapeLeft ||
                e.Orientation == PageOrientation.LandscapeRight)
            {
                VisualStateManager.GoToState(this, "Landscape", true);
            }
        }

        #endregion
    }
}