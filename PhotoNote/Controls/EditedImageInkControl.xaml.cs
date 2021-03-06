using System.Collections.Generic;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Ink;
using System.Windows.Media;

namespace PhotoNote.Controls
{
    public partial class EditedImageInkControl : UserControl
    {
        public EditedImageInkControl(BitmapSource bitmapSource, StrokeCollection strokeCollection, IList<TextBoxContext> textCollection, double scale)
        {
            InitializeComponent();

            if (bitmapSource == null)
                return;

            SetBackgroundImage(bitmapSource);
            SetText(textCollection, bitmapSource.PixelWidth, bitmapSource.PixelHeight, scale);
            SetInk(strokeCollection, bitmapSource.PixelWidth, bitmapSource.PixelHeight, scale);
        }

        /// <summary>
        /// Sets the background image.
        /// </summary>
        /// <param name="bitmapSource">The image source.</param>
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

        /// <summary>
        /// Sets the ink.
        /// </summary>
        /// <param name="strokeCollection">The stroke data.</param>
        /// <param name="width">The area width.</param>
        /// <param name="height">The area height.</param>
        /// <param name="scale">The scale factor.</param>
        private void SetInk(StrokeCollection strokeCollection, double width, double height, double scale)
        {
            // strokes
            InkControl.Width = width;
            InkControl.Height = height;
            InkControl.RenderTransform = new ScaleTransform
            {
                ScaleX = scale,
                ScaleY = scale
            };

            // add data
            InkControl.Strokes = strokeCollection;
        }

        /// <summary>
        /// Sets the texts.
        /// </summary>
        /// <param name="textCollection">The text context collection</param>
        /// <param name="width">The area width.</param>
        /// <param name="height">The area height.</param>
        /// <param name="scale">The scale factor.</param>
        private void SetText(IList<TextBoxContext> textCollection, double width, double height, double scale)
        {
            EditTextControl.Width = width;
            EditTextControl.Height = height;
            EditTextControl.RenderTransform = new CompositeTransform
            {
                ScaleX = scale,
                ScaleY = scale,
            };

            // add data
            this.UpdateLayout();
            foreach (var textBoxContext in textCollection)
            {
                var textbox = new ExtendedTextBox(textBoxContext);
                textbox.DataContext = new object();
                textbox.IsEnabled = false;
                textbox.IsActive = false;
                textbox.RotationAngle = textBoxContext.RotationAngle;
                EditTextControl.Children.Add(textbox);
                textbox.UpdateLayout();
            }
            this.UpdateLayout();
        }
    }
}
