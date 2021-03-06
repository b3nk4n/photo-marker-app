using System;
using System.Windows;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using PhotoNote.Resources;
using PhotoNote.Helpers;
using PhotoNote.Model;
using System.Windows.Media;
using System.Windows.Input;

namespace PhotoNote.Pages
{
    public partial class DetailPage : PhoneApplicationPage
    {
        private EditPicture _editImage;

        public DetailPage()
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


            // share
            ApplicationBarIconButton appBarShareButton = new ApplicationBarIconButton(new Uri("/Assets/AppBar/appbar.share.png", UriKind.Relative));
            appBarShareButton.Text = AppResources.Share;
            appBarShareButton.Click += (s, e) =>
            {
                if (HasNoImage())
                    return;

                _editImage.Share();
            };
            ApplicationBar.Buttons.Add(appBarShareButton);

            // edit
            ApplicationBarIconButton appBarPenButton = new ApplicationBarIconButton(new Uri("/Assets/AppBar/appbar.edit.png", UriKind.Relative));
            appBarPenButton.Text = AppResources.Edit;
            appBarPenButton.Click += (s, e) =>
            {
                if (HasNoImage())
                    return;

                var uriString = string.Format("/Pages/EditPage.xaml?{0}={1}", AppConstants.PARAM_SELECTED_FILE_NAME, _editImage.Name);
                NavigationService.Navigate(new Uri(uriString, UriKind.Relative));
            };
            ApplicationBar.Buttons.Add(appBarPenButton);

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

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            // query string lookup
            bool success = false;
            if (NavigationContext.QueryString != null)
            {
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

                // error handling? - go back or exit
                if (!success)
                {
                    MessageBox.Show(AppResources.MessageBoxUnknownError, AppResources.MessageBoxWarning, MessageBoxButton.OK);
                    NavigationHelper.BackToMainPageWithHistoryClear(NavigationService);
                    return;
                }
            }
        }

        private bool UpdatePicture(EditPicture pic)
        {
            _editImage = pic;
            ImageControl.Source = _editImage.Image; // do not use the full image here, because every time this page is visited, a new image is generated and memory is consumed
            return true;
        }

        private bool HasNoImage()
        {
            return _editImage == null || _editImage.Height == 0 || _editImage.Width == 0;
        }

        #region Image Zooming 
        // Source: http://nikovrdoljak.wordpress.com/2014/04/08/flick-and-zoom-with-viewport-control/

        private double m_Zoom = 1;
        private double m_Width = 0;
        private double m_Height = 0;

        private void image_Loaded(object sender, RoutedEventArgs e)
        {
            ImageControl.Width = ImageControl.ActualWidth;
            ImageControl.Height = ImageControl.ActualHeight;
            m_Width = ImageControl.Width;
            m_Height = ImageControl.Height;

            // Initaialy we put Stretch to None in XAML part, so we can read image ActualWidth i ActualHeight (otherwise values are strange)
            // After that we set Stretch to UniformToFill in order to be able to resize image
            ImageControl.Stretch = Stretch.Uniform;
            viewport.Bounds = new Rect(0, 0, ImageControl.ActualWidth, ImageControl.ActualHeight);

            ResetZoom();
        }

        private void viewport_ManipulationDelta(object sender, ManipulationDeltaEventArgs e)
        {
            if (e.PinchManipulation != null)
            {
                double newWidth, newHeight;


                if (m_Width < m_Height)  // box new size between image and viewport
                {
                    newHeight = m_Height * m_Zoom * e.PinchManipulation.CumulativeScale;
                    newHeight = Math.Max(viewport.ActualHeight, newHeight);
                    newHeight = Math.Min(newHeight, m_Height);
                    newWidth = newHeight * m_Width / m_Height;
                }
                else
                {
                    newWidth = m_Width * m_Zoom * e.PinchManipulation.CumulativeScale;
                    newWidth = Math.Max(viewport.ActualWidth, newWidth);
                    newWidth = Math.Min(newWidth, m_Width);
                    newHeight = newWidth * m_Height / m_Width;
                }


                if (newWidth < m_Width && newHeight < m_Height)
                {
                    MatrixTransform transform = ImageControl.TransformToVisual(viewport)
                      as MatrixTransform;
                    Point pinchCenterOnImage =
                      transform.Transform(e.PinchManipulation.Original.Center);
                    Point relativeCenter =
                      new Point(e.PinchManipulation.Original.Center.X / ImageControl.Width,
                      e.PinchManipulation.Original.Center.Y / ImageControl.Height);
                    Point newOriginPoint = new Point(
                      relativeCenter.X * newWidth - pinchCenterOnImage.X,
                      relativeCenter.Y * newHeight - pinchCenterOnImage.Y);
                    viewport.SetViewportOrigin(newOriginPoint);
                }

                ImageControl.Width = newWidth;
                ImageControl.Height = newHeight;

                // Set new view port bound
                viewport.Bounds = new Rect(0, 0, newWidth, newHeight);
            }
        }

        private void viewport_ManipulationCompleted(object sender, ManipulationCompletedEventArgs e)
        {
            m_Zoom = ImageControl.Width / m_Width;
        }

        private void ResetZoom()
        {
            // ensure there is an edit image. Could be NULL when the image could not be openend succesfully.
            // BugSense: 16.12.14
            //           Photo Note (1.0.0.0): Object reference not set to an instance of an object.
            if (HasNoImage())
                return;

            int newHeight;
            int newWidth;
            if (Orientation == PageOrientation.Landscape || Orientation == PageOrientation.LandscapeLeft || Orientation == PageOrientation.LandscapeRight)
            {
                newWidth = 728;
                newHeight = 480;
            }
            else
            {
                newWidth = 480;
                newHeight = 728;
            }

            ImageControl.Width = newWidth;
            ImageControl.Height = newHeight;

            if (_editImage.Width > _editImage.Height)
            {
                m_Zoom = newWidth / _editImage.Width;
            }
            else
            {
                m_Zoom = newHeight / _editImage.Height;
            }

            // Set new view port bound
            viewport.Bounds = new Rect(0, 0, newWidth, newHeight);
        }

        #endregion

        private void ImageDoubleTapped(object sender, System.Windows.Input.GestureEventArgs e)
        {
            ResetZoom();
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

            ResetZoom();
        }

        #endregion
    }
}