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
using Microsoft.Xna.Framework;
using System.Windows.Input;

namespace PhotoNote.Controls
{
    /// <summary>
    /// The extended TextBox control.
    /// </summary>
    public partial class ExtendedTextBox : UserControl
    {
        /// <summary>
        /// The is active value, which is visuallized by the dotted outline.
        /// </summary>
        private bool _isActive;

        /// <summary>
        /// Creates a default instance.
        /// </summary>
        public ExtendedTextBox()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Ceates an instance by the given context.
        /// </summary>
        /// <param name="context">The text context</param>
        public ExtendedTextBox(TextContext context)
            : this()
        {
            InitializeComponent();
            SetContext(context);
        }

        /// <summary>
        /// Ceates an instance by the given text box context.
        /// </summary>
        /// <param name="context">The text box context</param>
        public ExtendedTextBox(TextBoxContext context)
            : this()
        {
            InitializeComponent();
            SetTextBoxContext(context);
        }

        /// <summary>
        /// Sets the context of the text box.
        /// </summary>
        /// <param name="context">The text context.</param>
        public void SetContext(TextContext context)
        {
            TextAlignment = context.Alignment;
            FontWeight = context.Weight;
            FontStyle = context.Style;
            FontFamily = context.Font;
            FontSize = context.Size;
            TextOpacity = context.Opacity;
            HasBorder = context.HasBorder;
            HasBackgroundBorder = context.HasBackgroundBorder;
            Foreground = new SolidColorBrush(context.Color);
        }

        /// <summary>
        /// Sets the full context of the text box.
        /// </summary>
        /// <param name="context">The text context.</param>
        public void SetTextBoxContext(TextBoxContext context)
        {
            Text = context.Text;
            X = context.X;
            Y = context.Y;
            SetContext(context.Context);
        }

        /// <summary>
        /// Gets the text context.
        /// </summary>
        /// <returns>The text context.</returns>
        public TextContext GetContext()
        {
            return new TextContext(TextAlignment, FontWeight, FontStyle, FontFamily,
                FontSize, TextOpacity, ForegroundColor, HasBorder, HasBackgroundBorder);
        }

        /// <summary>
        /// Gets the text box context.
        /// </summary>
        /// <returns>The text box context.</returns>
        public TextBoxContext GetTextBoxContext()
        {
            return new TextBoxContext(Text, X, Y, GetContext());
        }

        /// <summary>
        /// Puts the cursor to the end.
        /// </summary>
        public void SelectLast()
        {
            TextControl.Select(Text.Length, 0);
        }

        /// <summary>
        /// Focuses the text box and displays the keyboard.
        /// </summary>
        public new void Focus()
        {
            TextControl.Focus();
        }

        /// <summary>
        /// Sets the text box position
        /// </summary>
        /// <param name="parent">The parent container</param>
        /// <param name="x">The center x coord.</param>
        /// <param name="y">The center y coord.</param>
        /// <param name="textbox">The textbox to move.</param>
        /// <returns>True, wenn new position is still in parent bounds.</returns>
        public bool SetTextBoxPosition(Canvas parent, double x, double y)
        {
            const int OUTER_DELTA = 12;
            // verify the text box stays in image bounds
            var top = y - this.ActualHeight / 2;
            var left = x - this.ActualWidth / 2;
            var tbBounds = new Rectangle((int)left, (int)top, (int)this.ActualWidth, (int)this.ActualHeight);
            var parentBounds = new Rectangle(OUTER_DELTA, OUTER_DELTA, (int)parent.ActualWidth - 2 * OUTER_DELTA, (int)parent.ActualHeight - 2 * OUTER_DELTA);
            var inBounds = tbBounds.Intersects(parentBounds);

            // set position
            X = left;
            Y = top;
            parent.UpdateLayout();

            return inBounds;
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

        private System.Windows.Media.Color ForegroundColor
        {
            get
            {
                var solidColor = TextControl.Foreground as SolidColorBrush;
                if (solidColor != null)
                    return solidColor.Color;
                else
                    return new System.Windows.Media.Color();
            }
        }

        /// <summary>
        /// Gets or sets the read only property.
        /// </summary>
        public bool IsActive
        {
            get 
            {
                return _isActive;
            }
            set 
            {
                _isActive = value;
                if (_isActive)
                    VisualStateManager.GoToState(TextControl, "Active", false);
                else
                    VisualStateManager.GoToState(TextControl, "Inactive", false);
            }
        }

        /// <summary>
        /// Gets the x coordinate (LEFT).
        /// </summary>
        public double X
        {
            get
            {
                return Canvas.GetLeft(this);
            }
            set
            {
                Canvas.SetLeft(this, value);
            }
        }

        /// <summary>
        /// Gets the Y coordinate (TOP).
        /// </summary>
        public double Y
        {
            get
            {
                return Canvas.GetTop(this);
            }
            set
            {
                Canvas.SetTop(this, value);
            }
        }

        /// <summary>
        /// Indicates whether the internal text control has the focus.
        /// </summary>
        public bool HasFocus
        {
            get
            {
                return FocusManager.GetFocusedElement() == TextControl;
            }
        }
    }
}
