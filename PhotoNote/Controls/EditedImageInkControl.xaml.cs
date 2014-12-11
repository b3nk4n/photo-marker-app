using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using System.Windows.Media.Imaging;

namespace PhotoNote.Controls
{
    public partial class EditedImageInkControl : UserControl
    {
        public EditedImageInkControl(BitmapSource bitmapSource)
        {
            InitializeComponent();
            SetBackgroundImage(bitmapSource);
        }

        /// <summary>
        /// Updates the attached image from the models image path.
        /// </summary>
        /// <remarks>
        /// Binding the image URI or path didn't work when the image is located in isolated storage,
        /// so we do it now this way manuelly.
        /// </remarks>
        /// <param name="note">The current note view model.</param>
        private void SetBackgroundImage(BitmapSource bitmapSource)
        {
            // check if the default image should be used.
            if (bitmapSource == null)
                return;

            this.Width = bitmapSource.PixelWidth;
            this.Height = bitmapSource.PixelHeight;
            BackgroundImage.Width = bitmapSource.PixelWidth;
            BackgroundImage.Height = bitmapSource.PixelHeight;
            BackgroundImage.Source = bitmapSource;
        }
    }
}
