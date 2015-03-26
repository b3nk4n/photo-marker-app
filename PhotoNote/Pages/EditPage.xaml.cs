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
using Newtonsoft.Json;
using Microsoft.Xna.Framework.Input.Touch;
using System.Runtime.InteropServices.WindowsRuntime;
using Microsoft.Phone.Tasks;
using PhoneKit.Framework.Core.Tile;

namespace PhotoNote.Pages
{
    public partial class EditPage : PhoneApplicationPage
    {
        private EditPicture _editImage;

        ApplicationBarIconButton _appBarZoomButton;
        ApplicationBarIconButton _appBarPenButton;
        ApplicationBarIconButton _appBarTextButton;
        ApplicationBarIconButton _appBarUndoButton;
        ApplicationBarMenuItem _appBarSaveMenuItem;
        ApplicationBarMenuItem _appBarInstantShareMenuItem;
        ApplicationBarMenuItem _appBarCropMenuItem;
        ApplicationBarMenuItem _appBarPhotoInfoMenuItem;

        ApplicationBarIconButton _appBarDoneButton;

        private Random rand = new Random();

        private static readonly ScaleTransform NEUTRAL_SCALE = new ScaleTransform();

        private bool _isPenToolbarVisible = false;

        private const string PEN_POPUP_VISIBLE_KEY = "_pen_popup_vis_";
        private const string PEN_DATA_KEY = "_pen_data_";
        private const string CENTER_START_KEY = "_center_start_";

        private const string ZOOM_KEY = "_zoom_";
        private const string TRANSLATION_X_KEY = "_trans_x_";
        private const string TRANSLATION_Y_KEY = "_trans_y_";

        private const string TEXT_ELEMENTS_KEY = "_text_elements_";
        private const string TEXT_SELECTED_INDEX = "_text_sel_";

        private const string EDIT_MODE_KEY = "_edit_mode_";

        private const string EDIT_MODE_BEFORE_CROP_KEY = "_bc_edit_mode_";

        private const string CLIP_LEFT_PERC = "_cl_left_";
        private const string CLIP_RIGHT_PERC = "_cl_right_";
        private const string CLIP_TOP_PERC = "_cl_top_";
        private const string CLIP_BOTTOM_PERC = "_cl_bottom_";

        private const string NEED_TO_SAVE_KEY = "_need_save_";

        private double _zoom = 1.0;
        private double _translateX;
        private double _translateY;

        private const double ZOOM_MIN = 1.0;
        private const double ZOOM_MAX = 5.0;

        private EditMode _currentEditMode = EditMode.Marker;

        private EditMode _editModeBeforeCrop = EditMode.Marker;

        private static StoredObject<bool> ZoomingInfoShow = new StoredObject<bool>("_zoomingInfo_", false);

        private TextContext _textContext = new TextContext();

        private MarkerContext _markerContext = new MarkerContext();

        private System.Windows.Shapes.Rectangle _draggedRect = null;
        private double _clipLeftPerc, _clipRightPerc, _clipTopPerc, _clipBotPerc = 0;


        public EditPage()
        {
            InitializeComponent();
            BuildLocalizedApplicationBarButtons();

            // add these slightly delayed, to get no selected events while creating the UI.
            FontPicker.SelectionChanged += FontPickerSelectionChanged;
            FontPickerLandscape.SelectionChanged += FontPickerSelectionChanged;
            ThicknessSlider.ValueChanged += MarkerThicknessChanged;
            OpacitySlider.ValueChanged += MarkerOpacityChanged;

            Loaded += (s, e) => {
                UpdateOrientation(this.Orientation);

                UpdateTextAppBar();
                UpdateMarkerAppBar();

                UpdatePenToolbarWithContext(_markerContext);

                if (_selectedTextBox != null)
                    UpdateTextToolbarWithContext(_selectedTextBox.GetContext());
                else
                    UpdateTextToolbarWithContext(_textContext);

                AllTextBoxesToActiveState(_currentEditMode == EditMode.Text);
            };
        }

        private void InitializeCropRect()
        {
            var rects = new System.Windows.Shapes.Rectangle[] { CropRectTopRight, CropRectTopLeft, CropRectBotRight, CropRectBotLeft };
            System.Windows.Point _dragOrigin = new System.Windows.Point();
            double origLeftPerc = 0, origRightPerc = 0, origTopPerc = 0, origBotPerc = 0;

            var setOrigin = new Action<System.Windows.Point>((p) =>
            {
                _dragOrigin = p;
                origLeftPerc = this._clipLeftPerc;
                origRightPerc = this._clipRightPerc;
                origTopPerc = this._clipTopPerc;
                origBotPerc = this._clipBotPerc;
            });

            foreach (var aRect in rects)
            {
                aRect.MouseLeftButtonDown += (s, e) =>
                {
                    var r = (System.Windows.Shapes.Rectangle)s;
                    _draggedRect = r;
                    setOrigin(e.GetPosition(EditImageControl));

                    r.CaptureMouse();
                };

                aRect.MouseLeftButtonUp += (s, e) =>
                {
                    _draggedRect = null;
                };

                aRect.MouseMove += (s, e) =>
                {
                    if (_draggedRect != null)
                    {

                        var pos = e.GetPosition(EditImageControl);

                        if (IsTopClipRect(s))
                        {
                            _clipTopPerc = origTopPerc + (pos.Y - _dragOrigin.Y) / EditImageControl.Height;
                        }
                        if (IsLeftClipRect(s))
                        {
                            // Adjust Left
                            _clipLeftPerc = origLeftPerc + (pos.X - _dragOrigin.X) / EditImageControl.Width;
                        }
                        if (IsBottomClipRect(s))
                        {
                            // Adjust bottom
                            _clipBotPerc = origBotPerc - (pos.Y - _dragOrigin.Y) / EditImageControl.Height;
                        }
                        if (IsRightClipRect(s))
                        {
                            // Adjust Right
                            _clipRightPerc = origRightPerc - (pos.X - _dragOrigin.X) / EditImageControl.Width;
                        }

                        this.UpdateClipAndTransforms(s);
                    }
                };
            }

            // NOTE: not in use currently, but might be from interest later
            //var draggingImg = false;

            //EditImageControlForCropping.MouseLeftButtonDown += (s, e) =>
            //{
            //    setOrigin(e.GetPosition(this.EditImageControlForCropping));
            //    EditImageControlForCropping.CaptureMouse();
            //    draggingImg = true;
            //};

            //EditImageControlForCropping.MouseLeftButtonUp += (s, e) =>
            //{
            //    draggingImg = false;
            //};

            //EditImageControlForCropping.MouseMove += (s, e) =>
            //{
            //    if (draggingImg)
            //    {
            //        var pos = e.GetPosition(this.EditImageControlForCropping);

            //        var xAdjust = (pos.X - _dragOrigin.X) / EditImageControlForCropping.Width;
            //        var yAdjust = (pos.Y - _dragOrigin.Y) / EditImageControlForCropping.Height;

            //        _clipLeftPerc = origLeftPerc + xAdjust;
            //        _clipRightPerc = origRightPerc - xAdjust;
            //        _clipTopPerc = origTopPerc + yAdjust;
            //        _clipBotPerc = origBotPerc - yAdjust;

            //        this.UpdateClipAndTransforms();
            //    }
            //};

            EditImageControl.SizeChanged += (x, y) =>
            {
                this.UpdateClipAndTransforms();
            };

            this.UpdateClipAndTransforms();
        }

