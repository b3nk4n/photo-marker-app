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
using PhoneKit.Framework.Storage;
using System.Threading.Tasks;
using System.Linq;
using System.Text;
using System.Globalization;

namespace PhotoNote.Pages
{
    public partial class EditPage : PhoneApplicationPage
    {
        private EditPicture _editImage;

        private Random rand = new Random();

        private static readonly ScaleTransform NEUTRAL_SCALE = new ScaleTransform();

        private bool _isPenToolbarVisible = false;

        private const string PEN_POPUP_VISIBLE_KEY = "_pen_popup_visible_";
        private const string PEN_DATA_KEY = "_pen_data_";

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


            // undo
            ApplicationBarIconButton appBarUndoButton = new ApplicationBarIconButton(new Uri("/Assets/AppBar/appbar.undo.curve.png", UriKind.Relative));
            appBarUndoButton.Text = AppResources.AppBarUndo;
            appBarUndoButton.Click += (s, e) =>
            {
                Undo();
            };
            ApplicationBar.Buttons.Add(appBarUndoButton);

            // pen toolbar
            ApplicationBarIconButton appBarPenButton = new ApplicationBarIconButton(new Uri("/Assets/AppBar/appbar.draw.marker.png", UriKind.Relative));
            appBarPenButton.Text = AppResources.AppBarPen;
            appBarPenButton.Click += (s, e) =>
            {
                if (_isPenToolbarVisible)
                {
                    HidePenToolbar();
                }
                else
                {
                    ShowPenToolbar();
                }
            };
            ApplicationBar.Buttons.Add(appBarPenButton);

            // save
            ApplicationBarIconButton appBarSaveButton = new ApplicationBarIconButton(new Uri("/Assets/AppBar/appbar.save.png", UriKind.Relative));
            appBarSaveButton.Text = AppResources.AppBarSave;
            appBarSaveButton.Click += async (s, e) =>
            {
                if (await Save())
                {
                    NavigationHelper.BackToMainPageWithHistoryClear(NavigationService);
                }
                else
                {
                    MessageBox.Show(AppResources.MessageBoxNoSave, AppResources.MessageBoxWarning, MessageBoxButton.OK);
                }
                
            };
            ApplicationBar.Buttons.Add(appBarSaveButton);


            // image info (photo info)
            ApplicationBarMenuItem appBarPhotoInfoMenuItem = new ApplicationBarMenuItem(AppResources.ShowPhotoInfo);
            appBarPhotoInfoMenuItem.Click += async (s, e) =>
            {
                if (HasNoImage())
                    return;

                if (!await LauncherHelper.LaunchPhotoInfoAsync(_editImage.FullName))
                {
                    MessageBox.Show(AppResources.MessageBoxNoInfo, AppResources.MessageBoxWarning, MessageBoxButton.OK);
                }
            };
            ApplicationBar.MenuItems.Add(appBarPhotoInfoMenuItem);
        }

