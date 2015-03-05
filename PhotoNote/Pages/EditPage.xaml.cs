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
using System.Collections.Generic;
using PhoneKit.Framework.Core.Storage;
using PhotoNote.Conversion;
using PhotoNote.ViewModel;
using PhoneKit.Framework.InAppPurchase;

namespace PhotoNote.Pages
{
    public partial class EditPage : PhoneApplicationPage
    {
        private EditPicture _editImage;

        ApplicationBarIconButton _appBarZoomButton;

        ApplicationBarIconButton _appBarPenButton;

        ApplicationBarIconButton _appBarTextButton;

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

        private const double ZOOM_MIN = 1.0;
        private const double ZOOM_MAX = 5.0;

        private DrawMode _currentDrawMode = DrawMode.Normal;

        private EditMode _currentEditMode = EditMode.Marker;

        private static StoredObject<bool> ZoomingInfoShow = new StoredObject<bool>("_zoomingInfo_", false);

        private static LinearToQuadraticConverter LinerToQuadraticConverter = new LinearToQuadraticConverter();

        private TextContext _textContext = new TextContext();

        public EditPage()
        {
            InitializeComponent();
            BuildLocalizedApplicationBar();

            Loaded += (s, e) => {
                SetTogglesToMode(_currentDrawMode);
                UpdateTextToolbarWithContext(_textContext);
                UpdateTextAppBar();
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
            _appBarPenButton = new ApplicationBarIconButton(new Uri("/Assets/AppBar/appbar.draw.marker.Normal.png", UriKind.Relative));
            _appBarPenButton.Text = AppResources.AppBarPen;
            _appBarPenButton.Click += (s, e) =>
            {
                if (_currentEditMode == EditMode.Text)
                {
                    ChangedEditMode(EditMode.Marker);
                    UpdateMarkerAppBar();
                    UpdateTextAppBar();

                    if (_isPenToolbarVisible)
                    {
                        // updates the view state
                        ShowPenToolbar();
                    }
                }
                else
                {
                    if (_isPenToolbarVisible)
                    {
                        HidePenToolbar();
                    }
                    else
                    {
                        ShowPenToolbar();
                    }
                }
            };
            ApplicationBar.Buttons.Add(_appBarPenButton);

            // text
            _appBarTextButton = new ApplicationBarIconButton(new Uri("/Assets/AppBar/appbar.interface.textbox.png", UriKind.Relative));
            _appBarTextButton.Text = "text"; // TODO: translate
            _appBarTextButton.Click += (s, e) =>
            {
                if (_currentEditMode == EditMode.Marker)
                {
                    ChangedEditMode(EditMode.Text);
                    UpdateMarkerAppBar();
                    UpdateTextAppBar();

                    if (_isPenToolbarVisible)
                    {
                        // updates the view state
                        ShowPenToolbar();
                    }
                }
                else
                {
                    var keyboardClosed = false;
                    if (_selectedTextBox != null)
                    {
                        // close the keyboard
                        this.Focus();
                        keyboardClosed = true;
                    }
                    
                    if (!_isPenToolbarVisible || keyboardClosed)
                    {
                        ShowPenToolbar();
                    }
                    else
                    {
                        HidePenToolbar();
                    }
                }
            };
            ApplicationBar.Buttons.Add(_appBarTextButton);

            // zoom
            _appBarZoomButton = new ApplicationBarIconButton(new Uri("/Assets/AppBar/appbar.magnify1.png", UriKind.Relative));
            _appBarZoomButton.Text = AppResources.AppBarZoom;
            _appBarZoomButton.Click += (s, e) =>
            {
                ToggleZoom();

                if (!ZoomingInfoShow.Value)
                {
                    ShowZoomingPopup.Begin();
                    ZoomingInfoShow.Value = true;
                }
            };
            ApplicationBar.Buttons.Add(_appBarZoomButton);

            // save
            ApplicationBarMenuItem appBarSaveMenuItem = new ApplicationBarMenuItem(AppResources.AppBarSave);
            appBarSaveMenuItem.Click += async (s, e) =>
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
            ApplicationBar.MenuItems.Add(appBarSaveMenuItem);

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
                    var textContextList = GetTextBoxContextList();
                    var editedImageInkControl = new EditedImageInkControl(_editImage.FullImage as BitmapSource, InkControl.Strokes, textContextList, 1.0 / neutralScaleFactor);
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

        /// <summary>
        /// Gets the context list of the text elements on screen.
        /// </summary>
        /// <returns>The list of text contexts.</returns>
        private IList<TextBoxContext> GetTextBoxContextList()
        {
            IList<TextBoxContext> list = new List<TextBoxContext>();
            foreach (var tb in EditTextControl.Children)
            {
                var textbox = tb as ExtendedTextBox;
                if (textbox != null)
                {
                    list.Add(textbox.GetTextBoxContext());
                }
            }
            return list;
        }

        private bool returnedFromTombstone = true; // flag to determine a tombstone

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            if (e.NavigationMode == NavigationMode.Back &&
                    !e.IsNavigationInitiator &&
                returnedFromTombstone) // to ensure this is only called after tombstone
                RestoreState();

            // load settings
            LoadSettings();
            LoadColorHistory();

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
            }

            returnedFromTombstone = false;
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);

