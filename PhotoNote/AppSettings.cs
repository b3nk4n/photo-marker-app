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
        public static StoredObject<double> PenOpacity = new StoredObject<double>("_pen_opacity_", 0.35);

        /// <summary>
        /// The pen thickness.
        /// </summary>
        public static StoredObject<double> PenThickness = new StoredObject<double>("_pen_thickness_", 14.0);

        /// <summary>
        /// The stored color history.
        /// </summary>
        public static StoredObject<List<Color>> ColorHistory = new StoredObject<List<Color>>("_color_hist_", new List<Color>{ Colors.Red, Colors.Green, Colors.Blue, Colors.Yellow, Colors.Black, Colors.White });

        /// <summary>
        /// The pen color.
        /// </summary>
        public static StoredObject<PhotoNote.DrawMode> DrawMode = new StoredObject<DrawMode>("_draw_mode_", PhotoNote.DrawMode.Normal);
    }
}
