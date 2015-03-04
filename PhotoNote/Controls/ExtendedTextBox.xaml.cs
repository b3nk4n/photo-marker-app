using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using System.Windows.Media;

namespace PhotoNote.Controls
{
    /// <summary>
    /// The extended TextBox control.
    /// </summary>
    public partial class ExtendedTextBox : UserControl
    {
        public ExtendedTextBox()
        {
            InitializeComponent();
        }

        public void SelectAll()
        {
            TextControl.SelectAll();
        }

        public new void Focus()
        {
            TextControl.Focus();
        }

        /// <summary>
        /// Gets or sets the font size.
        /// </summary>
        public string Text
        {
            get { return TextControl.Text; }
            set { TextControl.Text = value; }
        }

        /// <summary>
        /// Gets or sets the font size.
        /// </summary>
        public new double FontSize
        {
            get { return TextControl.FontSize; }
            set { TextControl.FontSize = value; }
        }

        /// <summary>
        /// Gets or sets the opacity.
        /// </summary>
        public double TextOpacity
        {
            get { return TextControl.TextOpacity; }
            set { TextControl.TextOpacity = value; }
        }

        /// <summary>
        /// Gets or sets the font weight.
        /// </summary>
        public new FontWeight FontWeight
        {
            get { return TextControl.FontWeight; }
            set { TextControl.FontWeight = value; }
        }

        /// <summary>
        /// Gets or sets the font alignment.
        /// </summary>
        public TextAlignment TextAlignment
        {
            get { return TextControl.TextAlignment; }
            set { TextControl.TextAlignment = value; }
        }

        /// <summary>
        /// Gets or sets the font family.
        /// </summary>
        public new FontFamily FontFamily
        {
            get { return TextControl.FontFamily; }
            set { TextControl.FontFamily = value; }
        }

        /// <summary>
        /// Gets or sets the border.
        /// </summary>
        public bool HasBorder
        {
            get { return Border.Visibility == Visibility.Visible; }
            set { Border.Visibility = (value) ? Visibility.Visible : Visibility.Collapsed; }
        }

        /// <summary>
        /// Gets or sets the border.
        /// </summary>
        public bool HasBackgroundBorder
        {
            get { return BackgroundBorder.Visibility == Visibility.Visible; }
            set { BackgroundBorder.Visibility = (value) ? Visibility.Visible : Visibility.Collapsed; }
        }

        /// <summary>
        /// Gets or sets the foreground.
        /// </summary>
        public new Brush Foreground
        {
            get { return TextControl.Foreground; }
            set { TextControl.Foreground = value; }
        }

        /// <summary>
        /// Gets or sets the read only property.
        /// </summary>
        public bool IsActive
        {
            get 
            {
                return TextControl.IsReadOnly;
            }
            set 
            { 
                TextControl.IsReadOnly = value;
                if (value == true)
                    VisualStateManager.GoToState(TextControl, "Inactive", false);
                else
                    VisualStateManager.GoToState(TextControl, "Active", false);
            }
        }
    }
}