            // save state
            if (!e.IsNavigationInitiator)
            {
                SaveState();
            }

            SaveSettings();
            SaveColorHistory();
        }

        private void LoadSettings()
        {
            this.ColorPicker.Color = AppSettings.PenColor.Value;
            this.OpacitySlider.Value = AppSettings.PenOpacity.Value;
            var thicknessQuadratic = AppSettings.PenThickness.Value;
            this.ThicknessSlider.Value = (double)LinerToQuadraticConverter.ConvertBack(thicknessQuadratic, null, null, null);
            _currentDrawMode = AppSettings.DrawMode.Value;
        }

        private void SaveSettings()
        {
            AppSettings.PenColor.Value = this.ColorPicker.Color;
            AppSettings.PenOpacity.Value = this.OpacitySlider.Value;
            AppSettings.PenThickness.Value = (double)LinerToQuadraticConverter.Convert(this.ThicknessSlider.Value, null, null, null);
            AppSettings.DrawMode.Value = this._currentDrawMode;
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
            UpdateZoomAppBarIcon();
            UpdateImageOrientationAndScale();
            EditImageControl.Source = _editImage.FullImage;
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

            // text control
            EditTextControl.Width = neutralScaleFactor * _editImage.Width;
            EditTextControl.Height = neutralScaleFactor * _editImage.Height;

            // check and adjust translation/move
            var renderedImageWidth = scale * _editImage.Width * _zoom;
            var renderedImageHeight = scale * _editImage.Height * _zoom;
            var deltaMaxX = Math.Abs((renderedImageWidth - GetViewportBounds().Width) / 2.0);
            var deltaMaxY = Math.Abs((renderedImageHeight - GetViewportBounds().Height) / 2.0);

            if (_translateX < -deltaMaxX)
                _translateX = -deltaMaxX;
            else if (_translateX > deltaMaxX)
                _translateX = deltaMaxX;
            
            if (_translateY < -deltaMaxY)
                _translateY = -deltaMaxY;
            else if (_translateY > deltaMaxY)
                _translateY = deltaMaxY;

            // scale
            var imageTransform = EditImageControl.RenderTransform as CompositeTransform;
            imageTransform.ScaleX = _zoom;
            imageTransform.ScaleY = _zoom;
            imageTransform.TranslateX = _translateX;
            imageTransform.TranslateY = _translateY;

            var inkTransform = InkControl.RenderTransform as CompositeTransform;
            inkTransform.ScaleX = scale / neutralScaleFactor * _zoom;
            inkTransform.ScaleY = scale / neutralScaleFactor * _zoom;
            inkTransform.TranslateX = _translateX;
            inkTransform.TranslateY = _translateY;

            var textTransform = EditTextControl.RenderTransform as CompositeTransform;
            textTransform.ScaleX = scale / neutralScaleFactor * _zoom;
            textTransform.ScaleY = scale / neutralScaleFactor * _zoom;
            textTransform.TranslateX = _translateX;
            textTransform.TranslateY = _translateY;

            // set clipping region
            SetBoundary(InkControl, InkControl.Width, InkControl.Height);
            SetBoundary(EditTextControl, EditTextControl.Width, EditTextControl.Height);
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

            UpdateImageOrientationAndScale();
        }

        #endregion

        private void ClosedPenToobarTapped(object sender, System.Windows.Input.GestureEventArgs e)
        {
            HidePenToolbar();
        }

        protected async override void OnBackKeyPress(System.ComponentModel.CancelEventArgs e)
        {
            if (_isPenToolbarVisible)
            {
                e.Cancel = true;
                HidePenToolbar();
            }
            else if (_selectedTextBox != null)
            {
                UnselectTextBox(ref _selectedTextBox);
                e.Cancel = true;
            }
            else if (InkControl.Strokes.Count > 0 ||
                EditTextControl.Children.Count > 0)
            {
                e.Cancel = true;
                await Task.Delay(100); // fixes the "kill app by OS" issue
                if (MessageBox.Show(AppResources.MessageBoxExitWithoutSave, AppResources.MessageBoxAttention, MessageBoxButton.OKCancel) == MessageBoxResult.OK)
                {
                    if (NavigationService.CanGoBack)
                        NavigationService.GoBack();
                    else
                        Application.Current.Terminate();
                }
            }

            base.OnBackKeyPress(e);
        }

        public void ShowPenToolbar(bool useTransition=true)
        {
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

            string visualState;
            if (_currentEditMode == EditMode.Marker)
                visualState = "DisplayedPenMode";
            else
                visualState = "DisplayedTextMode";

            VisualStateManager.GoToState(this, visualState, useTransition);
            _isPenToolbarVisible = true;
        }

