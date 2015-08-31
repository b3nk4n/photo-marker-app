using Newtonsoft.Json;
using PhotoNote.Helpers;
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
        [JsonIgnore]
        public FontWeight Weight { get; set; }

        /// <summary>
        /// Property used for serialization only.
        /// </summary>
        public string WeightSRZ
        {
            get
            {
                return FontHelper.GetString(Weight);
            }
            set
            {
                Weight = FontHelper.GetWeight(value);
            }
        }

        /// <summary>
        /// Gets or sets the text style.
        /// </summary>
        [JsonIgnore]
        public FontStyle Style { get; set; }

        /// <summary>
        /// Property used for serialization only.
        /// </summary>
        public string StyleSRZ
        {
            get
            {
                return FontHelper.GetString(Style);
            }
            set
            {
                Style = FontHelper.GetStyle(value);
            }
        }

        /// <summary>
        /// Gets or sets the text family.
        /// </summary>
        [JsonIgnore]
        public FontFamily Font { get; set; }

        /// <summary>
        /// Property used for serialization only.
        /// </summary>
        public string FontSRZ
        {
            get
            {
                return FontHelper.GetString(Font);
            }
            set
            {
                Font = FontHelper.GetFont(value);
            }
        }

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
            : this(AppConstants.DEFAULT_TEXT_ALIGNMENT, AppConstants.DEFAULT_TEXT_WEIGHT, AppConstants.DEFAULT_TEXT_STYLE,
                   AppConstants.DEFAULT_TEXT_FONT, AppConstants.DEFAULT_TEXT_SIZE, AppConstants.DEFAULT_TEXT_OPACITY,
                   AppConstants.DEFAULT_TEXT_COLOR, AppConstants.DEFAULT_TEXT_BORDER, AppConstants.DEFAULT_TEXT_BACKGROUND_BORDER)
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
