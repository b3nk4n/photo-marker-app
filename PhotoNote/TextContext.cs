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
        /// Creates the default text context.
        /// </summary>
        public TextContext()
        {
            Alignment = TextAlignment.Left;
            Weight = FontWeights.Normal;
            Style = FontStyles.Normal;
            Font = FontsViewModel.Default.Font;
            Size = 36.0;
            Opacity = 1.0;
            HasBorder = false;
            HasBackgroundBorder = false;
        }
    }
}
