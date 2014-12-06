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

        // Konstruktor
        public MainPage()
        {
            InitializeComponent();

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
    }
}