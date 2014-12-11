using System;
using System.Windows;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
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
using PhotoNote.Controls;

namespace PhotoNote.Pages
{
    public partial class EditPage : PhoneApplicationPage
    {
        private string currentImageName;

        private Random rand = new Random();

        private double _imageOriginalHeight;
        private double _imageOriginalWidth;

        private static readonly ScaleTransform NEUTRAL_SCALE = new ScaleTransform();

        public EditPage()
        {
            InitializeComponent();
            BuildLocalizedApplicationBar();
        }

        /// <summary>
        /// Builds the localized app bar.
        /// </summary>
        private void BuildLocalizedApplicationBar()
        {
            // ApplicationBar der Seite einer neuen Instanz von ApplicationBar zuweisen
            ApplicationBar = new ApplicationBar();
            ApplicationBar.Opacity = 0.99;

            // save
            ApplicationBarIconButton appBarSaveButton = new ApplicationBarIconButton(new Uri("/Assets/AppBar/appbar.save.png", UriKind.Relative));
            appBarSaveButton.Text = AppResources.AppBarSave;
            appBarSaveButton.Click += (s, e) =>
            {
                Save();
            };
            ApplicationBar.Buttons.Add(appBarSaveButton);

            // undo
            ApplicationBarIconButton appBarUndoButton = new ApplicationBarIconButton(new Uri("/Assets/AppBar/appbar.undo.curve.png", UriKind.Relative));
            appBarUndoButton.Text = AppResources.AppBarUndo;
            appBarUndoButton.Click += (s, e) =>
            {
                Undo();
            };
            ApplicationBar.Buttons.Add(appBarUndoButton);

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
                var editedImageInkControl = new EditedImageInkControl(EditImageControl.Source as BitmapSource);
                var gfx = GraphicsHelper.Create(editedImageInkControl);
                gfx.SaveJpeg(memStream, gfx.PixelWidth, gfx.PixelHeight, 0, 100);
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
            BitmapSource img = new BitmapImage();
            using (var imageStream = pic.ImageStream)
            {
                // in case of a not successfully saved image
                if (imageStream == null)
                {
                    EditImageControl.Source = null;
                    EditImageContainer.Visibility = System.Windows.Visibility.Collapsed;
                    _imageOriginalHeight = 0;
                    _imageOriginalWidth = 0;
                    return false;
                }
                _imageOriginalHeight = pic.Height;
                _imageOriginalWidth = pic.Width;
                img.SetSource(imageStream);

                UpdateImageOrientationAndScale();

                EditImageControl.Source = img;
            }
            return true;
        }

        private void UpdateImageOrientationAndScale()
        {
            if (HasNoImage())
                return;

            // image
            var scale = GetScaleFactorOfOrientation();
            EditImageControl.Width = scale * _imageOriginalWidth;
            EditImageControl.Height = scale * _imageOriginalHeight;

            // ink surface
            var neutralScaleFactors = GetBiggestScaleFactorOfSmallerOrientation();
            InkControl.Width = neutralScaleFactors * _imageOriginalWidth;
            InkControl.Height = neutralScaleFactors * _imageOriginalHeight;

            // check if upper-scaling is required
            if (scale != neutralScaleFactors)
            {
                InkControl.RenderTransform = new ScaleTransform
                {
                    ScaleX = scale / neutralScaleFactors,
                    ScaleY = scale / neutralScaleFactors
                };
            }
            else
            {
                InkControl.RenderTransform = NEUTRAL_SCALE;
            }

            SetBoundary(InkControl.Width, InkControl.Height);
        }

        private bool HasNoImage()
        {
            return _imageOriginalHeight == 0 || _imageOriginalWidth == 0;
        }

        private Size GetScaledImageSize(double scaleFactor)
        {
            return new Size(_imageOriginalWidth * scaleFactor,
                _imageOriginalHeight * scaleFactor);
        }

        private double GetScaleFactorOfOrientation()
        {
            var viewportBounds = GetViewportBounds();

            var heightScale = viewportBounds.Height / _imageOriginalHeight;
            var widthScale = viewportBounds.Width / _imageOriginalWidth;
            return (heightScale < widthScale) ? heightScale : widthScale;
        }

        private double GetBiggestScaleFactorOfSmallerOrientation()
        {
            var viewportBounds = GetNeutralViewportBounds();

            var heightScale = viewportBounds.Height / _imageOriginalHeight;
            var widthScale = viewportBounds.Width / _imageOriginalWidth;
            return (heightScale > widthScale) ? heightScale : widthScale;
        }

        private Size GetViewportBounds()
        {
            
            if (Orientation == PageOrientation.Portrait ||
                Orientation == PageOrientation.PortraitDown ||
                Orientation == PageOrientation.PortraitUp)
            {
                return new Size(480, 728);
            }
            else // landscape
            {
                return new Size(728, 480);
            }
        }

        private Size GetNeutralViewportBounds()
        {
            return new Size(480, 480);
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
            InkControl.CaptureMouse();
            StylusPointCollection MyStylusPointCollection = new StylusPointCollection();
            MyStylusPointCollection.Add(e.StylusDevice.GetStylusPoints(InkControl));
            
            NewStroke = new Stroke(MyStylusPointCollection);
            NewStroke.DrawingAttributes = new DrawingAttributes
            {
                Color = Color.FromArgb(50, 255, 255, 0),
                Height = 12,
                Width = 12,
            };
            InkControl.Strokes.Add(NewStroke);
        }

        //StylusPoint objects are collected from the MouseEventArgs and added to MyStroke. 
        private void MyIP_MouseMove(object sender, MouseEventArgs e)
        {
            if (NewStroke != null)
            {
                var stylusPoints = e.StylusDevice.GetStylusPoints(InkControl);
                NewStroke.StylusPoints.Add(stylusPoints);
            }
                
        }

        //MyStroke is completed
        private void MyIP_LostMouseCapture(object sender, MouseEventArgs e)
        {
            NewStroke = null;
        }

        //Set the Clip property of the inkpresenter so that the strokes
        //are contained within the boundary of the inkpresenter
        private void SetBoundary(double width, double height)
        {
            RectangleGeometry MyRectangleGeometry = new RectangleGeometry();
            MyRectangleGeometry.Rect = new Rect(0, 0, width, height);
            InkControl.Clip = MyRectangleGeometry;
        }

        private bool CanUndo()
        {
            return NewStroke == null && InkControl.Strokes.Count > 0;
        }

        private void Undo()
        {
            if (CanUndo())
            {
                InkControl.Strokes.RemoveAt(InkControl.Strokes.Count - 1);
            }
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

            UpdateImageOrientationAndScale();
        }

        #endregion
    }
}