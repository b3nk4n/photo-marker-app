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
using PhotoNote.Resources;
using PhotoNote.Helpers;
using PhotoNote.Controls;
using PhoneKit.Framework.Storage;
using System.Threading.Tasks;
using System.Linq;
using System.Text;
using System.Globalization;
using System.IO;

namespace PhotoNote.Pages
{
    public partial class EditPage : PhoneApplicationPage
    {
        private EditPicture _editImage;

        ApplicationBarIconButton _appBarZoomButton;

        private Random rand = new Random();

        private static readonly ScaleTransform NEUTRAL_SCALE = new ScaleTransform();

        private bool _isPenToolbarVisible = false;

        private const string PEN_POPUP_VISIBLE_KEY = "_pen_popup_visible_";
        private const string PEN_DATA_KEY = "_pen_data_";

        private const string ZOOM_KEY = "_zoom_";
        private const string TRANSLATION_X_KEY = "_trans_x_";
        private const string TRANSLATION_Y_KEY = "_trans_y_";

        private double _zoom = 1.0;
        private double _translateX;
        private double _translateY;

        private const double ZOOM_MAX = 3.0;
        private const double MOVE_STEP = 12.0;

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

            // zoom
            _appBarZoomButton = new ApplicationBarIconButton(new Uri("/Assets/AppBar/appbar.magnify.add.png", UriKind.Relative));
            _appBarZoomButton.Text = AppResources.AppBarZoom;
            _appBarZoomButton.Click += (s, e) =>
            {
                ToggleZoom();
            };
            ApplicationBar.Buttons.Add(_appBarZoomButton);

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

            if (e.NavigationMode == NavigationMode.Back &&
                    !e.IsNavigationInitiator) // to ensure this is only called after tombstone
                RestoreState();

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

                    // Update new content (e.g. when user took a new photo)
                    StaticMediaLibrary.Update();

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

            // zoom
            var zoomLevel = PhoneStateHelper.LoadValue<double>(ZOOM_KEY, 1.0);
            PhoneStateHelper.DeleteValue(ZOOM_KEY);
            _zoom = zoomLevel;

            // translation
            var transX = PhoneStateHelper.LoadValue<double>(TRANSLATION_X_KEY, 0.0);
            PhoneStateHelper.DeleteValue(TRANSLATION_X_KEY);
            _translateX = transX;
            var transY = PhoneStateHelper.LoadValue<double>(TRANSLATION_Y_KEY, 0.0);
            PhoneStateHelper.DeleteValue(TRANSLATION_Y_KEY);
            _translateY = transY;

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

            // zoom
            PhoneStateHelper.SaveValue(ZOOM_KEY, _zoom);

            // translation
            PhoneStateHelper.SaveValue(TRANSLATION_X_KEY, _translateX);
            PhoneStateHelper.SaveValue(TRANSLATION_Y_KEY, _translateY);
            
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

                UpdateMoveButtonVisibility();
                UpdateZoomAppBarIcon();
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

            // check and adjust translation/move
            var renderedImageWidth = scale * _editImage.Width * _zoom;
            var renderedImageHeight = scale * _editImage.Height * _zoom;
            var deltaMaxX = (renderedImageWidth - GetViewportBounds().Width) / 2.0;
            var deltaMaxY = (renderedImageHeight - GetViewportBounds().Height) / 2.0;

            if (deltaMaxX > 0)
            {
                if (_translateX < -deltaMaxX)
                    _translateX = -deltaMaxX;
                else if (_translateX > deltaMaxX)
                    _translateX = deltaMaxX;
            }
            
            if (deltaMaxY > 0)
            {
                if (_translateY < -deltaMaxY)
                    _translateY = -deltaMaxY;
                else if (_translateY > deltaMaxY)
                    _translateY = deltaMaxY;
            }
            

            // scale
            EditImageControl.RenderTransform = new CompositeTransform
            {
                ScaleX = _zoom,
                ScaleY = _zoom,
                TranslateX = _translateX,
                TranslateY = _translateY
            };

            // check if upper-scaling is required
            //if (scale != neutralScaleFactor)
            //{
                InkControl.RenderTransform = new CompositeTransform
                {
                    ScaleX = scale / neutralScaleFactor * _zoom,
                    ScaleY = scale / neutralScaleFactor * _zoom,
                    TranslateX = _translateX,
                    TranslateY = _translateY
                };
            //}
            //else
            //{
            //    InkControl.RenderTransform = NEUTRAL_SCALE;
            //}

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

        private void ToggleZoom()
        {
            _zoom += 1;

            if (_zoom > ZOOM_MAX)
            {
                _zoom = 1.0;

                // reset translation
                _translateX = 0;
                _translateY = 0;
            }

            UpdateMoveButtonVisibility();
            UpdateZoomAppBarIcon();
            UpdateImageOrientationAndScale();
        }