        private async Task<bool> Save()
        {
            if (HasNoImage())
                return false;

            SavingPopup.Visibility = System.Windows.Visibility.Visible;

            await Task.Delay(33);
            bool success = true;
            try
            {
                using (var memStream = new MemoryStream())
                {
                    var neutralScaleFactor = GetBiggestScaleFactorOfSmallerOrientation();
                    var editedImageInkControl = new EditedImageInkControl(_editImage.FullImage as BitmapSource, InkControl.Strokes, 1.0 / neutralScaleFactor);
                    var gfx = GraphicsHelper.Create(editedImageInkControl);
                    gfx.SaveJpeg(memStream, gfx.PixelWidth, gfx.PixelHeight, 0, 100);
                    memStream.Seek(0, SeekOrigin.Begin);

                    using (var media = StaticMediaLibrary.Instance)
                    {
                        var nameWithoutExtension = Path.GetFileNameWithoutExtension(_editImage.Name);

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
            catch (Exception)
            {
                success = false;
            } 
            finally
            {
                SavingPopup.Visibility = System.Windows.Visibility.Collapsed;
            }

            return success;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            // query string lookup
            bool success = false;
            if (NavigationContext.QueryString != null)
            {
                if (NavigationContext.QueryString.ContainsKey(AppConstants.PARAM_FILE_TOKEN))
                {
                    var token = NavigationContext.QueryString[AppConstants.PARAM_FILE_TOKEN];

                    var image = StaticMediaLibrary.GetImageFromToken(token);
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

                    var image = StaticMediaLibrary.GetImageFromFileName(selectedFileName);
                    if (image != null)
                    {
                        if (UpdatePicture(image))
                        {
                            success = true;
                        }
                    }
                }

                // error: go back or exit
                if (!success)
                {
                    MessageBox.Show(AppResources.MessageBoxUnknownError, AppResources.MessageBoxWarning, MessageBoxButton.OK);
                    NavigationHelper.BackToMainPageWithHistoryClear(NavigationService);
                    return;
                }
                
                if (e.NavigationMode == NavigationMode.New)
                    RestoreState();

                LoadSettings();
            }
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);

            // save state
            if (!e.IsNavigationInitiator)
            {
                SaveState();
            }
        }

        private void LoadSettings()
        {
            this.ColorPicker.Color = AppSettings.PenColor.Value;
            this.OpacitySlider.Value = AppSettings.PenOpacity.Value;
            this.ThicknessSlider.Value = AppSettings.PenThickness.Value;
        }

        private void SaveSettings()
        {
            AppSettings.PenColor.Value = this.ColorPicker.Color;
            AppSettings.PenOpacity.Value = this.OpacitySlider.Value;
            AppSettings.PenThickness.Value = this.ThicknessSlider.Value;
        }

        private void RestoreState()
        {
            // popup state
            var showPenToolbar = PhoneStateHelper.LoadValue<bool>(PEN_POPUP_VISIBLE_KEY, false);
            PhoneStateHelper.DeleteValue(PEN_POPUP_VISIBLE_KEY);
            if (showPenToolbar)
            {
                ShowPenToolbar(false);
            }

            // strokes
            var strokeData = PhoneStateHelper.LoadValue<string>(PEN_DATA_KEY);
            PhoneStateHelper.DeleteValue(PEN_DATA_KEY);
            if (!string.IsNullOrEmpty(strokeData))
            {
                var strokes = strokeData.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var stroke in strokes)
                {
                    var strokeParams = stroke.Split('|');

                    var myStroke = new Stroke();
                    myStroke.DrawingAttributes.Color = HexToColor(strokeParams[0]);
                    myStroke.DrawingAttributes.Height = double.Parse(strokeParams[1], CultureInfo.InvariantCulture);
                    myStroke.DrawingAttributes.Width = double.Parse(strokeParams[2], CultureInfo.InvariantCulture);

                    var pointList = strokeParams[3].Split('$');
                    foreach (var pointPair in pointList)
                    {
                        var pointPairList = pointPair.Split('_');
                        var x = Convert.ToDouble(pointPairList[0], CultureInfo.InvariantCulture);
                        var y = Convert.ToDouble(pointPairList[1], CultureInfo.InvariantCulture);

                        myStroke.StylusPoints.Add(new StylusPoint(x, y));
                    }

                    InkControl.Strokes.Add(myStroke);
                }
            }
        }

        private static Color HexToColor(string hexString)
        {
            string cleanString = hexString.Replace("-", String.Empty).Substring(1);

            var bytes = Enumerable.Range(0, cleanString.Length)
                           .Where(x => x % 2 == 0)
                           .Select(x => Convert.ToByte(cleanString.Substring(x, 2), 16))
                           .ToArray();

            return System.Windows.Media.Color.FromArgb(bytes[0], bytes[1], bytes[2], bytes[3]);
        }

        private void SaveState()
        {
            // popup state
            PhoneStateHelper.SaveValue(PEN_POPUP_VISIBLE_KEY, _isPenToolbarVisible);
            
            // strokes
            if (InkControl.Strokes.Count > 0)
            {
                StringBuilder strokeData = new StringBuilder();
                foreach (var stroke in InkControl.Strokes)
                {
                    strokeData.AppendLine(String.Format("{0}|{1}|{2}|{3}",
                        stroke.DrawingAttributes.Color.ToString(),
                        stroke.DrawingAttributes.Height.ToString(CultureInfo.InvariantCulture),
                        stroke.DrawingAttributes.Width.ToString(CultureInfo.InvariantCulture),
                        String.Join("$", stroke.StylusPoints.Select(p => String.Format("{0}_{1}", p.X.ToString(CultureInfo.InvariantCulture), p.Y.ToString(CultureInfo.InvariantCulture))))));
                }
                PhoneStateHelper.SaveValue(PEN_DATA_KEY, strokeData.ToString());
            }
        }

        private bool UpdatePicture(EditPicture pic)
        {
            _editImage = pic;
            BitmapSource img = new BitmapImage();
            using (var imageStream = pic.ImageStream)
            {
                // in case of a not successfully saved image
                if (imageStream == null)
                {
                    EditImageControl.Source = null;
                    EditImageContainer.Visibility = System.Windows.Visibility.Collapsed;
                    _editImage = null;
                    return false;
                }

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
            EditImageControl.Width = scale * _editImage.Width;
            EditImageControl.Height = scale * _editImage.Height;

            // ink surface
            var neutralScaleFactor = GetBiggestScaleFactorOfSmallerOrientation();
            InkControl.Width = neutralScaleFactor * _editImage.Width;
            InkControl.Height = neutralScaleFactor * _editImage.Height;

            // check if upper-scaling is required
            if (scale != neutralScaleFactor)
            {
                InkControl.RenderTransform = new ScaleTransform
                {
                    ScaleX = scale / neutralScaleFactor,
                    ScaleY = scale / neutralScaleFactor
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
            return _editImage == null || _editImage.Height == 0 || _editImage.Width == 0;
        }

        private Size GetScaledImageSize(double scaleFactor)
        {
            return new Size(_editImage.Width * scaleFactor,
                _editImage.Height * scaleFactor);
        }

        private double GetScaleFactorOfOrientation()
        {
            var viewportBounds = GetViewportBounds();

            var heightScale = viewportBounds.Height / _editImage.Height;
            var widthScale = viewportBounds.Width / _editImage.Width;
            return (heightScale < widthScale) ? heightScale : widthScale;
        }

        private double GetBiggestScaleFactorOfSmallerOrientation()
        {
            var viewportBounds = GetNeutralViewportBounds();

            var heightScale = viewportBounds.Height / _editImage.Height;
            var widthScale = viewportBounds.Width / _editImage.Width;
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

        #region  INK REGION

        private Stroke _activeStroke;

        //A new stroke object named MyStroke is created. MyStroke is added to the StrokeCollection of the InkPresenter named MyIP
        private void MyIP_MouseLeftButtonDown(object sender, MouseEventArgs e)
        {
            if (_isPenToolbarVisible)
                return;

            InkControl.CaptureMouse();
            StylusPointCollection MyStylusPointCollection = new StylusPointCollection();
            MyStylusPointCollection.Add(e.StylusDevice.GetStylusPoints(InkControl));

            var opacity = AppSettings.PenOpacity.Value;
            var color = AppSettings.PenColor.Value;
            var size = AppSettings.PenThickness.Value;
            _activeStroke = new Stroke(MyStylusPointCollection);
            _activeStroke.DrawingAttributes = new DrawingAttributes
            {
                Color = Color.FromArgb((byte)(255 * opacity), color.R, color.G, color.B),
                Height = size,
                Width = size,
            };
            InkControl.Strokes.Add(_activeStroke);
        }

        //StylusPoint objects are collected from the MouseEventArgs and added to MyStroke. 
        private void MyIP_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isPenToolbarVisible)
                return;

            if (_activeStroke != null)
            {
                var stylusPoints = e.StylusDevice.GetStylusPoints(InkControl);
                _activeStroke.StylusPoints.Add(stylusPoints);
            }
                
        }

        //MyStroke is completed
        private void MyIP_LostMouseCapture(object sender, MouseEventArgs e)
        {
            _activeStroke = null;
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
            return _activeStroke == null && InkControl.Strokes.Count > 0;
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
                e.Orientation == PageOrientation.LandscapeLeft)
            {
                VisualStateManager.GoToState(this, "LandscapeLeft", true);
            }
            else if (e.Orientation == PageOrientation.LandscapeRight)
            {
                VisualStateManager.GoToState(this, "LandscapeRight", true);
            }

            UpdateImageOrientationAndScale();
        }

        #endregion

        private void ClosedPenToobarTapped(object sender, System.Windows.Input.GestureEventArgs e)
        {
            HidePenToolbar();
        }

        protected override void OnBackKeyPress(System.ComponentModel.CancelEventArgs e)
        {
            if (_isPenToolbarVisible)
            {
                e.Cancel = true;
                HidePenToolbar();
            }

            base.OnBackKeyPress(e);
        }

        public void ShowPenToolbar(bool useTransition=true)
        {
            if (_isPenToolbarVisible)
                return;

            // make sure the right toobar is visible (required for the first launch
            if (Orientation == PageOrientation.Portrait ||
                Orientation == PageOrientation.PortraitDown ||
                Orientation == PageOrientation.PortraitUp)
            {
                PenToolbar.Visibility = System.Windows.Visibility.Visible;
            }
            else
            {
                PenToolbarLandscape.Visibility = System.Windows.Visibility.Visible;
            }

            VisualStateManager.GoToState(this, "Displayed", useTransition);
            _isPenToolbarVisible = true;
        }

        public void HidePenToolbar()
        {
            if (!_isPenToolbarVisible)
                return;

            VisualStateManager.GoToState(this, "Normal", true);
            _isPenToolbarVisible = false;

            SaveSettings();
        }
    }
}