        public void HidePenToolbar()
        {
            if (!_isPenToolbarVisible)
                return;

            VisualStateManager.GoToState(this, "Normal", true);
            _isPenToolbarVisible = false;

            //SaveSettings();
            //SaveColorHistory(); // TODO: remove ok?
        }

        private List<System.Windows.Media.Color> _colorHistory;

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
            _colorHistory = historyColors;
            UpdateColorHistoryUI();
        }

        private void UpdateColorHistoryUI()
        {
            var historyColors = _colorHistory;

            if (historyColors == null)
            {
                // load defaults
                historyColors = AppSettings.ColorHistory.DefaultValue;
            }

            ColorHistory1.Fill = new SolidColorBrush(historyColors[0]);
            ColorHistory2.Fill = new SolidColorBrush(historyColors[1]);
            ColorHistory3.Fill = new SolidColorBrush(historyColors[2]);
            ColorHistory4.Fill = new SolidColorBrush(historyColors[3]);
            ColorHistory5.Fill = new SolidColorBrush(historyColors[4]);
            ColorHistory6.Fill = new SolidColorBrush(historyColors[5]);
        }

        private void UpdateColorHistory()
        {
            var currentColor = this.ColorPicker.Color;

            // add current color and clear it if already in list to be in front
            if (_colorHistory[0] == currentColor)
            {
                return;
            }
            else if (_colorHistory.Contains(currentColor))
            {
                _colorHistory.Remove(currentColor);
            }
            _colorHistory.Insert(0, currentColor);

            // ensure history size
            if (_colorHistory.Count > AppConstants.COLOR_HISTORY_SIZE)
            {
                _colorHistory.RemoveAt(_colorHistory.Count - 1);
            }
        }

        private void SaveColorHistory()
        {
            AppSettings.ColorHistory.Value = _colorHistory;
        }

        private void HistoryColorSelected(object sender, System.Windows.Input.GestureEventArgs e)
        {
            System.Windows.Shapes.Rectangle rect = sender as System.Windows.Shapes.Rectangle;

            if (rect != null)
            {
                SolidColorBrush solidColor = rect.Fill as SolidColorBrush;

                if (solidColor != null) {
                    ColorPicker.Color = solidColor.Color;

                    // manually call the changed event
                    ColorPickerChanged(null, solidColor.Color);
                }
            }
        }

        private void PenModeToggled(object sender, RoutedEventArgs e)
        {
            var toggle = sender as ToggleButton;

            if (toggle != null)
            {
                var toggledMode = (DrawMode)Enum.Parse(typeof(DrawMode), (string)toggle.Tag);
                // set mode
                _currentDrawMode = toggledMode;
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

            UpdateMarkerAppBar();

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

        #region  INK REGION

        private Stroke _activeStroke;
        private Vector2 _centerStart;
        private int _moveCounter;

        /// <summary>
        /// Used to make sure no stroke is added multiple times. Not really necessary anymore, but makes sure
        /// the performance will not be bad in the move-event.
        /// </summary>
        private bool strokeAdded;

        

        //A new stroke object named MyStroke is created. MyStroke is added to the StrokeCollection of the InkPresenter named MyIP
        private void MyIP_MouseLeftButtonDown(object sender, MouseEventArgs e)
        {
            InkControl.CaptureMouse();
            StylusPointCollection MyStylusPointCollection = new StylusPointCollection();
            var touchPoint = e.StylusDevice.GetStylusPoints(InkControl).First();
            _centerStart = new Vector2((float)touchPoint.X, (float)touchPoint.Y);
        }

        //StylusPoint objects are collected from the MouseEventArgs and added to MyStroke. 
        private void MyIP_MouseMove(object sender, MouseEventArgs e)
        {
            if (_twoFingersActive || _currentEditMode == EditMode.Text)
                return;

            _moveCounter++;

            // delayed display of first point, that the point is not visible
            // when tapping on the screen to close the toolbar
            if (_activeStroke == null)
            {
                _activeStroke = StartStroke();
            } else if (!strokeAdded)
            {
                CheckedAddStroke(_activeStroke);
                strokeAdded = true;
            }

            var stylusPoint = e.StylusDevice.GetStylusPoints(InkControl).First();

            switch (_currentDrawMode)
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

        //MyStroke is completed
        private void MyIP_LostMouseCapture(object sender, MouseEventArgs e)
        {
            if (_currentEditMode != EditMode.Text)
            {

                if (_isPenToolbarVisible)
                {
                    // close the toolbar and do not draw anything when there was quite like a tap.
                    if (_activeStroke == null || _activeStroke.StylusPoints.Count < 2)
                    {
                        if (_activeStroke != null)
                            InkControl.Strokes.Remove(_activeStroke);
                        HidePenToolbar();
                        _activeStroke = null;
                        strokeAdded = false;
                        return;
                    }
                }

                // delayed display of first point, that the point is not visible
                // when tapping on the screen to close the toolbar
                if (_activeStroke == null)
                {
                    _activeStroke = StartStroke();

                    if (!_twoFingersActive)
                    {
                        if (!strokeAdded)
                        {
                            CheckedAddStroke(_activeStroke);
                            strokeAdded = true;
                        }
                    }

                }
                else if (!strokeAdded)
                {
                    CheckedAddStroke(_activeStroke);
                    strokeAdded = true;
                }

                if (_currentDrawMode == DrawMode.Arrow)
                {
                    FinishArrow();
                }

                // remove ink segments when two fingers have been detected shortly after the drawing has begun
                if (_twoFingersActive && _moveCounter < 3)
                {
                    InkControl.Strokes.Remove(_activeStroke);
                }
            }

            // reset current context
            _activeStroke = null;
            strokeAdded = false;
            _twoFingersActive = false;
            _moveCounter = 0;
        }

        /// <summary>
        /// To make sure no to get an "Element is already the child of another element." error.
        /// </summary>
        /// <param name="stroke">The stroke to add only once.</param>
        private void CheckedAddStroke(Stroke stroke)
        {
            if (!InkControl.Strokes.Contains(stroke))
                InkControl.Strokes.Add(stroke);
        }

        private Stroke StartStroke()
        {
            StylusPointCollection MyStylusPointCollection = new StylusPointCollection();
            MyStylusPointCollection.Add(new StylusPoint(_centerStart.X, _centerStart.Y));
            var opacity = OpacitySlider.Value;
            var color = ColorPicker.Color;
            var size = (double)LinerToQuadraticConverter.Convert(this.ThicknessSlider.Value, null, null, null);
            var stroke = new Stroke(MyStylusPointCollection);
            stroke.DrawingAttributes = new DrawingAttributes
            {
                Color = System.Windows.Media.Color.FromArgb((byte)(255 * opacity), color.R, color.G, color.B),
                Height = size,
                Width = size,
            };
            UpdateColorHistory();
            UpdateColorHistoryUI();
            return stroke;
        }

        private void RenderCircle(Stroke activeStroke, Vector2 centerStart, Vector2 touchLocation)
        {
            var radiusVec = touchLocation - centerStart;
            var radius = radiusVec.Length();
            var partsPerHalf = 10 + radius / 8;

            _activeStroke.StylusPoints.Add(new StylusPoint(touchLocation.X, touchLocation.Y));
            for (float i = 0; i <= 2 * MathHelper.Pi; i += MathHelper.Pi / partsPerHalf)
            {
                var nextLocation = centerStart + RotateVector(radiusVec, i);
                _activeStroke.StylusPoints.Add(new StylusPoint(nextLocation.X, nextLocation.Y));
            }
            _activeStroke.StylusPoints.Add(new StylusPoint(touchLocation.X, touchLocation.Y));
        }

        private void FinishArrow()
        {
            if (_activeStroke.StylusPoints.Count > 1)
            {
                var start = _activeStroke.StylusPoints[0];
                var startVec = new Vector2((float)start.X, (float)start.Y);
                var end = _activeStroke.StylusPoints[1];
                var endVec = new Vector2((float)end.X, (float)end.Y);

                var arrowDirection = endVec - startVec;
                float shoulderLength = GetShoulderLength(arrowDirection.Length(), (float)((double)LinerToQuadraticConverter.Convert(this.ThicknessSlider.Value, null, null, null)));
                arrowDirection.Normalize();

                var leftShoulder = RotateVector(arrowDirection, 3 * MathHelper.PiOver4);
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
        private void SetBoundary(FrameworkElement control, double width, double height)
        {
            RectangleGeometry MyRectangleGeometry = new RectangleGeometry();
            MyRectangleGeometry.Rect = new Rect(0, 0, width, height);
            control.Clip = MyRectangleGeometry;
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
            _zoom = Math.Round(_zoom + 1);

            if (_zoom > ZOOM_MAX)
            {
                _zoom = ZOOM_MIN;

                // reset translation
                _translateX = 0;
                _translateY = 0;
            }

            UpdateZoomAppBarIcon();
            UpdateImageOrientationAndScale();
        }

        /// <summary>
        /// Remember the current zoom icon value to update the icon only when there is a change.
        /// </summary>
        private int currentZoomIcon = 1;

        private void UpdateZoomAppBarIcon()
        {
            if (_appBarZoomButton == null)
                return;

            int zoomValue = (int)Math.Round(_zoom);

            zoomValue = (int)CheckZoomInBounds(zoomValue);

            if (currentZoomIcon == zoomValue)
                return;

            currentZoomIcon = zoomValue;

            var uriString = string.Format("/Assets/AppBar/appbar.magnify{0}.png", zoomValue);
            _appBarZoomButton.IconUri = new Uri(uriString, UriKind.Relative);
        }

        /// <summary>
        /// Update marker application bar icon.
        /// </summary>
        private void UpdateMarkerAppBar()
        {
            var active = (_currentEditMode == EditMode.Marker) ? ".active" : string.Empty;
            var iconUriString = string.Format("/Assets/AppBar/appbar.draw.marker.{0}{1}.png", _currentDrawMode.ToString(), active);
            _appBarPenButton.IconUri = new Uri(iconUriString, UriKind.Relative);
        }

        /// <summary>
        /// Update text application bar icon.
        /// </summary>
        private void UpdateTextAppBar()
        {
            var active = (_currentEditMode == EditMode.Text) ? ".active" : string.Empty;
            var iconUriString = string.Format("/Assets/AppBar/appbar.interface.textbox{0}.png", active);
            _appBarTextButton.IconUri = new Uri(iconUriString, UriKind.Relative);
        }

        #endregion

        #region PinchToZoom/Panning

        private double translationDeltaX;
        private double translationDeltaY;
        private double zoomBaseline;

        private bool _twoFingersActive;

        public const int TEXT_SELECTION_MARGIN = 5;

        private ExtendedTextBox _selectedTextBox = null;
        private ExtendedTextBox _previouslySelectedTextBox = null;
        private bool _isKeyboardActive = false;


        private void MyIP_ManipulationStarted(object sender, ManipulationStartedEventArgs e)
        {
            // init start of manipulation
            translationDeltaX = double.MinValue;
            translationDeltaY = double.MinValue;
            zoomBaseline = _zoom;

            if (_currentEditMode == EditMode.Text)
            {
                // init start of manipulation
                translationDeltaX = double.MinValue;
                translationDeltaY = double.MinValue;
                zoomBaseline = _zoom;

                _previouslySelectedTextBox = _selectedTextBox;
                _selectedTextBox = null;

                for (int i = EditTextControl.Children.Count - 1; i >= 0; --i)
                {
                    var textbox = EditTextControl.Children[i] as ExtendedTextBox;
                    if (textbox != null)
                    {
                        var boundingBox = new Rectangle((int)Canvas.GetLeft(textbox) - TEXT_SELECTION_MARGIN, (int)Canvas.GetTop(textbox) - TEXT_SELECTION_MARGIN,
                                                        (int)textbox.ActualWidth + 2 * TEXT_SELECTION_MARGIN, (int)textbox.ActualHeight + 2 * TEXT_SELECTION_MARGIN);

                        if (boundingBox.Contains((int)e.ManipulationOrigin.X, (int)e.ManipulationOrigin.Y))
                        {
                            SelectTextBox(textbox);
                            
                            // update UI in toolbar
                            UpdateTextToolbarWithContext(textbox.GetContext());
                            break;
                        }
                    }
                }

                if (_previouslySelectedTextBox != _selectedTextBox)
                    UnselectTextBox(ref _previouslySelectedTextBox);

                // reset previous selection when nothing was clicked
                if (_selectedTextBox == null)
                {
                    _previouslySelectedTextBox = null;
                }
            }
        }

        private void MyIP_ManipulationDelta(object sender, ManipulationDeltaEventArgs e)
        {
            // first make sure we re using 2 fingers
            if (e.PinchManipulation != null)
            {
                _twoFingersActive = true;

                InkPresenter photo = sender as InkPresenter;
                CompositeTransform transform = photo.RenderTransform as CompositeTransform;

                if (translationDeltaX == double.MinValue || translationDeltaY == double.MinValue)
                {
                    translationDeltaX = e.ManipulationOrigin.X;
                    translationDeltaY = e.ManipulationOrigin.Y;
                }

                double dx = translationDeltaX - e.ManipulationOrigin.X;
                double dy = translationDeltaY - e.ManipulationOrigin.Y;

                _translateX = ExponentialFilter(_translateX, _translateX - dx * _zoom, 0.33);
                _translateY = ExponentialFilter(_translateY, _translateY - dy * _zoom, 0.33);


                double zoomDeltaFactor = (e.PinchManipulation.CumulativeScale - 1);
                if (zoomDeltaFactor < 0)
                {
                    zoomDeltaFactor *= _zoom;
                }

                _zoom = ExponentialFilter(_zoom, zoomBaseline + zoomDeltaFactor, 0.33);

                _zoom = CheckZoomInBounds(_zoom);

                UpdateImageOrientationAndScale();
                UpdateZoomAppBarIcon();
            }
            else if (_selectedTextBox != null)
            {
                // remove textbox when moved out of image
                if (!_selectedTextBox.SetTextBoxPosition(EditTextControl, e.ManipulationOrigin.X, e.ManipulationOrigin.Y))
                {
                    RemoveTextBox(EditTextControl, ref _selectedTextBox);
                }
            }

            if (_currentEditMode == EditMode.Text)
            {
                _moveCounter++;
            }
        }

        private bool _upgradePopupShown;

        private void MyIP_ManipulationCompleted(object sender, ManipulationCompletedEventArgs e)
        {
            if (_currentEditMode == EditMode.Text && !_twoFingersActive)
            {
                if (_selectedTextBox != null)
                {
                    _selectedTextBox.IsEnabled = true;

                    if (_previouslySelectedTextBox == _selectedTextBox && _moveCounter == 0)
                    {
                        EditTextBox(_selectedTextBox);
                    }
                }
                else if (_previouslySelectedTextBox == null && _moveCounter <= 1 && !_isKeyboardActive)
                {
                    if (EditTextControl.Children.Count == 0 || InAppPurchaseHelper.IsProductActive(AppConstants.IAP_PREMIUM_VERSION))
                    {
                        AddTextBox(EditTextControl, string.Empty, e.ManipulationOrigin.X, e.ManipulationOrigin.Y);
                    }
                    else if (!_upgradePopupShown) // show upgrade message only once (not req. to make it persistent)
                    {
                        _upgradePopupShown = true;

                        // ask to buy the premium version to add multiple text elements
                        if (MessageBox.Show(AppResources.MessageBoxMultipleTextUpgrade, AppResources.MessageBoxAttention, MessageBoxButton.OKCancel) == MessageBoxResult.OK)
                        {
                            NavigationService.Navigate(new Uri("/Pages/InAppStorePage.xaml", UriKind.Relative));
                        }
                    }

                    
                } 
            }
        }

        /// <summary>
        /// Checks whether the zoom is in bounds.
        /// </summary>
        /// <param name="zoom">The zoom value</param>
        /// <returns>The adjusted value.</returns>
        private double CheckZoomInBounds(double zoom)
        {
            if (zoom < ZOOM_MIN)
                zoom = ZOOM_MIN;
            else if (zoom > ZOOM_MAX)
                zoom = ZOOM_MAX;

            return zoom;
        }

        private double ExponentialFilter(double lastValue, double newValue, double alpha)
        {
            return lastValue * (1 - alpha) + newValue * alpha;
        }

        /// <summary>
        /// Changes the edit mode
        /// </summary>
        /// <param name="newEditMode"></param>
        private void ChangedEditMode(EditMode newEditMode)
        {
            _currentEditMode = newEditMode;

            if (newEditMode == EditMode.Text)
            {
                AllTextBoxesToActiveState(false);
            }
            else
            {
                AllTextBoxesToActiveState(true);
                UnselectTextBox(ref _selectedTextBox);
            }
        }

        /// <summary>
        /// Add a text with current text properties.
        /// </summary>
        /// <param name="parent">The parent canvas container.</param>
        /// <param name="text">The default text.</param>
        /// <param name="x">The x coord.</param>
        /// <param name="y">THe y coord.</param>
        private void AddTextBox(Canvas parent, string text, double x, double y)
        {
            var textbox = new ExtendedTextBox();
            textbox.Text = text;
            textbox.Foreground = ColorPicker.SolidColorBrush; // TODO: define a common context?
            textbox.SetContext(_textContext);
            textbox.IsActive = true;
            textbox.LostFocus += (s, e) =>
            {
                var thisTextBox = s as ExtendedTextBox;
                if (thisTextBox != null && string.IsNullOrWhiteSpace(thisTextBox.Text))
                {
                    RemoveTextBox(parent, ref thisTextBox);
                }
                //_selectedTextBox = null;
                _isKeyboardActive = false;
            };
            textbox.GotFocus += (s, e) =>
            {
                _isKeyboardActive = true;
            };
            textbox.IsEnabledChanged += (s, e) =>
            {
                if ((bool)e.NewValue)
                {
                    // show text options
                    ShowTextOptionsAnimation.Begin();
                }
                else
                {
                    // hide text options
                    HideTextOptionsAnimation.Begin();
                }
            };
            // use out of screen location to get the actual width and height
            //Canvas.SetTop(textbox, -999);
            //Canvas.SetLeft(textbox, -999); // TODO: 999 necessary?
            parent.Children.Add(textbox);
            textbox.UpdateLayout();

            textbox.SetTextBoxPosition(parent, x, y);

            // select
            SelectTextBox(textbox);
            EditTextBox(_selectedTextBox);

            // show text options
            ShowTextOptionsAnimation.Begin();
        }

        #endregion

        private void TextOptionsEditClicked(object sender, RoutedEventArgs e)
        {
            EditTextBox(_selectedTextBox);
        }

        private void TextOptionsDeleteClicked(object sender, RoutedEventArgs e)
        {
            if (_selectedTextBox != null)
            {
                RemoveTextBox(EditTextControl, ref _selectedTextBox);
            }
        }

        /// <summary>
        /// Unsets the focus and removes the text box.
        /// </summary>
        /// <param name="parent">The container panel.</param>
        /// <param name="textBox">The text box to remove.</param>
        private static void RemoveTextBox(Panel parent, ref ExtendedTextBox textBox)
        {
            parent.Children.Remove(textBox);
            UnselectTextBox(ref textBox);
            
        }

        private void SelectTextBox(ExtendedTextBox textBox)
        {
            if (textBox != null)
            {
                _selectedTextBox = textBox;
                _selectedTextBox.IsActive = false;
            }
        }

        /// <summary>
        /// Sets the read only state of all text boxes, where readOnly means
        /// if a text box is highlighted or not.
        /// </summary>
        /// <param name="readOnly"></param>
        private void AllTextBoxesToActiveState(bool readOnly)
        {
            foreach (var tb in EditTextControl.Children)
            {
                var textbox = tb as ExtendedTextBox;
                if (textbox != null)
                {
                    textbox.IsActive = readOnly;
                }
            }
        }

        /// <summary>
        /// Unselects the active text box.
        /// </summary>
        private static void UnselectTextBox(ref ExtendedTextBox textBox)
        {
            if (textBox != null)
            {
                textBox.IsEnabled = false;
                textBox = null;
            }
        }

        /// <summary>
        /// Edits the text box.
        /// </summary>
        private static void EditTextBox(ExtendedTextBox textBox)
        {
            if (textBox != null)
            {
                textBox.IsEnabled = true;
                textBox.Focus();
                textBox.SelectLast();
            }
        }

        /// <summary>
        /// Updates the toolbar to the given context.
        /// </summary>
        /// <param name="context">The context to set.</param>
        private void UpdateTextToolbarWithContext(TextContext context)
        {
            SetTogglesToTextAlignment(context.Alignment);
            SetTogglesToTextWeight(context.Weight);
            SetTogglesToTextStyle(context.Style);
            TextOpacitySlider.Value = context.Opacity;
            TextSizeSlider.Value = context.Size;
            SetTogglesToTextBorder(context.HasBorder);
            SetTogglesToTextBackgroundBorder(context.HasBackgroundBorder);
            SetSelectionTextFont(context.Font);
        }

        private void TextAlignmentToggled(object sender, RoutedEventArgs e)
        {
            var toggle = sender as ToggleButton;

            if (toggle != null)
            {
                var toggledAlignment = (TextAlignment)Enum.Parse(typeof(TextAlignment), (string)toggle.Tag);
                
                // set mode
                _textContext.Alignment = toggledAlignment;

                // change allignment when a text is selected
                if (_selectedTextBox != null)
                {
                    _selectedTextBox.TextAlignment = toggledAlignment;
                }

                // refresh UI
                SetTogglesToTextAlignment(toggledAlignment);
            }
        }

        private void SetTogglesToTextAlignment(TextAlignment alignment)
        {
            // unregister events
            AlignmentLeft.Checked -= TextAlignmentToggled;
            AlignmentCenter.Checked -= TextAlignmentToggled;
            AlignmentRight.Checked -= TextAlignmentToggled;
            AlignmentLeft.Unchecked -= TextAlignmentToggled;
            AlignmentCenter.Unchecked -= TextAlignmentToggled;
            AlignmentRight.Unchecked -= TextAlignmentToggled;

            // update UI
            switch (alignment)
            {
                case TextAlignment.Left:
                    if (!AlignmentLeft.IsChecked.Value)
                    {
                        AlignmentLeft.IsChecked = true;
                    }
                    AlignmentCenter.IsChecked = false;
                    AlignmentRight.IsChecked = false;
                    break;
                case TextAlignment.Center:
                    if (!AlignmentCenter.IsChecked.Value)
                    {
                        AlignmentCenter.IsChecked = true;
                    }
                    AlignmentLeft.IsChecked = false;
                    AlignmentRight.IsChecked = false;
                    break;
                case TextAlignment.Right:
                    if (!AlignmentRight.IsChecked.Value)
                    {
                        AlignmentRight.IsChecked = true;
                    }
                    AlignmentLeft.IsChecked = false;
                    AlignmentCenter.IsChecked = false;
                    break;
            }

            // reregister events
            AlignmentLeft.Checked += TextAlignmentToggled;
            AlignmentCenter.Checked += TextAlignmentToggled;
            AlignmentRight.Checked += TextAlignmentToggled;
            AlignmentLeft.Unchecked += TextAlignmentToggled;
            AlignmentCenter.Unchecked += TextAlignmentToggled;
            AlignmentRight.Unchecked += TextAlignmentToggled;
        }

        private void TextWeightToggled(object sender, RoutedEventArgs e)
        {
            var toggle = sender as ToggleButton;

            if (toggle != null)
            {
                if (toggle.IsChecked.Value)
                    UpdateFontWeight(FontWeights.Bold);
                else
                    UpdateFontWeight(FontWeights.Normal);
            }
        }

        private void UpdateFontWeight(FontWeight weight)
        {
            _textContext.Weight = weight;

            // change font weight when a text is selected
            if (_selectedTextBox != null)
            {
                _selectedTextBox.FontWeight = weight;
            }
        }

        private void SetTogglesToTextWeight(FontWeight weight)
        {
            // unregister events
            TextWeight.Checked -= TextWeightToggled;

            // update UI
            if (weight == FontWeights.Bold)
            {
                if (!TextWeight.IsChecked.Value)
                {
                    TextWeight.IsChecked = true;
                }
            }
            else
            {
                if (TextWeight.IsChecked.Value)
                {
                    TextWeight.IsChecked = false;
                }
            }

            // reregister events
            TextWeight.Checked += TextWeightToggled;
        }

        private void TextStyleToggled(object sender, RoutedEventArgs e)
        {
            var toggle = sender as ToggleButton;

            if (toggle != null)
            {
                if (toggle.IsChecked.Value)
                    UpdateFontStyle(FontStyles.Italic);
                else 
                    UpdateFontStyle(FontStyles.Normal);
            }
        }

        private void UpdateFontStyle(FontStyle style)
        {
            _textContext.Style = style;

            // change font style when a text is selected
            if (_selectedTextBox != null)
            {
                _selectedTextBox.FontStyle = style;
            }
        }

        private void SetTogglesToTextStyle(FontStyle style)
        {
            // unregister events
            TextStyle.Checked -= TextStyleToggled;

            // update UI
            if (style == FontStyles.Italic)
            {
                if (!TextStyle.IsChecked.Value)
                {
                    TextStyle.IsChecked = true;
                }
            }
            else
            {
                if (TextStyle.IsChecked.Value)
                {
                    TextStyle.IsChecked = false;
                }
            }

            // reregister events
            TextStyle.Checked += TextStyleToggled;
        }

        private void ColorPickerChanged(object sender, System.Windows.Media.Color color)
        {
            if (_selectedTextBox != null)
            {
                // Remark: color not used, because we do not want to create a new instance each change
                _selectedTextBox.Foreground = ColorPicker.SolidColorBrush;
            }
        }

        private void TextOpacityChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            _textContext.Opacity = e.NewValue;

            if (_selectedTextBox != null)
            {
                _selectedTextBox.TextOpacity = _textContext.Opacity;
            }
        }

        private void TextSizeChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            _textContext.Size = e.NewValue;

            if (_selectedTextBox != null)
            {
                _selectedTextBox.FontSize = _textContext.Size;
            }
        }

        private void TextBorderToggled(object sender, RoutedEventArgs e)
        {
            var toggle = sender as ToggleButton;

            if (toggle != null)
            {
                UpdateTextBorder(toggle.IsChecked.Value);
            }
        }

        private void UpdateTextBorder(bool hasBorder)
        {
            _textContext.HasBorder = hasBorder;

            // change when a text is selected
            if (_selectedTextBox != null)
            {
                _selectedTextBox.HasBorder = hasBorder;
            }
        }

        private void SetTogglesToTextBorder(bool hasBorder)
        {
            // unregister events
            TextBorder.Checked -= TextBorderToggled;

            // update UI
            TextBorder.IsChecked = hasBorder;

            // reregister events
            TextBorder.Checked += TextBorderToggled;
        }

        private void TextBackgroundBorderToggled(object sender, RoutedEventArgs e)
        {
            var toggle = sender as ToggleButton;

            if (toggle != null)
            {
                UpdateTextBackgroundBorder(toggle.IsChecked.Value);
            }
        }

        private void UpdateTextBackgroundBorder(bool hasBackgroundBorder)
        {
            _textContext.HasBackgroundBorder = hasBackgroundBorder;

            // change when a text is selected
            if (_selectedTextBox != null)
            {
                _selectedTextBox.HasBackgroundBorder = hasBackgroundBorder;
            }
        }

        private void SetTogglesToTextBackgroundBorder(bool hasBackgroundBorder)
        {
            // unregister events
            TextBackgroundBorder.Checked -= TextBackgroundBorderToggled;

            // update UI
            TextBackgroundBorder.IsChecked = hasBackgroundBorder;

            // reregister events
            TextBackgroundBorder.Checked += TextBackgroundBorderToggled;
        }

        private void FontPickerSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var picker = sender as ListPicker;
            if (picker != null)
            {
                var selectedFontItem = picker.SelectedItem as FontItemViewModel;

                if (selectedFontItem != null)
                    UpdateTextFont(selectedFontItem.Font);
            }
        }

        private void UpdateTextFont(FontFamily font)
        {
            _textContext.Font = font;

            // change when a text is selected
            if (_selectedTextBox != null)
            {
                _selectedTextBox.FontFamily = _textContext.Font;
            }
        }

        private void SetSelectionTextFont(FontFamily font)
        {
            // unregister events
            FontPicker.SelectionChanged -= FontPickerSelectionChanged;

            // update UI
            FontPicker.SelectedItem = FontsViewModel.GetItemByFont(font);

            // reregister events
            FontPicker.SelectionChanged += FontPickerSelectionChanged;
        }
    }
}