        private bool IsTopClipRect(object cropRect)
        {
            return cropRect == this.CropRectTopLeft || cropRect == this.CropRectTopRight;
        }

        private bool IsLeftClipRect(object cropRect)
        {
            return cropRect == this.CropRectTopLeft || cropRect == this.CropRectBotLeft;
        }

        private bool IsRightClipRect(object cropRect)
        {
            return cropRect == this.CropRectTopRight || cropRect == this.CropRectBotRight;
        }

        private bool IsBottomClipRect(object cropRect)
        {
            return cropRect == this.CropRectBotLeft || cropRect == this.CropRectBotRight;
        }

        

        void UpdateClipAndTransforms(object cropRect = null)
        {
            double w = EditImageControl.Width;
            double h = EditImageControl.Height;

            // ensure the image control is rendered
            if (w == 0 || h == 0)
                return;

            if (cropRect != null)
            {
                double offsetX = 0.25;
                double offsetY = 0.25;

                if (w != h)
                {
                    if (w > h)
                    {
                        offsetX = offsetX * (h / w);
                    }
                    else
                    {
                        offsetY = offsetY * (w / h);
                    }
                }

                if (_clipLeftPerc + _clipRightPerc >= 1.0 - offsetX)
                {
                    if (IsLeftClipRect(cropRect))
                        _clipLeftPerc = (1.0 - offsetX - _clipRightPerc);
                    else
                        _clipRightPerc = (1.0 - offsetX - _clipLeftPerc);
                }

                if (_clipTopPerc + _clipBotPerc >= 1.0 - offsetY)
                {
                    if (IsTopClipRect(cropRect))
                        _clipTopPerc = (1.0 - offsetY - _clipBotPerc);
                    else
                        _clipBotPerc = (1.0 - offsetY - _clipTopPerc);
                }
            }
            else
            {
                // paranoia check
                if (_clipLeftPerc + _clipRightPerc >= 1)
                    _clipLeftPerc = (1 - _clipRightPerc) - 0.15;
                if (_clipTopPerc + _clipBotPerc >= 1)
                    _clipTopPerc = (1 - _clipBotPerc) - 0.15;
            }

            if (_clipLeftPerc < 0)
                _clipLeftPerc = 0;
            if (_clipRightPerc < 0)
                _clipRightPerc = 0;
            if (_clipBotPerc < 0)
                _clipBotPerc = 0;
            if (_clipTopPerc < 0)
                _clipTopPerc = 0;
            if (_clipLeftPerc >= 1)
                _clipLeftPerc = 0.99;
            if (_clipRightPerc >= 1)
                _clipRightPerc = 0.99;
            if (_clipBotPerc >= 1)
                _clipBotPerc = 0.99;
            if (_clipTopPerc >= 1)
                _clipTopPerc = 0.99;


            // Image Clip
            var visibleRect = GetCropRect();

            ClipRectLeft.Rect = new Rect(0, 0, visibleRect.Left, h + CROPPING_GRAY_OFFSET);
            ClipRectRight.Rect = new Rect(visibleRect.Right, 0, w + CROPPING_GRAY_OFFSET - visibleRect.Right, h + CROPPING_GRAY_OFFSET);
            ClipRectTop.Rect = new Rect(0, 0, w + CROPPING_GRAY_OFFSET, visibleRect.Top);
            ClipRectBottom.Rect = new Rect(0, visibleRect.Bottom, w + CROPPING_GRAY_OFFSET, h + CROPPING_GRAY_OFFSET - visibleRect.Bottom);

            // Text
            var imageScale = 1 / GetScaleFactorOfOrientation();
            int imageWidth = (int)Math.Round(visibleRect.Width * imageScale);
            int imageHeight = (int)Math.Round(visibleRect.Height * imageScale);
            CropText.Text = string.Format(CultureInfo.InvariantCulture, "{0} x {1}", imageWidth, imageHeight);


            // Rectangle Transforms
            ((TranslateTransform)this.CropRectTopLeft.RenderTransform).X = visibleRect.X;
            ((TranslateTransform)this.CropRectTopLeft.RenderTransform).Y = visibleRect.Y;
            ((TranslateTransform)this.CropRectTopRight.RenderTransform).X = -_clipRightPerc * w;
            ((TranslateTransform)this.CropRectTopRight.RenderTransform).Y = visibleRect.Y;
            ((TranslateTransform)this.CropRectBotLeft.RenderTransform).X = visibleRect.X;
            ((TranslateTransform)this.CropRectBotLeft.RenderTransform).Y = -_clipBotPerc * h;
            ((TranslateTransform)this.CropRectBotRight.RenderTransform).X = -_clipRightPerc * w;
            ((TranslateTransform)this.CropRectBotRight.RenderTransform).Y = -_clipBotPerc * h;
        }

