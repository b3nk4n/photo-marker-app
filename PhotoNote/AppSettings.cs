using PhoneKit.Framework.Core.Storage;
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
    /// The app internal settings
    /// </summary>
    public static class AppSettings
    {
        /// <summary>
        /// The pen color.
        /// </summary>
        public static StoredObject<Color> PenColor = new StoredObject<Color>("_pen_color_", AppConstants.DEFAULT_PEN_COLOR);

        /// <summary>
        /// The pen opacity.
        /// </summary>
        public static StoredObject<double> PenOpacity = new StoredObject<double>("_pen_opacity_", AppConstants.DEFAULT_PEN_OPACITY);

        /// <summary>
        /// The pen thickness.
        /// </summary>
        public static StoredObject<double> PenThickness = new StoredObject<double>("_pen_thickness_", AppConstants.DEFAULT_PEN_THICKNESS);

        /// <summary>
        /// The stored color history.
        /// </summary>
        public static StoredObject<List<Color>> ColorHistory = new StoredObject<List<Color>>("_color_hist_", AppConstants.DEFAULT_HISTORY_COLORS);

        /// <summary>
        /// The pen color.
        /// </summary>
        public static StoredObject<PhotoNote.DrawMode> DrawMode = new StoredObject<DrawMode>("_draw_mode_", AppConstants.DEFAULT_DRAW_MODE);

        /// <summary>
        /// The text color.
        /// </summary>
        public static StoredObject<Color> TextColor = new StoredObject<Color>("_text_color_", AppConstants.DEFAULT_TEXT_COLOR);

        /// <summary>
        /// The text size.
        /// </summary>
        public static StoredObject<double> TextSize = new StoredObject<double>("_text_size_", AppConstants.DEFAULT_TEXT_SIZE);

        /// <summary>
        /// The text opacity.
        /// </summary>
        public static StoredObject<double> TextOpacity = new StoredObject<double>("_text_opacity_", AppConstants.DEFAULT_TEXT_OPACITY);

        /// <summary>
        /// The text alignment.
        /// </summary>
        public static StoredObject<TextAlignment> TextAlignment = new StoredObject<TextAlignment>("_text_alignment_", AppConstants.DEFAULT_TEXT_ALIGNMENT);

        /// <summary>
        /// The text weight.
        /// </summary>
        public static StoredObject<FontWeight> TextWeight = new StoredObject<FontWeight>("_text_weight_", AppConstants.DEFAULT_TEXT_WEIGHT);

        /// <summary>
        /// The text style.
        /// </summary>
        public static StoredObject<FontStyle> TextStyle = new StoredObject<FontStyle>("_text_style_", AppConstants.DEFAULT_TEXT_STYLE);

        /// <summary>
        /// The text style.
        /// </summary>
        public static StoredObject<FontFamily> TextFont = new StoredObject<FontFamily>("_text_font_", AppConstants.DEFAULT_TEXT_FONT);

        /// <summary>
        /// The text border.
        /// </summary>
        public static StoredObject<bool> TextBorder = new StoredObject<bool>("_text_border_", AppConstants.DEFAULT_TEXT_BORDER);

        /// <summary>
        /// The text background border.
        /// </summary>
        public static StoredObject<bool> TextBackgroundBorder = new StoredObject<bool>("_text_backborder_", AppConstants.DEFAULT_TEXT_BACKGROUND_BORDER);
    }
}
