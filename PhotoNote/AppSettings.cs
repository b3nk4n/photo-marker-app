using PhoneKit.Framework.Core.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
        public static StoredObject<Color> PenColor = new StoredObject<Color>("_pen_color_", Colors.Red);

        /// <summary>
        /// The pen opacity.
        /// </summary>
        public static StoredObject<double> PenOpacity = new StoredObject<double>("_pen_opacity_", 0.4);

        /// <summary>
        /// The pen thickness.
        /// </summary>
        public static StoredObject<double> PenThickness = new StoredObject<double>("_pen_thickness_", 10.0);
    }
}
