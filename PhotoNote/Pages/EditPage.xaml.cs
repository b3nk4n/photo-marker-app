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
using Microsoft.Xna.Framework;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

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
        private const string CENTER_START_KEY = "_center_start_";

        private const string ZOOM_KEY = "_zoom_";
        private const string TRANSLATION_X_KEY = "_trans_x_";
        private const string TRANSLATION_Y_KEY = "_trans_y_";

        private double _zoom = 1.0;
        private double _translateX;
        private double _translateY;

        private const double ZOOM_MAX = 3.0;
        private const double MOVE_STEP = 12.0;

        private DrawMode _drawMode = DrawMode.Normal;

        public EditPage()
        {
            InitializeComponent();
            BuildLocalizedApplicationBar();

            Loaded += (s, e) => {
                // FIXME: there might be the case that there can be crashed when accessing UI-Controls in OnNavigatedTo()
                //        these should be accessed in Loaded() event instad. See BugSense errors (e.g. in UpdateMoveButtonVisibility()).

                SetTogglesToMode(_drawMode);
            };
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
            this._drawMode = AppSettings.DrawMode.Value;
        }

        private void SaveSettings()
        {
            AppSettings.PenColor.Value = this.ColorPicker.Color;
            AppSettings.PenOpacity.Value = this.OpacitySlider.Value;
            AppSettings.PenThickness.Value = this.ThicknessSlider.Value;
            AppSettings.DrawMode.Value = this._drawMode;
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
                        if (pointPairList.Length == 2)
                        {
                            var x = Convert.ToDouble(pointPairList[0], CultureInfo.InvariantCulture);
                            var y = Convert.ToDouble(pointPairList[1], CultureInfo.InvariantCulture);

                            myStroke.StylusPoints.Add(new StylusPoint(x, y));
                        }
                    }

                    InkControl.Strokes.Add(myStroke);
                }
            }

            // center start
            var centerStart = PhoneStateHelper.LoadValue<Vector2>(CENTER_START_KEY, new Vector2());
            PhoneStateHelper.DeleteValue(CENTER_START_KEY);
            _centerStart = centerStart;
        }

        private static System.Windows.Media.Color HexToColor(string hexString)
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
                        stroke.DrawingAttributes.Height.ToString("0.00", 
                        CultureInfo.InvariantCulture),
                        stroke.DrawingAttributes.Width.ToString("0.00", CultureInfo.InvariantCulture),
                        String.Join("$", stroke.StylusPoints.Select(p => String.Format("{0}_{1}", p.X.ToString("0.00", CultureInfo.InvariantCulture), p.Y.ToString("0.00", CultureInfo.InvariantCulture))))));
                }
                PhoneStateHelper.SaveValue(PEN_DATA_KEY, strokeData.ToString());
            }

            // center start (cirlce)
            PhoneStateHelper.SaveValue(CENTER_START_KEY, _centerStart);
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

            InkControl.RenderTransform = new CompositeTransform
            {
                ScaleX = scale / neutralScaleFactor * _zoom,
                ScaleY = scale / neutralScaleFactor * _zoom,
                TranslateX = _translateX,
                TranslateY = _translateY
            };

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
        private Vector2 _centerStart;

        //A new stroke object named MyStroke is created. MyStroke is added to the StrokeCollection of the InkPresenter named MyIP
        private void MyIP_MouseLeftButtonDown(object sender, MouseEventArgs e)
        {
            InkControl.CaptureMouse();
            StylusPointCollection MyStylusPointCollection = new StylusPointCollection();
            var touchPoint = e.StylusDevice.GetStylusPoints(InkControl).First();
            //MyStylusPointCollection.Add(touchPoint);
            _centerStart = new Vector2((float)touchPoint.X, (float)touchPoint.Y);

            
        }

        //StylusPoint objects are collected from the MouseEventArgs and added to MyStroke. 
        private void MyIP_MouseMove(object sender, MouseEventArgs e)
        {
            // delayed display of first point, that the point is not visible
            // when tapping on the screen to close the toolbar
            if (_activeStroke == null)
            {
                StylusPointCollection MyStylusPointCollection = new StylusPointCollection();
                MyStylusPointCollection.Add(new StylusPoint(_centerStart.X, _centerStart.Y));
                var opacity = AppSettings.PenOpacity.Value;
                var color = AppSettings.PenColor.Value;
                var size = AppSettings.PenThickness.Value;
                _activeStroke = new Stroke(MyStylusPointCollection);
                _activeStroke.DrawingAttributes = new DrawingAttributes
                {
                    Color = System.Windows.Media.Color.FromArgb((byte)(255 * opacity), color.R, color.G, color.B),
                    Height = size,
                    Width = size,
                };
                InkControl.Strokes.Add(_activeStroke);
            }

            var stylusPoint = e.StylusDevice.GetStylusPoints(InkControl).First();

            switch (_drawMode)
            {
                case DrawMode.Normal:
                    _activeStroke.StylusPoints.Add(stylusPoint);
                    break;
                case DrawMode.Line:
                case DrawMode.Arrow:
                    if (_activeStroke.StylusPoints.Count > 1)
                    {
                        _activeStroke.StylusPoints.RemoveAt(1);
                    }
                    _activeStroke.StylusPoints.Add(stylusPoint);
                    break;
                case DrawMode.Circle:
                    while (_activeStroke.StylusPoints.Count > 0)
                    {
                        _activeStroke.StylusPoints.RemoveAt(0);
                    }
                    var touchLocation = new Vector2((float)stylusPoint.X, (float)stylusPoint.Y);
                    RenderCircle(_activeStroke, _centerStart, touchLocation);
                    break;
                case DrawMode.Rectangle:
                    while (_activeStroke.StylusPoints.Count > 1)
                    {
                        _activeStroke.StylusPoints.RemoveAt(1);
                    }
                    var startPoint = _activeStroke.StylusPoints.First();
                    _activeStroke.StylusPoints.Add(new StylusPoint(stylusPoint.X, startPoint.Y));
                    _activeStroke.StylusPoints.Add(stylusPoint);
                    _activeStroke.StylusPoints.Add(new StylusPoint(startPoint.X, stylusPoint.Y));
                    _activeStroke.StylusPoints.Add(startPoint);
                    break;
                default:
                    break;
            }  
        }

        private void RenderCircle(Stroke activeStroke, Vector2 centerStart, Vector2 touchLocation)
        {
            var radiusVec = touchLocation - centerStart;
            var radius = radiusVec.Length();
            var partsPerHalf = 10 + radius / 8;
            Debug.WriteLine(partsPerHalf);

            _activeStroke.StylusPoints.Add(new StylusPoint(touchLocation.X, touchLocation.Y));
            for (float i = 0; i <= 2 * MathHelper.Pi; i += MathHelper.Pi / partsPerHalf)
            {
                var nextLocation = centerStart + RotateVector(radiusVec, i);
                _activeStroke.StylusPoints.Add(new StylusPoint(nextLocation.X, nextLocation.Y));
            }
            _activeStroke.StylusPoints.Add(new StylusPoint(touchLocation.X, touchLocation.Y));
        }

        //MyStroke is completed
        private void MyIP_LostMouseCapture(object sender, MouseEventArgs e)
        {
            if (_isPenToolbarVisible)
            {
                // close the toolbar and do not draw anything when there was quite like a tap.
                if (_activeStroke.StylusPoints.Count < 2)
                {
                    InkControl.Strokes.Remove(_activeStroke);
                    HidePenToolbar();
                }
            }

            if (_drawMode == DrawMode.Arrow)
            {
                if (_activeStroke.StylusPoints.Count > 1)
                {
                    var start = _activeStroke.StylusPoints[0];
                    var startVec = new Vector2((float)start.X, (float)start.Y);
                    var end = _activeStroke.StylusPoints[1];
                    var endVec = new Vector2((float)end.X, (float)end.Y);

                    var arrowDirection = endVec - startVec;
                    float shoulderLength = GetShoulderLength(arrowDirection.Length(), (float)AppSettings.PenThickness.Value);
                    arrowDirection.Normalize();

                    var leftShoulder = RotateVector(arrowDirection,  3 * MathHelper.PiOver4);
                    var leftShoulderEndPoint = endVec + leftShoulder * shoulderLength;
                    var rightShoulder = RotateVector(arrowDirection, 5 * MathHelper.PiOver4);
                    var rightShoulderEndPoint = endVec + rightShoulder * shoulderLength;

                    _activeStroke.StylusPoints.Add(new StylusPoint(leftShoulderEndPoint.X, leftShoulderEndPoint.Y));
                    _activeStroke.StylusPoints.Add(end);
                    _activeStroke.StylusPoints.Add(new StylusPoint(rightShoulderEndPoint.X, rightShoulderEndPoint.Y));
                }
                else
                {
                    // remove, because an arrow direction could not be determined
                    InkControl.Strokes.Remove(_activeStroke);
                }
            }

            _activeStroke = null;
        }

        private float GetShoulderLength(float length, float thickness)
        {
            float result = 50;
            result = Math.Min(length / 2, result);
            result = Math.Min(thickness * 4, result);
            return result;
        }

        private Vector2 RotateVector(Vector2 vec, float radians)
        {
            var transformed = Vector2.Transform(vec, Microsoft.Xna.Framework.Matrix.CreateRotationZ(radians));
            return transformed;
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

        private void PenModeToggled(object sender, RoutedEventArgs e)
        {
            var toggle = sender as ToggleButton;

            if (toggle != null)
            {
                var toggledMode = (DrawMode)Enum.Parse(typeof(DrawMode), (string)toggle.Tag);
                SetTogglesToMode(toggledMode);
            }
        }

        private void SetTogglesToMode(DrawMode mode)
        {
            // unregister events
            NormalPen.Checked -= PenModeToggled;
            ArrowPen.Checked -= PenModeToggled;
            LinePen.Checked -= PenModeToggled;
            CirclePen.Checked -= PenModeToggled;
            RectanglePen.Checked -= PenModeToggled;
            NormalPen.Unchecked -= PenModeToggled;
            ArrowPen.Unchecked -= PenModeToggled;
            LinePen.Unchecked -= PenModeToggled;
            CirclePen.Unchecked -= PenModeToggled;
            RectanglePen.Unchecked -= PenModeToggled;

            // set mode
            _drawMode = mode;

            // update UI
            switch (mode)
            {
                case DrawMode.Normal:
                    if (!NormalPen.IsChecked.Value)
                    {
                        NormalPen.IsChecked = true;
                    }
                    ArrowPen.IsChecked = false;
                    LinePen.IsChecked = false;
                    CirclePen.IsChecked = false;
                    RectanglePen.IsChecked = false;
                    break;
                case DrawMode.Line:
                    if (!LinePen.IsChecked.Value)
                    {
                        LinePen.IsChecked = true;
                    }
                    NormalPen.IsChecked = false;
                    ArrowPen.IsChecked = false;
                    CirclePen.IsChecked = false;
                    RectanglePen.IsChecked = false;
                    break;
                case DrawMode.Arrow:
                    if (!ArrowPen.IsChecked.Value)
                    {
                        ArrowPen.IsChecked = true;
                    }
                    NormalPen.IsChecked = false;
                    LinePen.IsChecked = false;
                    CirclePen.IsChecked = false;
                    RectanglePen.IsChecked = false;
                    break;
                case DrawMode.Circle:
                    if (!CirclePen.IsChecked.Value)
                    {
                        CirclePen.IsChecked = true;
                    }
                    NormalPen.IsChecked = false;
                    ArrowPen.IsChecked = false;
                    LinePen.IsChecked = false;
                    RectanglePen.IsChecked = false;
                    break;
                case DrawMode.Rectangle:
                    if (!RectanglePen.IsChecked.Value)
                    {
                        RectanglePen.IsChecked = true;
                    }
                    NormalPen.IsChecked = false;
                    ArrowPen.IsChecked = false;
                    LinePen.IsChecked = false;
                    CirclePen.IsChecked = false;
                    break;
            }

            // reregister events
            NormalPen.Checked += PenModeToggled;
            ArrowPen.Checked += PenModeToggled;
            LinePen.Checked += PenModeToggled;
            CirclePen.Checked += PenModeToggled;
            RectanglePen.Checked += PenModeToggled;
            NormalPen.Unchecked += PenModeToggled;
            ArrowPen.Unchecked += PenModeToggled;
            LinePen.Unchecked += PenModeToggled;
            CirclePen.Unchecked += PenModeToggled;
            RectanglePen.Unchecked += PenModeToggled;
        }
    }
}