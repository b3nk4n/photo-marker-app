using System.Windows;
using System.Windows.Controls;

namespace PhotoNote.Controls
{
    /// <summary>
    /// The internal text opacity aware text box.
    /// </summary>
    public class OpacityTextBox : TextBox
    {
        public OpacityTextBox()
        {
            TextOpacity = 1.0;
        }

        /// <summary>
        /// Obsucred opacity property.
        /// </summary>
        public new double Opacity
        {
            get
            {
                return base.Opacity;
            }
            set
            {
                // do nothing!
            }
        }

        /// <summary>
        /// Gets or sets the text opacity.
        /// </summary>
        public double TextOpacity
        {
            get { return (double)GetValue(TextOpacityProperty); }
            set { SetValue(TextOpacityProperty, value); }
        }

        // Using a DependencyProperty as the backing store for TextOpacity.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty TextOpacityProperty =
            DependencyProperty.Register("TextOpacity", typeof(double), typeof(OpacityTextBox), new PropertyMetadata(1.0));


    }
}
