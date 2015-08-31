using System.Windows;
using System.Windows.Media;

namespace PhotoNote.Helpers
{
    /// <summary>
    /// Helper class for convertsion of string to font properties and vice versa.
    /// </summary>
    public static class FontHelper
    {
        public const string FONT_NORMAL = "Normal";
        public const string FONT_ITALIC = "Italic";
        public const string FONT_BOLD = "Bold";

        public static string GetString(FontStyle style)
        {
            if (style == FontStyles.Italic)
                return FONT_ITALIC;
            else
                return FONT_NORMAL;
        }

        public static FontStyle GetStyle(string style)
        {
            if (style == FONT_ITALIC)
                return FontStyles.Italic;
            else
                return FontStyles.Normal;
        }

        public static string GetString(FontWeight weight)
        {
            if (weight == FontWeights.Bold)
                return FONT_BOLD;
            else
                return FONT_NORMAL;
        }

        public static FontWeight GetWeight(string weight)
        {
            if (weight == FONT_BOLD)
                return FontWeights.Bold;
            else
                return FontWeights.Normal;
        }

        public static string GetString(FontFamily font)
        {
            return font.Source;
        }

        public static FontFamily GetFont(string font)
        {
            if (string.IsNullOrEmpty(font))
                return AppConstants.DEFAULT_TEXT_FONT;

            return new FontFamily(font);
        }
    }
}
