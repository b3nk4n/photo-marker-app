using System;
using System.Collections.Generic;
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
using PhotoNote.Model;

namespace PhotoNote.Pages
{
    public partial class MainPage : PhoneApplicationPage
    {
        /// <summary>
        /// The photo chooser task.
        /// </summary>
        /// <remarks>
        /// Must be defined at class level to work properly in tombstoning.
        /// BUT: must be NON-STATIC, to ensure the completed event is NOT firing MULTIPLE TIMES!
        /// </remarks>
        private readonly PhotoChooserTask photoTask = new PhotoChooserTask();

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

        /// <summary>
        /// Indicates the image was selected by the chooser, so that no images
        /// have to be loaded.
        /// </summary>
        private bool _imageWasSelectedByChooser;

        /// <summary>
        /// A random number generator for file names.
        /// </summary>
        private readonly Random _random = new Random();

        // Konstruktor
        public MainPage()
        {
            InitializeComponent();

            Loaded += (s, e) =>
            {
                if (_mainViewModel != null && !_imageWasSelectedByChooser)
                    _mainViewModel.Update();
                
                if (!_imageInAnimationPlayed)
                {
                    HideAllImages();
                    ImagesInAnimation.Begin();
                    _imageInAnimationPlayed = true;
                }
            };

            BuildLocalizedApplicationBar();

            // Note: a too short delay could result in the problem that NavigationService is still NULL
            _delayedNavigaionTimer.Interval = TimeSpan.FromMilliseconds(200);
            _delayedNavigaionTimer.Tick += _delayedNavigaionTimer_Tick;

            _delayedInfoTimer.Interval = TimeSpan.FromMilliseconds(3000);
            _delayedInfoTimer.Tick += (s, e) =>
            {
                _delayedInfoTimer.Stop();

                if (_infoControl != null)
                    return;

                _infoControl = new InfoControl();
                this.LayoutRoot.Children.Insert(this.LayoutRoot.Children.Count - 1, _infoControl);
                Grid.SetRow(_infoControl, 0);
                Grid.SetRowSpan(_infoControl, 3);
            };

            // init photo chooser task
            photoTask.ShowCamera = true;
            photoTask.Completed += photoTask_Completed;

            // register startup actions
            StartupActionManager.Instance.Register(10, ActionExecutionRule.Equals, () =>
            {
                FeedbackManager.Instance.StartFirst();
            });
            StartupActionManager.Instance.Register(25, ActionExecutionRule.Equals, () =>
            {
                FeedbackManager.Instance.StartSecond();
            });

            InitializeBannerBehaviour();
        }

        void _delayedNavigaionTimer_Tick(object sender, EventArgs e)
        {
            _delayedNavigaionTimer.Stop();

            var uriString = new Uri(string.Format("/Pages/EditPage.xaml?{0}={1}", AppConstants.PARAM_SELECTED_FILE_NAME, fileNameToOpen), UriKind.Relative);
            if (NavigationService != null)
            {
                NavigationService.Navigate(uriString);
                // BUGSENSE: Object reference not set to an instance of an object.
                //           PhotoNote.Pages.MainPage.<.ctor>b__1(Object s, EventArgs e)
                // Reason: tick-delay was to low and event was fired multiple times
            }
        }

        void photoTask_Completed(object se, PhotoResult pr)
        {
            if (pr.Error != null || pr.TaskResult != TaskResult.OK)
                return;

            // block screen (just because it looks better)
            ScreenBlocker.Visibility = Visibility.Visible;

            // check for OneDrive files
            if (pr.OriginalFileName.StartsWith("C:\\Data\\SharedData\\"))
            {
                var fileName = Path.GetFileName(pr.OriginalFileName);
                var file = string.Format("{0:0000}_{1}", _random.Next(10000), fileName);

                if (StaticMediaLibrary.SaveStreamToSavedPictures(pr.ChosenPhoto, file))
                {
                    fileNameToOpen = file;
                    _delayedNavigaionTimer.Start();
                }          
            }
            else
            {
                fileNameToOpen = Path.GetFileName(pr.OriginalFileName);
                _delayedNavigaionTimer.Start();
            }

            _imageWasSelectedByChooser = true;
        }

        private void InitializeBannerBehaviour()
        {
            BannerControl.AdReceived += (s, e) =>
            {
                FallbackOfflineBanner.Visibility = Visibility.Collapsed;
            };

            if (FallbackOfflineBanner.AdvertsCount == 0)
            {
                List<AdvertData> advertList = new List<AdvertData>();
                advertList.Add(new AdvertData(new Uri("/Assets/Banner/Banner_pocketBRAIN.png", UriKind.Relative), AdvertData.ActionTypes.AppId, "ad1227e4-9f80-4967-957f-6db140dc0c90"));
                advertList.Add(new AdvertData(new Uri("/Assets/Banner/Banner_powernAPP.png", UriKind.Relative), AdvertData.ActionTypes.AppId, "92740dff-b2e1-4813-b08b-c6429df03356"));
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

            StartupActionManager.Instance.Fire(e);
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);

            if (ScreenBlocker != null) // BugSense: System.NullReferenceException: Object reference not set to an instance of an object.
                ScreenBlocker.Visibility = Visibility.Collapsed;

            _imageWasSelectedByChooser = false;
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
            ApplicationBarMenuItem appBarSettingsMenuItem = new ApplicationBarMenuItem(AppResources.SettingsTitle);
            appBarSettingsMenuItem.Click += (s, e) =>
            {
                NavigationService.Navigate(new Uri("/Pages/SettingsPage.xaml", UriKind.Relative));
            };
            ApplicationBar.MenuItems.Add(appBarSettingsMenuItem);

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