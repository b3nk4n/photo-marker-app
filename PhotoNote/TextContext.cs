using PhotoNote.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace PhotoNote
{
    /// <summary>
    /// The text context.
    /// </summary>
    public class TextContext
    {
        /// <summary>
        /// Gets or sets the text alignment.
        /// </summary>
        public TextAlignment Alignment { get; set; }

        /// <summary>
        /// Gets or sets the text weight.
        /// </summary>
        public FontWeight Weight { get; set; }

        /// <summary>
        /// Gets or sets the text style.
        /// </summary>
        public FontStyle Style { get; set; }

        /// <summary>
        /// Gets or sets the text family.
        /// </summary>
        public FontFamily Font { get; set; }

        /// <summary>
        /// Gets or sets the text size.
        /// </summary>
        public double Size { get; set; }

        /// <summary>
        /// Gets or sets the text opacity
        /// </summary>
        public double Opacity { get; set; }

        /// <summary>
        /// Gets or sets whether the text has a border.
        /// </summary>
        public bool HasBorder { get; set; }

        /// <summary>
        /// Gets or sets whether the text has a background border.
        /// </summary>
        public bool HasBackgroundBorder { get; set; }

        /// <summary>
        /// Gets or sets the color.
        /// </summary>
        public Color Color { get; set; }

        /// <summary>
        /// Creates the default text context.
        /// </summary>
        public TextContext()
            : this(TextAlignment.Left, FontWeights.Normal, FontStyles.Normal, FontsViewModel.DEFAULT.Font, 36.0, 1.0, Colors.Black, false, false)
        {
        }

        /// <summary>
        /// Creates a custom text context.
        /// </summary>
        public TextContext(TextAlignment alignment, FontWeight weight, FontStyle style, FontFamily font,
            double size, double opacity, Color color, bool hasBorder, bool hasBackBorder)
        {
            Alignment = alignment;
            Weight = weight;
            Style = style;
            Font = font;
            Size = size;
            Opacity = opacity;
            Color = color;
            HasBorder = hasBorder;
            HasBackgroundBorder = hasBackBorder;
        }
    }
}
