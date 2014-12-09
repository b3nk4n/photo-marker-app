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
using System.Windows.Threading;
using Microsoft.Phone.Tasks;
using System.IO;
using PhoneKit.Framework.Support;
using PhoneKit.Framework.InAppPurchase;
using PhotoNote.ViewModel;

namespace PhotoNote.Pages
{
    public partial class MainPage : PhoneApplicationPage
    {
        /// <summary>
        /// The photo chooser task.
        /// </summary>
        /// <remarks>Must be defined at class level to work properly in tombstoning.</remarks>
        private static readonly PhotoChooserTask photoTask = new PhotoChooserTask();

        /// <summary>
        /// The selected file name (for delayed opening).
        /// </summary>
        private static string fileNameToOpen;

        /// <summary>
        /// Used for delayed pin, because there is an issue when we pin directly after the photo-task returns.
        /// </summary>
        private DispatcherTimer _delayedNavigaionTimer = new DispatcherTimer();

        /// <summary>
        /// The main view model.
        /// </summary>
        private MainViewModel _mainViewModel = new MainViewModel();

        /// <summary>
        /// To ensure the animation is only played once.
        /// </summary>
        private bool _imageInAnimationPlayed;

        // Konstruktor
        public MainPage()
        {
            InitializeComponent();

            Loaded += (s, e) =>
            {
                _mainViewModel.Update();
                

                if (!_imageInAnimationPlayed)
                {
                    HideAllImages();
                    ImagesInAnimation.Begin();
                    _imageInAnimationPlayed = true;
                }
            };

            BuildLocalizedApplicationBar();

            _delayedNavigaionTimer.Interval = TimeSpan.FromMilliseconds(100);
            _delayedNavigaionTimer.Tick += (s, e) =>
            {
                _delayedNavigaionTimer.Stop();

                var uriString = new Uri(string.Format("/Pages/EditPage.xaml?{0}={1}", AppConstants.PARAM_SELECTED_FILE_NAME, fileNameToOpen), UriKind.Relative);
                NavigationService.Navigate(uriString);
            };

            // init photo chooser task
            photoTask.ShowCamera = true;
            photoTask.Completed += (se, pr) =>
            {
                if (pr.Error != null || pr.TaskResult != TaskResult.OK)
                    return;

                fileNameToOpen = Path.GetFileName(pr.OriginalFileName);
                _delayedNavigaionTimer.Start();
            };

            // register startup actions
            StartupActionManager.Instance.Register(3, ActionExecutionRule.MoreThan, () =>
            {
                if (!InAppPurchaseHelper.IsProductActive(AppConstants.IAP_PREMIUM_VERSION))
                {
                    BannerContainer.Visibility = System.Windows.Visibility.Visible;
                }
            });

            InitializeBannerBehaviour();

            DataContext = _mainViewModel;
        }

        private void InitializeBannerBehaviour()
        {
            BannerControl.AdReceived += (s, e) =>
            {
                FallbackOfflineBanner.Visibility = Visibility.Collapsed;
            };

            FallbackOfflineBanner.Tap += (s, e) =>
            {
                var task = new MarketplaceDetailTask();
                task.ContentType = MarketplaceContentType.Applications;
                task.ContentIdentifier = "ac39aa30-c9b1-4dc6-af2d-1cc17d9807cc";
                task.Show();
            };
        }

        private void HideBannerForPremiumVersion()
        {
            if (InAppPurchaseHelper.IsProductActive(AppConstants.IAP_PREMIUM_VERSION))
            {
                BannerContainer.Visibility = System.Windows.Visibility.Collapsed;
            }
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            // fire startup events
            StartupActionManager.Instance.Fire(e);

            HideBannerForPremiumVersion();

            bool res = _mainViewModel.CheckHasAnyPicture();
            EmptyButton.Visibility = (!res) ? Visibility.Visible : Visibility.Collapsed;
        }

        private void HideAllImages()
        {
            b1.Opacity = b2.Opacity = b3.Opacity = b4.Opacity = b5.Opacity = b6.Opacity = 0.0;
        }

        /// <summary>
        /// Builds the localized app bar.
        /// </summary>
        private void BuildLocalizedApplicationBar()
        {
            // ApplicationBar der Seite einer neuen Instanz von ApplicationBar zuweisen
            ApplicationBar = new ApplicationBar();
            ApplicationBar.Opacity = 0.99;

            // add tile
            ApplicationBarIconButton appBarTileButton = new ApplicationBarIconButton(new Uri("/Assets/AppBar/appbar.image.select.png", UriKind.Relative));
            appBarTileButton.Text = AppResources.AppBarSelectPicture;
            appBarTileButton.Click += (s, e) =>
            {
                try
                {
                    photoTask.Show();
                }
                catch (InvalidOperationException)
                {
                    // suppress multiple Show() calls:
                    // reported via Email error report (24.11.2014)
                }

            };
            ApplicationBar.Buttons.Add(appBarTileButton);

            // in-app store
            ApplicationBarMenuItem appBarInAppStoreMenuItem = new ApplicationBarMenuItem(AppResources.InAppStoreTitle);
            appBarInAppStoreMenuItem.Click += (s, e) =>
            {
                NavigationService.Navigate(new Uri("/Pages/InAppStorePage.xaml", UriKind.Relative));
            };
            ApplicationBar.MenuItems.Add(appBarInAppStoreMenuItem);

            // about
            ApplicationBarMenuItem appBarAboutMenuItem = new ApplicationBarMenuItem(AppResources.AboutTitle);
            appBarAboutMenuItem.Click += (s, e) =>
            {
                NavigationService.Navigate(new Uri("/Pages/AboutPage.xaml", UriKind.Relative));
            };
            ApplicationBar.MenuItems.Add(appBarAboutMenuItem);
        }

        private void ChoosePhotoClicked(object sender, RoutedEventArgs e)
        {
            photoTask.Show();
        }

        #region Info Popup

        private bool _isInfoVisible = false;

        private void InfoArrowClicked(object sender, RoutedEventArgs e)
        {
            _isInfoVisible = !_isInfoVisible;

            if (_isInfoVisible)
            {
                VisualStateManager.GoToState(this, "InfoState", true);
            }
            else
            {
                VisualStateManager.GoToState(this, "NormalState", true);
            }
        }

        #endregion
    }
}