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
using PhotoNote.Controls;
using PhoneKit.Framework.Advertising;
using PhoneKit.Framework.Core.Collections;

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
        /// Delayed info control loading.
        /// </summary>
        private DispatcherTimer _delayedInfoTimer = new DispatcherTimer();

        /// <summary>
        /// The main view model.
        /// </summary>
        private MainViewModel _mainViewModel;

        // the info control with screenshots, which uses lazy loading to improve the startup time.
        private InfoControl _infoControl;

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
                if (_mainViewModel != null)
                    _mainViewModel.Update();
                
                if (!_imageInAnimationPlayed)
                {
                    HideAllImages();
                    ImagesInAnimation.Begin();
                    _imageInAnimationPlayed = true;
                }
            };

            BuildLocalizedApplicationBar();

            _delayedNavigaionTimer.Interval = TimeSpan.FromMilliseconds(10);
            _delayedNavigaionTimer.Tick += (s, e) =>
            {
                _delayedNavigaionTimer.Stop();

                var uriString = new Uri(string.Format("/Pages/EditPage.xaml?{0}={1}", AppConstants.PARAM_SELECTED_FILE_NAME, fileNameToOpen), UriKind.Relative);
                NavigationService.Navigate(uriString);
            };

            _delayedInfoTimer.Interval = TimeSpan.FromMilliseconds(3000);
            _delayedInfoTimer.Tick += (s, e) =>
            {
                _delayedInfoTimer.Stop();

                if (_infoControl != null)
                    return;

                _infoControl = new InfoControl();
                //this.LayoutRoot.Children.Add(_infoControl);
                this.LayoutRoot.Children.Insert(this.LayoutRoot.Children.Count - 2, _infoControl);
                Grid.SetRow(_infoControl, 0);
                Grid.SetRowSpan(_infoControl, 3);
            };

            // init photo chooser task
            photoTask.ShowCamera = true;
            photoTask.Completed += (se, pr) =>
            {
                if (pr.Error != null || pr.TaskResult != TaskResult.OK)
                    return;

                // block screen (just because it looks better)
                ScreenBlocker.Visibility = Visibility.Visible;

                fileNameToOpen = Path.GetFileName(pr.OriginalFileName);
                _delayedNavigaionTimer.Start();
            };

            InitializeBannerBehaviour();
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
                try
                {
                    task.Show();
                }
                catch (InvalidOperationException)
                {
                    // BugSense: Not allowed to call Show() multiple times before an invocation returns.
                }
            };

            if (FallbackOfflineBanner.AdvertsCount == 0)
            {
                List<AdvertData> advertList = new List<AdvertData>();
                advertList.Add(new AdvertData(new Uri("/Assets/Banner/Banner_Bash0r.png", UriKind.Relative), AdvertData.ActionTypes.AppId, "e43a2937-b0e2-461e-92de-cf33c1360f73"));
                advertList.Add(new AdvertData(new Uri("/Assets/Banner/Banner_PhotoInfo.png", UriKind.Relative), AdvertData.ActionTypes.AppId, "ac39aa30-c9b1-4dc6-af2d-1cc17d9807cc"));
                advertList.Add(new AdvertData(new Uri("/Assets/Banner/Banner_pocketBRAIN.png", UriKind.Relative), AdvertData.ActionTypes.AppId, "ad1227e4-9f80-4967-957f-6db140dc0c90"));
                advertList.Add(new AdvertData(new Uri("/Assets/Banner/Banner_powernAPP.png", UriKind.Relative), AdvertData.ActionTypes.AppId, "92740dff-b2e1-4813-b08b-c6429df03356"));
                advertList.Add(new AdvertData(new Uri("/Assets/Banner/Banner_ScribbleHunter.png", UriKind.Relative), AdvertData.ActionTypes.AppId, "ed250596-e670-4d22-aee1-8ed0a08c411f"));
                advertList.ShuffleList();

                foreach (var advert in advertList)
                {
                    FallbackOfflineBanner.AddAdvert(advert);
                }

                FallbackOfflineBanner.Start();
            }
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            if (_mainViewModel == null)
            {
                _mainViewModel = new MainViewModel(NavigationService);
                DataContext = _mainViewModel;
            }

            if (NavigationContext.QueryString.ContainsKey(AppConstants.PARAM_CLEAR_HISTORY))
            {
                // clear back-history
                while (NavigationService.CanGoBack)
                    NavigationService.RemoveBackEntry();
            }

            if (e.NavigationMode == NavigationMode.Back)
            {
                if (_imageInAnimationPlayed) {
                    HideAllImages();
                    ImagesInAnimation.Begin();
                }
            }

            if (InAppPurchaseHelper.IsProductActive(AppConstants.IAP_PREMIUM_VERSION))
            {
                BannerContainer.Visibility = System.Windows.Visibility.Collapsed;
            }
            else
            {
                BannerContainer.Visibility = System.Windows.Visibility.Visible;
            }

            // show info button
            if (StartupActionManager.Instance.Count <= 10)
            {
                 _delayedInfoTimer.Start();
            }

            bool res = _mainViewModel.CheckHasAnyPicture();
            EmptyButton.Visibility = (!res) ? Visibility.Visible : Visibility.Collapsed;
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);

            ScreenBlocker.Visibility = Visibility.Collapsed;
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
                ShowPhotoTask();
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
            ShowPhotoTask();
        }

        private void ShowPhotoTask()
        {
            try
            {
                photoTask.Show();
            }
            catch (InvalidOperationException)
            {
                // BugSense: Not allowed to call Show() multiple times before an invocation returns.
                // suppress multiple Show() calls:
                // reported via Email error report (24.11.2014)
            }
        }

        protected override void OnBackKeyPress(System.ComponentModel.CancelEventArgs e)
        {

            if (_infoControl != null)
                e.Cancel = _infoControl.HandleBack();

            base.OnBackKeyPress(e);
        }

        private void AdCloseTapped(object sender, System.Windows.Input.GestureEventArgs e)
        {
            NavigationService.Navigate(new Uri("/Pages/InAppStorePage.xaml", UriKind.Relative));
        }
    }
}