using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using PhotoNote.Resources;
using PhotoNote.Helpers;
using PhotoNote.Model;
using System.Windows.Media.Imaging;
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
                if (_editImage != null)
                    _editImage.Share();
            };
            ApplicationBar.Buttons.Add(appBarShareButton);

            // edit
            ApplicationBarIconButton appBarPenButton = new ApplicationBarIconButton(new Uri("/Assets/AppBar/appbar.edit.png", UriKind.Relative));
            appBarPenButton.Text = AppResources.Edit;
            appBarPenButton.Click += (s, e) =>
            {
                // TODO implement link to edit ... clear back stack after that???? what happens if not?
            };
            ApplicationBar.Buttons.Add(appBarPenButton);

            // image info (photo info)
            ApplicationBarMenuItem appBarPhotoInfoMenuItem = new ApplicationBarMenuItem(AppResources.ShowPhotoInfo);
            appBarPhotoInfoMenuItem.Click += async (s, e) =>
            {
                if (!await LauncherHelper.LaunchPhotoInfoAsync(_editImage.Name))
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
                    return;
                }
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
                    ImageControl.Source = null;
                    _editImage = null;
                    return false;
                }

                img.SetSource(imageStream);

                //UpdateImageOrientationAndScale();

                ImageControl.Source = img;
            }
            return true;
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
            ImageControl.Stretch = Stretch.UniformToFill;
            viewport.Bounds = new Rect(0, 0, ImageControl.ActualWidth, ImageControl.ActualHeight);
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

        #endregion
    }
}