        /// <summary>
        /// Gets the cropping rect.
        /// </summary>
        /// <returns>The cropping rectangle in logical screen coordinated.</returns>
        private Rect GetCropRect()
        {
            double width = EditImageControl.Width;
            double height = EditImageControl.Height;
            var leftX = _clipLeftPerc * width;
            var topY = _clipTopPerc * height;

            var visibleRect = new Rect(leftX, topY, (1 - _clipRightPerc) * width - leftX, (1 - _clipBotPerc) * height - topY);
            return visibleRect;
        }

        /// <summary>
        /// Builds the localized app bar.
        /// </summary>
        private void BuildLocalizedApplicationBarButtons()
        {
            // ApplicationBar der Seite einer neuen Instanz von ApplicationBar zuweisen
            ApplicationBar = new ApplicationBar();
            ApplicationBar.Opacity = 0.99;

            // undo
            _appBarUndoButton = new ApplicationBarIconButton(new Uri("/Assets/AppBar/appbar.undo.curve.png", UriKind.Relative));
            _appBarUndoButton.Text = AppResources.AppBarUndo;
            _appBarUndoButton.Click += (s, e) =>
            {
                Undo();
            };

            // pen toolbar
            _appBarPenButton = new ApplicationBarIconButton(new Uri("/Assets/AppBar/appbar.draw.marker.Normal.png", UriKind.Relative));
            _appBarPenButton.Text = AppResources.AppBarPen;
            _appBarPenButton.Click += (s, e) =>
            {
                if (_currentEditMode != EditMode.Marker)
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

            // text
            _appBarTextButton = new ApplicationBarIconButton(new Uri("/Assets/AppBar/appbar.interface.textbox.png", UriKind.Relative));
            _appBarTextButton.Text = AppResources.AppBarText;
            _appBarTextButton.Click += (s, e) =>
            {
                if (_currentEditMode != EditMode.Text)
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
                    if (_selectedTextBox != null && _selectedTextBox.HasFocus)
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

            // save
            _appBarSaveMenuItem = new ApplicationBarMenuItem(AppResources.AppBarSave);
            _appBarSaveMenuItem.Click += async (s, e) =>
            {
                if (await Save() != null)
                {
                    Dispatcher.BeginInvoke(() =>
                    {
                        // BugSense: Navigation is not allowed when the task is not in the foreground
                        //           Solution: http://stackoverflow.com/questions/7373533/navigation-is-not-allowed-when-the-task-is-not-in-the-foreground-in-wp7-applic
                        NavigationHelper.BackToMainPageWithHistoryClear(NavigationService);
                    });
                    
                }
                else
                {
                    MessageBox.Show(AppResources.MessageBoxNoSave, AppResources.MessageBoxWarning, MessageBoxButton.OK);
                }
            };

            // instant share
            _appBarInstantShareMenuItem = new ApplicationBarMenuItem(AppResources.AppBarSaveAndShare);
            _appBarInstantShareMenuItem.Click += async (s, e) =>
            {
                var fileName = await Save();
                if (fileName != null)
                {
                    var shareTask = new ShareMediaTask();
                    shareTask.FilePath = "C:\\Data\\Users\\Public\\Pictures\\Saved Pictures\\" + fileName;
                    shareTask.Show();
                }
                else
                {
                    MessageBox.Show(AppResources.MessageBoxNoSave, AppResources.MessageBoxWarning, MessageBoxButton.OK);
                }
            };

            // crop
            _appBarCropMenuItem = new ApplicationBarMenuItem(AppResources.AppBarCrop);
            _appBarCropMenuItem.Click += (s, e) =>
            {
                ChangedEditMode(EditMode.Cropping);
            };

            // image info (photo info)
            _appBarPhotoInfoMenuItem = new ApplicationBarMenuItem(AppResources.ShowPhotoInfo);
            _appBarPhotoInfoMenuItem.Click += async (s, e) =>
            {
                if (HasNoImage())
                    return;

                if (!await LauncherHelper.LaunchPhotoInfoAsync(_editImage.FullName))
                {
                    MessageBox.Show(AppResources.MessageBoxNoInfo, AppResources.MessageBoxWarning, MessageBoxButton.OK);
                }
            };

            // done
            _appBarDoneButton = new ApplicationBarIconButton(new Uri("/Assets/AppBar/appbar.check.png", UriKind.Relative));
            _appBarDoneButton.Text = AppResources.AppBarDone;
            _appBarDoneButton.Click += (s, e) =>
            {
                ChangedEditMode(_editModeBeforeCrop);
                UpdateTextAppBar();
                UpdateMarkerAppBar();
            };
        }

        private void UpdateApplicationBar(EditMode editMode)
        {
            ApplicationBar.Buttons.Clear();
            ApplicationBar.MenuItems.Clear();

            if (editMode == EditMode.Cropping)
            {
                ApplicationBar.Buttons.Add(_appBarDoneButton);
            }
            else
            {
                ApplicationBar.Buttons.Add(_appBarUndoButton);
                ApplicationBar.Buttons.Add(_appBarPenButton);
                ApplicationBar.Buttons.Add(_appBarTextButton);
                ApplicationBar.Buttons.Add(_appBarZoomButton);
                ApplicationBar.MenuItems.Add(_appBarSaveMenuItem);
                ApplicationBar.MenuItems.Add(_appBarInstantShareMenuItem);
                ApplicationBar.MenuItems.Add(_appBarCropMenuItem);
                ApplicationBar.MenuItems.Add(_appBarPhotoInfoMenuItem);
            }
        }

        private async Task<string> Save()
        {
            if (HasNoImage())
                return null;

            SavingPopup.Visibility = System.Windows.Visibility.Visible;

            await Task.Delay(33);
            string resultString;
            try
            {
                using (var memStream = new MemoryStream())
                {
                    var neutralScaleFactor = GetBiggestScaleFactorOfSmallerOrientation();
                    var textContextList = GetTextBoxContextList();

                    //var editedImageInkControl = new EditedImageInkControl(_editImage.FullImage as BitmapSource, InkControl.Strokes, textContextList, 1.0 / neutralScaleFactor);
                    // reuse the picture of EditImageControl.Source, which saves a lot of memory!
                    var editedImageInkControl = new EditedImageInkControl(EditImageControl.Source as BitmapSource, InkControl.Strokes, textContextList, 1.0 / neutralScaleFactor);

                    string imageName = _editImage.Name;
                    RenderingTrash.Children.Add(editedImageInkControl); // add to visual tree to enforce Bindings are invoked // note: SAVES MEMORY according to Profiler tests (why ever !?!)
                    
                    await Task.Delay(500);
                    var gfx = GraphicsHelper.Create(editedImageInkControl);

                    var imageScale = 1 / GetScaleFactorOfOrientation();
                    var cropRect = GetCropRect();
                    var scaledClipRect = new Rect(Math.Round(cropRect.Left * imageScale), Math.Round(cropRect.Top * imageScale),
                                                  Math.Round(cropRect.Width * imageScale), Math.Round(cropRect.Height * imageScale));
                    if ((int)scaledClipRect.Width != gfx.PixelWidth || (int)scaledClipRect.Height != gfx.PixelHeight)
                    {
                        gfx = gfx.Crop(scaledClipRect);
                    }

                    // save to memory stream
                    gfx.SaveJpeg(memStream, gfx.PixelWidth, gfx.PixelHeight, 0, 100);
                    
                    GraphicsHelper.CleanUpMemory(gfx);
                    RenderingTrash.Children.Remove(editedImageInkControl); // and remove it again from visual tree
                    
                    memStream.Seek(0, SeekOrigin.Begin);

                    // save to library
                    using (var media = StaticMediaLibrary.Instance)
                    {
                        var nameWithoutExtension = Path.GetFileNameWithoutExtension(imageName);

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
                        var outputName = string.Format("{0}_{1:0000}.jpg", nameWithoutExtension, rand.Next(9999));
                        media.SavePicture(outputName, memStream);
                        resultString = outputName;

                        _needToSave = false;
                    }
                }
            } 
            catch (Exception)
            {
                resultString = null;
            } 
            finally
            {
                SavingPopup.Visibility = System.Windows.Visibility.Collapsed;
            }

            return resultString;
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

            UpdateApplicationBar(_currentEditMode);
            InitializeCropRect();

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
            // marker
            _markerContext.Color = AppSettings.PenColor.Value;
            _markerContext.Opacity = AppSettings.PenOpacity.Value;
            var markerSize = AppSettings.PenThickness.Value;
            if (markerSize > AppConstants.MAX_MARKER_SIZE)
            {
                // check because marker size was minimized in version 2.0
                markerSize = AppConstants.MAX_MARKER_SIZE;
            }
            _markerContext.Size = markerSize;
            _markerContext.Mode = AppSettings.DrawMode.Value;

            // text
            _textContext.Color = AppSettings.TextColor.Value;
            _textContext.Alignment = AppSettings.TextAlignment.Value;
            _textContext.HasBorder = AppSettings.TextBorder.Value;
            _textContext.HasBackgroundBorder = AppSettings.TextBackgroundBorder.Value;
            _textContext.Opacity = AppSettings.TextOpacity.Value;
            _textContext.Size = AppSettings.TextSize.Value;
            _textContext.Style = FontHelper.GetStyle(AppSettings.TextStyle.Value);
            _textContext.Weight = FontHelper.GetWeight(AppSettings.TextWeight.Value);

            if (_fontFamilyFromChangedEvent != null)
            {
                // do not override the selected value from changed event.
                _textContext.Font = _fontFamilyFromChangedEvent;
                _fontFamilyFromChangedEvent = null;
            }
            else
            {
                _textContext.Font = FontHelper.GetFont(AppSettings.TextFont.Value);
            }
        }

        private void SaveSettings()
        {
            // marker
            AppSettings.PenColor.Value = _markerContext.Color;
            AppSettings.PenOpacity.Value = _markerContext.Opacity;
            AppSettings.PenThickness.Value = _markerContext.Size;
            AppSettings.DrawMode.Value = _markerContext.Mode;

            // text
            AppSettings.TextColor.Value = _textContext.Color;
            AppSettings.TextAlignment.Value = _textContext.Alignment;
            AppSettings.TextFont.Value = FontHelper.GetString(_textContext.Font);
            AppSettings.TextBorder.Value = _textContext.HasBorder;
            AppSettings.TextBackgroundBorder.Value = _textContext.HasBackgroundBorder;
            AppSettings.TextOpacity.Value = _textContext.Opacity;
            AppSettings.TextSize.Value = _textContext.Size;
            AppSettings.TextStyle.Value = FontHelper.GetString(_textContext.Style);
            AppSettings.TextWeight.Value = FontHelper.GetString(_textContext.Weight);
        }

        private void RestoreState()
        {
            // edit mode
            var editMode = PhoneStateHelper.LoadValue<EditMode>(EDIT_MODE_KEY, EditMode.Marker);
            PhoneStateHelper.DeleteValue(EDIT_MODE_KEY);
            ChangedEditMode(editMode);

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

            // clipping
            var clipLeft = PhoneStateHelper.LoadValue<double>(CLIP_LEFT_PERC, 0);
            PhoneStateHelper.DeleteValue(CLIP_LEFT_PERC);
            _clipLeftPerc = clipLeft;
            var clipRight = PhoneStateHelper.LoadValue<double>(CLIP_RIGHT_PERC, 0);
            PhoneStateHelper.DeleteValue(CLIP_RIGHT_PERC);
            _clipRightPerc = clipRight;
            var clipTop = PhoneStateHelper.LoadValue<double>(CLIP_TOP_PERC, 0);
            PhoneStateHelper.DeleteValue(CLIP_TOP_PERC);
            _clipTopPerc = clipTop;
            var clipBottom = PhoneStateHelper.LoadValue<double>(CLIP_BOTTOM_PERC, 0);
            PhoneStateHelper.DeleteValue(CLIP_BOTTOM_PERC);
            _clipBotPerc = clipBottom;

            // last edit mode before crop
            var editModeBeforeCrop = PhoneStateHelper.LoadValue<EditMode>(EDIT_MODE_BEFORE_CROP_KEY, EditMode.Marker);
            PhoneStateHelper.DeleteValue(EDIT_MODE_BEFORE_CROP_KEY);
            _editModeBeforeCrop = editModeBeforeCrop;

            // need save state
            var needSave = PhoneStateHelper.LoadValue<bool>(NEED_TO_SAVE_KEY, false);
            PhoneStateHelper.DeleteValue(NEED_TO_SAVE_KEY);
            _needToSave = needSave;

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

            // text elements
            if (EditTextControl.Children.Count == 0)
            {
                var jsonArrayTextContexts = PhoneStateHelper.LoadValue<string>(TEXT_ELEMENTS_KEY, null);
                if (!string.IsNullOrEmpty(jsonArrayTextContexts))
                {
                    PhoneStateHelper.DeleteValue(TEXT_ELEMENTS_KEY);
                    var tbContextList = JsonConvert.DeserializeObject<IList<TextBoxContext>>(jsonArrayTextContexts);
                
                    foreach (var context in tbContextList)
	                {
                        var textbox = AddTextBox(EditTextControl, context);
                        textbox.IsActive = false;
                        textbox.IsEnabled = false;
	                }
                }
            }

            // text selected index
            var textSelectedIndex = PhoneStateHelper.LoadValue<int>(TEXT_SELECTED_INDEX, -1);
            PhoneStateHelper.DeleteValue(TEXT_SELECTED_INDEX);
            if (textSelectedIndex != -1 && textSelectedIndex < EditTextControl.Children.Count)
            {
                var textbox = EditTextControl.Children[textSelectedIndex] as ExtendedTextBox;
                SelectTextBox(textbox);
                SetSelectionTextFont(textbox.FontFamily);
            } // FIXME: selecting the item causes that the seleted items font is reverted to DEFAULT or that of the context.  (!?)
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
            // edit mode
            PhoneStateHelper.SaveValue(EDIT_MODE_KEY, _currentEditMode);

            // popup state
            PhoneStateHelper.SaveValue(PEN_POPUP_VISIBLE_KEY, _isPenToolbarVisible);

            // zoom
            PhoneStateHelper.SaveValue(ZOOM_KEY, _zoom);

            // translation
            PhoneStateHelper.SaveValue(TRANSLATION_X_KEY, _translateX);
            PhoneStateHelper.SaveValue(TRANSLATION_Y_KEY, _translateY);

            // clipping
            PhoneStateHelper.SaveValue(CLIP_LEFT_PERC, _clipLeftPerc);
            PhoneStateHelper.SaveValue(CLIP_RIGHT_PERC, _clipRightPerc);
            PhoneStateHelper.SaveValue(CLIP_TOP_PERC, _clipTopPerc);
            PhoneStateHelper.SaveValue(CLIP_BOTTOM_PERC, _clipBotPerc);

            // last edit mode before crop
            PhoneStateHelper.SaveValue(EDIT_MODE_BEFORE_CROP_KEY, _editModeBeforeCrop);

            // need save state
            PhoneStateHelper.SaveValue(NEED_TO_SAVE_KEY, _needToSave);

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

            // text elements
            if (EditTextControl.Children.Count > 0)
            {
                var contextList = GetTextBoxContextList();
                if (contextList != null)
                {
                    var jsonArrayTextContexts = JsonConvert.SerializeObject(contextList);
                    PhoneStateHelper.SaveValue(TEXT_ELEMENTS_KEY, jsonArrayTextContexts);
                }
                
            }

            // text selected index
            if (_selectedTextBox != null)
            {
                var index = -1;

                for (int i = 0; i < EditTextControl.Children.Count; ++i)
                {
                    var textbox = EditTextControl.Children[i] as ExtendedTextBox;
                    if (textbox != null && _selectedTextBox == textbox)
                    {
                        index = i;
                        break;
                    }
                }
                PhoneStateHelper.SaveValue(TEXT_SELECTED_INDEX, index);
            }
            
        }

        private bool UpdatePicture(EditPicture pic)
        {
            try
            {
                _editImage = pic;
                UpdateZoomAppBarIcon();
                UpdateImageOrientationAndScale();
                EditImageControl.Source = _editImage.FullImage;
            }
            catch(Exception)
            {
                // sometimes the image can not be loaded (whyever...)
                return false;
            }
            
            return true;
        }

        /// <summary>
        /// Offset dimension offset to see no transparent 1px border.
        /// </summary>
        private const int CROPPING_GRAY_OFFSET = 2;

        private void UpdateImageOrientationAndScale()
        {
            if (HasNoImage())
                return;

            // image
            var scale = GetScaleFactorOfOrientation();
            CroppingControl.Width = (scale * _editImage.Width) + CROPPING_GRAY_OFFSET;
            CroppingControl.Height = (scale * _editImage.Height) + CROPPING_GRAY_OFFSET;

            EditImageControl.Width = scale * _editImage.Width;
            EditImageControl.Height = scale * _editImage.Height;

            CropRectsContainer.Width = scale * _editImage.Width;
            CropRectsContainer.Height = scale * _editImage.Height;

            // ink surface
            var neutralScaleFactor = GetBiggestScaleFactorOfSmallerOrientation();
            InkControl.Width = neutralScaleFactor * _editImage.Width;
            InkControl.Height = neutralScaleFactor * _editImage.Height;

            // text control
            EditTextControl.Width = neutralScaleFactor * _editImage.Width;
            EditTextControl.Height = neutralScaleFactor * _editImage.Height;

            // input control
            InputControl.Width = neutralScaleFactor * _editImage.Width;
            InputControl.Height = neutralScaleFactor * _editImage.Height;

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

            var imageCropTransform = CroppingControl.RenderTransform as CompositeTransform;
            imageCropTransform.ScaleX = _zoom;
            imageCropTransform.ScaleY = _zoom;
            imageCropTransform.TranslateX = _translateX;
            imageCropTransform.TranslateY = _translateY;

            var cropRectsTransform = CropRectsContainer.RenderTransform as CompositeTransform;
            cropRectsTransform.ScaleX = _zoom;
            cropRectsTransform.ScaleY = _zoom;
            cropRectsTransform.TranslateX = _translateX;
            cropRectsTransform.TranslateY = _translateY;

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

            var inputTransform = InputControl.RenderTransform as CompositeTransform;
            inputTransform.ScaleX = scale / neutralScaleFactor * _zoom;
            inputTransform.ScaleY = scale / neutralScaleFactor * _zoom;
            inputTransform.TranslateX = _translateX;
            inputTransform.TranslateY = _translateY;

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

            UpdateOrientation(e.Orientation);

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

        /// <summary>
        /// Updates the orientation visual state.
        /// </summary>
        private void UpdateOrientation(PageOrientation orientation)
        {
            if (orientation == PageOrientation.Portrait ||
                orientation == PageOrientation.PortraitDown ||
                orientation == PageOrientation.PortraitUp)
            {
                VisualStateManager.GoToState(this, "Portrait", true);
            }
            else if (orientation == PageOrientation.Landscape ||
                orientation == PageOrientation.LandscapeLeft)
            {
                VisualStateManager.GoToState(this, "LandscapeLeft", true);
            }
            else if (orientation == PageOrientation.LandscapeRight)
            {
                VisualStateManager.GoToState(this, "LandscapeRight", true);
            }

            UpdateClipAndTransforms();
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
            else if (_currentEditMode == EditMode.Cropping)
            {
                ChangedEditMode(_editModeBeforeCrop);
                UpdateTextAppBar();
                UpdateMarkerAppBar();
                e.Cancel = true;
            }
            else if (NeedToSave)
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

        private bool _needToSave;

        private void UpdateNeedToSaveStatus()
        {
            _needToSave = InkControl.Strokes.Count > 0 || EditTextControl.Children.Count > 0;
        }

        /// <summary>
        /// Indicates whether the save dialog has to be shown.
        /// </summary>
        private bool NeedToSave
        {
            get
            {
                return _needToSave;
            }
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

            string visualState = null;
            if (_currentEditMode == EditMode.Marker)
                visualState = "DisplayedPenMode";
            else if (_currentEditMode == EditMode.Text)
                visualState = "DisplayedTextMode";

            if (visualState != null)
            {
                VisualStateManager.GoToState(this, visualState, useTransition);
                _isPenToolbarVisible = true;
            }
        }

        public void HidePenToolbar()
        {
            if (!_isPenToolbarVisible)
                return;

            VisualStateManager.GoToState(this, "Normal", true);
            _isPenToolbarVisible = false;
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
                _markerContext.Mode = toggledMode;
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
        private int _inkMoveCounter;

        /// <summary>
        /// Used to make sure no stroke is added multiple times. Not really necessary anymore, but makes sure
        /// the performance will not be bad in the move-event.
        /// </summary>
        private bool strokeAdded;

        //A new stroke object named MyStroke is created. MyStroke is added to the StrokeCollection of the InkPresenter named MyIP
        private void MyIP_MouseLeftButtonDown(object sender, MouseEventArgs e)
        {
            if (_currentEditMode != EditMode.Marker)
                return;

            InkControl.CaptureMouse();
            StylusPointCollection MyStylusPointCollection = new StylusPointCollection();
            var touchPoint = e.StylusDevice.GetStylusPoints(InkControl).First();
            _centerStart = new Vector2((float)touchPoint.X, (float)touchPoint.Y);

            if (!_isPenToolbarVisible && AppSettings.TouchHoldPenOptimization.Value == true) // only when setting is active
            {
                // delayed display of first point, that the point is not visible
                // when tapping on the screen to close the toolbar
                _activeStroke = StartStroke();
                InkControl.Strokes.Add(_activeStroke);
            }
            
        }

        //StylusPoint objects are collected from the MouseEventArgs and added to MyStroke. 
        private void MyIP_MouseMove(object sender, MouseEventArgs e)
        {
            if (_currentEditMode != EditMode.Marker)
                return;

            if (_twoFingersActive)
            {
                // remove it, because 
                if (_activeStroke != null )
                {
                    InkControl.Strokes.Remove(_activeStroke);
                    _activeStroke = null;
                }
                return;
            }

            _inkMoveCounter++;

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

            switch (_markerContext.Mode)
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
            if (_currentEditMode == EditMode.Marker)
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

                if (_markerContext.Mode == DrawMode.Arrow)
                {
                    FinishArrow();
                }

                // remove ink segments when two fingers have been detected shortly after the drawing has begun.
                // just to make shure that really everything was cleaned, because this cleaning is also done in
                // move envent.
                if (_twoFingersActive && _inkMoveCounter < 3)
                {
                    InkControl.Strokes.Remove(_activeStroke);
                }
                else if (!_twoFingersActive)
                {
                    // stroke was added, so the image was changed and needs to be saved
                    UpdateNeedToSaveStatus();
                }
            }

            // reset current context
            _activeStroke = null;
            strokeAdded = false;
            _twoFingersActive = false;
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
            var opacity = _markerContext.Opacity;
            var color = _markerContext.Color;
            var size = _markerContext.Size;
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
                float shoulderLength = GetShoulderLength(arrowDirection.Length(), (float)_markerContext.Size);
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

                UpdateNeedToSaveStatus();
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

        private void ResetZoom()
        {
            _zoom = ZOOM_MIN;

            // reset translation
            _translateX = 0;
            _translateY = 0;

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
            var iconUriString = string.Format("/Assets/AppBar/appbar.draw.marker.{0}{1}.png", _markerContext.Mode.ToString(), active);
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

            // reset move counter
            _inkMoveCounter = 0;
            _twoFingersActive = false;

            if (_currentEditMode == EditMode.Text)
            {
                // init start of manipulation
                translationDeltaX = double.MinValue;
                translationDeltaY = double.MinValue;
                zoomBaseline = _zoom;

                _previouslySelectedTextBox = _selectedTextBox;
                _selectedTextBox = null;

                ExtendedTextBox textBoxToSelect = null;
                for (int i = EditTextControl.Children.Count - 1; i >= 0; --i)
                {
                    var textbox = EditTextControl.Children[i] as ExtendedTextBox;
                    if (textbox != null)
                    {
                        var boundingBox = new Rectangle((int)Canvas.GetLeft(textbox) - TEXT_SELECTION_MARGIN, (int)Canvas.GetTop(textbox) - TEXT_SELECTION_MARGIN,
                                                        (int)textbox.ActualWidth + 2 * TEXT_SELECTION_MARGIN, (int)textbox.ActualHeight + 2 * TEXT_SELECTION_MARGIN);

                        if (boundingBox.Contains((int)e.ManipulationOrigin.X, (int)e.ManipulationOrigin.Y))
                        {
                            // just remember selection to select the new one after
                            // the old one is unselected
                            textBoxToSelect = textbox;
                            
                            break;
                        }
                    }
                }

                if (_previouslySelectedTextBox != textBoxToSelect)
                    UnselectTextBox(ref _previouslySelectedTextBox);

                if (textBoxToSelect != null)
                {
                    SelectTextBox(textBoxToSelect);

                    // update UI in toolbar
                    UpdateTextToolbarWithContext(textBoxToSelect.GetContext());
                }

                // reset previous selection when nothing was clicked
                if (_selectedTextBox == null)
                {
                    _previouslySelectedTextBox = null;
                }
            }
        }

        private void MyIP_ManipulationDelta(object sender, ManipulationDeltaEventArgs e)
        {
            if (_currentEditMode == EditMode.Cropping)
                return;

            // first make sure we re using 2 fingers
            if (e.PinchManipulation != null)
            {
                _twoFingersActive = true;

                InkPresenter photo = InkControl;
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
            else if (_selectedTextBox != null && !_twoFingersActive)
            {
                // move text box
                _selectedTextBox.SetPosition(EditTextControl, e.ManipulationOrigin.X, e.ManipulationOrigin.Y);
            }

            if (_currentEditMode == EditMode.Text)
            {
                _inkMoveCounter++;
            }
        }

        private bool _upgradePopupShown;

        private void MyIP_ManipulationCompleted(object sender, ManipulationCompletedEventArgs e)
        {
            if (_currentEditMode == EditMode.Text && _selectedTextBox != null)
            {
                // remove textbox when moved out of image
                if (!_selectedTextBox.IsInBounds(EditTextControl))
                {
                    RemoveTextBox(EditTextControl, ref _selectedTextBox);

                    UpdateNeedToSaveStatus();
                }
            }

            if (_currentEditMode == EditMode.Text && !_twoFingersActive)
            {
                if (_selectedTextBox != null)
                {
                    _selectedTextBox.IsEnabled = true;

                    if (_previouslySelectedTextBox == _selectedTextBox && _inkMoveCounter == 0)
                    {
                        EditTextBox(_selectedTextBox);
                    }
                }
                else if (_previouslySelectedTextBox == null && _inkMoveCounter <= 1 && !_isKeyboardActive)
                {
                    if (_isPenToolbarVisible)
                    {
                        HidePenToolbar();
                    }
                    else
                    {
                        if (EditTextControl.Children.Count == 0 || InAppPurchaseHelper.IsProductActive(AppConstants.IAP_PREMIUM_VERSION))
                        {
                            var context = new TextBoxContext(string.Empty, e.ManipulationOrigin.X, e.ManipulationOrigin.Y, 0, _textContext);
                            var textbox = AddTextBox(EditTextControl, context);

                            // select
                            SelectTextBox(textbox);
                            EditTextBox(_selectedTextBox);

                            // show text options
                            ShowTextOptionsAnimation.Begin();

                            UpdateNeedToSaveStatus();
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
            var lastEditMode = _currentEditMode;
            _currentEditMode = newEditMode;

            // check if old mode was cropping
            if (lastEditMode == EditMode.Cropping && newEditMode != lastEditMode)
            {
                CropDisabledAnimation.Begin();
            }

            if ((lastEditMode == EditMode.Cropping || newEditMode == EditMode.Cropping) && lastEditMode != newEditMode)
            {
                UpdateApplicationBar(newEditMode);
            }

            if (newEditMode == EditMode.Text)
            {
                AllTextBoxesToActiveState(true);
                UpdateTextToolbarWithContext(_textContext);
                _appBarUndoButton.IsEnabled = false;
                InputControl.Visibility = Visibility.Visible;
            }
            else if (newEditMode == EditMode.Marker)
            {
                AllTextBoxesToActiveState(false);
                UnselectTextBox(ref _selectedTextBox);
                UpdatePenToolbarWithContext(_markerContext);
                _appBarUndoButton.IsEnabled = true;
                InputControl.Visibility = Visibility.Collapsed;
            }
            else if (newEditMode == EditMode.Cropping)
            {
                // store the last edit mode to be able to get back and make sure
                // not to store crop mode (which leads to an endless loop)
                if (lastEditMode != EditMode.Cropping)
                {
                    _editModeBeforeCrop = lastEditMode;
                }

                AllTextBoxesToActiveState(false);
                UnselectTextBox(ref _selectedTextBox);
                ResetZoom();
                HidePenToolbar();

                InputControl.Visibility = Visibility.Collapsed;

                CropEnabledAnimation.Begin();
            }
        }

        /// <summary>
        /// Add a text with current text properties.
        /// </summary>
        /// <param name="parent">The parent canvas container.</param>
        /// <param name="text">The default text.</param>
        /// <param name="x">The x coord.</param>
        /// <param name="y">THe y coord.</param>
        private ExtendedTextBox AddTextBox(Canvas parent, TextBoxContext context)
        {
            var textbox = new ExtendedTextBox();
            textbox.Text = context.Text;
            textbox.SetContext(context.Context);
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
            parent.Children.Add(textbox);
            textbox.UpdateLayout();

            textbox.SetPosition(parent, context.X, context.Y);
            return textbox;
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

        private void TextOptionsRotateClicked(object sender, RoutedEventArgs e)
        {
            if (_selectedTextBox != null)
            {
                _selectedTextBox.Rotate(15);
            }
        }

        /// <summary>
        /// Unsets the focus and removes the text box.
        /// </summary>
        /// <param name="parent">The container panel.</param>
        /// <param name="textBox">The text box to remove.</param>
        private void RemoveTextBox(Panel parent, ref ExtendedTextBox textBox)
        {
            parent.Children.Remove(textBox);
            UnselectTextBox(ref textBox);
        }

        private void SelectTextBox(ExtendedTextBox textBox)
        {
            if (textBox != null)
            {
                _selectedTextBox = textBox;
                _selectedTextBox.IsActive = true;
                _selectedTextBox.IsEnabled = true;
            }
        }

        /// <summary>
        /// Sets the read only state of all text boxes, where readOnly means
        /// if a text box is highlighted or not.
        /// </summary>
        /// <param name="readOnly">The read only value.</param>
        private void AllTextBoxesToActiveState(bool active)
        {
            foreach (var tb in EditTextControl.Children)
            {
                var textbox = tb as ExtendedTextBox;
                if (textbox != null)
                {
                    textbox.IsActive = active;
                }
            }
        }

        /// <summary>
        /// Unselects the active text box.
        /// </summary>
        private void UnselectTextBox(ref ExtendedTextBox textBox)
        {
            if (textBox != null)
            {
                textBox.IsEnabled = false;
                textBox = null;
            }

            // set text back to general text context
            UpdateTextToolbarWithContext(_textContext);
        }

        /// <summary>
        /// Edits the text box.
        /// </summary>
        private void EditTextBox(ExtendedTextBox textBox)
        {
            if (textBox != null)
            {
                textBox.IsEnabled = true;
                textBox.Focus();
                textBox.SelectLast();

                // hide toolbar to make sure the nothing is hiding the text element
                if (_isPenToolbarVisible)
                {
                    HidePenToolbar();
                    _isPenToolbarVisible = false;
                }
            }
        }

        /// <summary>
        /// Updates the toolbar to the given context.
        /// </summary>
        /// <param name="context">The context to set.</param>
        private void UpdatePenToolbarWithContext(MarkerContext context)
        {
            OpacitySlider.Value = context.Opacity;
            ThicknessSlider.Value = context.Size;
            SetTogglesToMode(context.Mode);

            if (_currentEditMode == EditMode.Marker)
                ColorPicker.Color = context.Color;
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

            if (_currentEditMode == EditMode.Text)
                ColorPicker.Color = context.Color;
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
            if (_currentEditMode == EditMode.Marker)
            {
                _markerContext.Color = color;
            }
            else if (_currentEditMode == EditMode.Text)
            {
                _textContext.Color = color;

                if (_selectedTextBox != null)
                {
                    // Remark: color not used, because we do not want to create a new instance each change
                    _selectedTextBox.Foreground = ColorPicker.SolidColorBrush;
                }
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

        private FontFamily _fontFamilyFromChangedEvent; // because loadstate overrides the changes

        private void FontPickerSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var picker = sender as ListPicker;
            if (picker != null)
            {
                var selectedFontItem = picker.SelectedItem as FontItemViewModel;

                if (selectedFontItem != null)
                {
                    _fontFamilyFromChangedEvent = selectedFontItem.Font;
                    UpdateTextFont(selectedFontItem.Font);
                    SetSelectionTextFont(selectedFontItem.Font);
                }
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
            if (FontPicker == null || FontPickerLandscape == null)
                return;

            // unregister events
            FontPicker.SelectionChanged -= FontPickerSelectionChanged;
            FontPickerLandscape.SelectionChanged -= FontPickerSelectionChanged;

            // update UI
            FontPicker.SelectedItem = FontsViewModel.GetItemByFont(font);
            FontPickerLandscape.SelectedItem = FontsViewModel.GetItemByFont(font);

            // reregister events
            FontPicker.SelectionChanged += FontPickerSelectionChanged;
            FontPickerLandscape.SelectionChanged += FontPickerSelectionChanged;
        }

        private void MarkerThicknessChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            _markerContext.Size = e.NewValue;
        }

        private void MarkerOpacityChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            _markerContext.Opacity = e.NewValue;
        }
    }
}