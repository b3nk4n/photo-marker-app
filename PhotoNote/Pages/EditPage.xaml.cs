using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using PhoneKit.Framework.Core.Storage;
using PhoneKit.Framework.Core.Tile;

namespace PhotoNote.Pages
{
    public partial class EditPage : PhoneApplicationPage
    {
        public EditPage()
        {
            InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            // query string lookup
            //bool success = false;
            if (NavigationContext.QueryString != null)
            {
                if (e.NavigationMode == NavigationMode.Back)
                {
                    BackOrTerminate();
                }

                if (NavigationContext.QueryString.ContainsKey(AppConstants.PARAM_FILE_TOKEN))
                {
                    var token = NavigationContext.QueryString[AppConstants.PARAM_FILE_TOKEN];

                    //var image = GetImageFromToken(token);
                    //if (image != null)
                    //{
                    //    if (SaveAndPinImage(image))
                    //    {
                    //        success = true;
                    //    }
                    //}
                }

                if (NavigationContext.QueryString.ContainsKey(AppConstants.PARAM_SELECTED_FILE_NAME))
                {
                    var selectedFileName = NavigationContext.QueryString[AppConstants.PARAM_SELECTED_FILE_NAME];

                    //var image = GetImageFromFileName(selectedFileName);
                    //if (image != null)
                    //{
                    //    if (SaveAndPinImage(image))
                    //    {
                    //        success = true;
                    //    }
                    //}
                }

                if (NavigationContext.QueryString.ContainsKey(AppConstants.PARAM_FILE_NAME))
                {
                    var fileName = NavigationContext.QueryString[AppConstants.PARAM_FILE_NAME];
                    //var imagePath = string.Format("{0}{1}", LiveTileHelper.SHARED_SHELL_CONTENT_PATH, fileName);

                    //if (StorageHelper.FileExists(imagePath))
                    //{
                    //    var file = await Windows.Storage.StorageFile.GetFileFromApplicationUriAsync(new Uri(StorageHelper.APPDATA_LOCAL_SCHEME + imagePath));
                    //    await Windows.System.Launcher.LaunchFileAsync(file);
                    //}
                }

                // error handling - warning and go back or exit
                //if (!success)
                //{
                //    MessageBox.Show(AppResources.MessageBoxNoImageFound, AppResources.MessageBoxWarning, MessageBoxButton.OK);
                //    BackOrTerminate();
                //    return;
                //}
            }
        }

        /// <summary>
        /// Goes back or terminates the app when the back stack is empty.
        /// </summary>
        private void BackOrTerminate()
        {
            if (NavigationService.CanGoBack)
                NavigationService.GoBack();
            else
                App.Current.Terminate();
        }
    }
}