        private void UpdateMoveButtonVisibility()
        {
            if (HasNoImage())
                return;

            var scale = GetScaleFactorOfOrientation();

            // check and adjust translation/move
            var renderedImageWidth = scale * _editImage.Width * _zoom;
            var renderedImageHeight = scale * _editImage.Height * _zoom;
            var deltaMaxX = (renderedImageWidth - GetViewportBounds().Width) / 2.0;
            var deltaMaxY = (renderedImageHeight - GetViewportBounds().Height) / 2.0;

            if (deltaMaxX > 0)
            {
                MoveLeft.Visibility = Visibility.Visible;
                MoveRight.Visibility = Visibility.Visible;
            }
            else
            {
                MoveLeft.Visibility = Visibility.Collapsed;
                MoveRight.Visibility = Visibility.Collapsed;
            }

            if (deltaMaxY > 0)
            {
                MoveUp.Visibility = Visibility.Visible;
                MoveDown.Visibility = Visibility.Visible;
            }
            else
            {
                MoveUp.Visibility = Visibility.Collapsed;
                MoveDown.Visibility = Visibility.Collapsed;
            }
        }

        private void UpdateZoomAppBarIcon()
        {
            if (_zoom == 3.0)
            {
                _appBarZoomButton.IconUri = new Uri("/Assets/AppBar/appbar.magnify3.png", UriKind.Relative);
            }
            else if (_zoom == 2.0)
            {
                _appBarZoomButton.IconUri = new Uri("/Assets/AppBar/appbar.magnify2.png", UriKind.Relative);
            }
            else
            {
                _appBarZoomButton.IconUri = new Uri("/Assets/AppBar/appbar.magnify.add.png", UriKind.Relative);
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

            // reset translate when necessary
            if (!HasNoImage())
            {
                var scale = GetScaleFactorOfOrientation();

                // check and adjust translation/move
                var renderedImageWidth = scale * _editImage.Width * _zoom;
                var renderedImageHeight = scale * _editImage.Height * _zoom;

                var viewport = GetViewportBounds();
                if (renderedImageHeight <= viewport.Height)
                {
                    _translateY = 0;
                }
                if (renderedImageWidth <= viewport.Width)
                {
                    _translateX = 0;
                }
            }

            UpdateMoveButtonVisibility();
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

            LoadColorHistory();

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
            SaveColorHistory();
        }

        private void LoadColorHistory()
        {
            var historyColors = AppSettings.ColorHistory.Value;

            // BUGSENSE: Object reference not set to an instance of an object.
            //           21.01.15
            if (historyColors == null)
            {
                // load defaults
                historyColors = AppSettings.ColorHistory.DefaultValue;
            }

            if (historyColors.Count < AppConstants.COLOR_HISTORY_SIZE)
                return; // should never occure

            ColorHistory1.Fill = new SolidColorBrush(historyColors[0]);
            ColorHistory2.Fill = new SolidColorBrush(historyColors[1]);
            ColorHistory3.Fill = new SolidColorBrush(historyColors[2]);
            ColorHistory4.Fill = new SolidColorBrush(historyColors[3]);
            ColorHistory5.Fill = new SolidColorBrush(historyColors[4]);
            ColorHistory6.Fill = new SolidColorBrush(historyColors[5]);
        }

        private void SaveColorHistory()
        {
            var historyColors = AppSettings.ColorHistory.Value;
            var currentColor = this.ColorPicker.Color;

            // add current color and clear it if already in list to be in front
            if (historyColors.Contains(currentColor))
            {
                historyColors.Remove(currentColor);
            }
            historyColors.Insert(0, currentColor);

            // ensure history size
            if (historyColors.Count > AppConstants.COLOR_HISTORY_SIZE)
            {
                historyColors.RemoveAt(historyColors.Count - 1);
            }
        }

        private void MoveLeftClicked(object sender, RoutedEventArgs e)
        {
            _translateX += MOVE_STEP * _zoom;
            UpdateImageOrientationAndScale();
        }

        private void MoveRightClicked(object sender, RoutedEventArgs e)
        {
            _translateX -= MOVE_STEP * _zoom;
            UpdateImageOrientationAndScale();
        }

        private void MoveUpClicked(object sender, RoutedEventArgs e)
        {
            _translateY += MOVE_STEP * _zoom;
            UpdateImageOrientationAndScale();
        }

        private void MoveDownClicked(object sender, RoutedEventArgs e)
        {
            _translateY -= MOVE_STEP * _zoom;
            UpdateImageOrientationAndScale();
        }

        private void HistoryColorSelected(object sender, System.Windows.Input.GestureEventArgs e)
        {
            System.Windows.Shapes.Rectangle rect = sender as System.Windows.Shapes.Rectangle;

            if (rect != null)
            {
                SolidColorBrush solidColor = rect.Fill as SolidColorBrush;

                if (solidColor != null) {
                    ColorPicker.Color = solidColor.Color;
                }
            }
        }
    }
}