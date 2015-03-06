using PhotoNote.Helpers;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;

namespace PhotoNote
{
    /// <summary>
    /// Global app constants.
    /// </summary>
    public static class AppConstants
    {
        /// <summary>
        /// Premium version IAP key.
        /// </summary>
        public const string IAP_PREMIUM_VERSION = "photoMarker_premium";

        /// <summary>
        /// The placeholder string.
        /// </summary>
        public const string PARAM_FILE_TOKEN = "token";

        /// <summary>
        /// The selected file name.
        /// </summary>
        public const string PARAM_SELECTED_FILE_NAME = "selectedFileName";

        /// <summary>
        /// The clear history indicator.
        /// </summary>
        public const string PARAM_CLEAR_HISTORY = "clearHistory";

        /// <summary>
        /// The photo prefix to recognize edited photos in the library.
        /// </summary>
        public const string IMAGE_PREFIX = "PhotoNote_";

        /// <summary>
        /// The size of the color history.
        /// </summary>
        public const int COLOR_HISTORY_SIZE = 6;

        /// <summary>
        /// The maximum marker size.
        /// </summary>
        public const double MAX_MARKER_SIZE = 42;

        // PEN DEFAULTS
        public static readonly Color DEFAULT_PEN_COLOR = Colors.Red;
        public const double DEFAULT_PEN_OPACITY = 0.35;
        public const double DEFAULT_PEN_THICKNESS = 14.0;
        public static readonly List<Color> DEFAULT_HISTORY_COLORS = new List<Color> { Colors.Red, Colors.Green, Colors.Blue, Colors.Yellow, Colors.Black, Colors.White };
        public const DrawMode DEFAULT_DRAW_MODE = PhotoNote.DrawMode.Normal;

        // TEXT DEFAULTS
        public const TextAlignment DEFAULT_TEXT_ALIGNMENT = TextAlignment.Left;
        public static readonly FontWeight DEFAULT_TEXT_WEIGHT = FontWeights.Normal;
        public static readonly string DEFAULT_TEXT_WEIGHT_STRING = FontHelper.FONT_NORMAL;
        public static readonly FontStyle DEFAULT_TEXT_STYLE = FontStyles.Normal;
        public static readonly string DEFAULT_TEXT_STYLE_STRING = FontHelper.FONT_NORMAL;
        public static readonly FontFamily DEFAULT_TEXT_FONT = new FontFamily(DEFAULT_TEXT_FONT_STRING);
        public static readonly string DEFAULT_TEXT_FONT_STRING = "Portable User Interface";
        public const double DEFAULT_TEXT_SIZE = 48.0;
        public const double DEFAULT_TEXT_OPACITY = 1.0;
        public static readonly Color DEFAULT_TEXT_COLOR = Colors.Red;
        public const bool DEFAULT_TEXT_BORDER = false;
        public const bool DEFAULT_TEXT_BACKGROUND_BORDER = false;
    }
}
