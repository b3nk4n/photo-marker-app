using System.Windows.Media;

namespace PhotoNote
{
    public class MarkerContext
    {
        /// <summary>
        /// Gets or sets the draw mode.
        /// </summary>
        public DrawMode Mode { get; set; }

        /// <summary>
        /// Gets or sets the marker size.
        /// <remarks>
        /// The stored value is linar. Use the <code>LinearToQuadraticConverter</code> to convert the value.
        /// </remarks>
        /// </summary>
        public double Size { get; set; }

        /// <summary>
        /// Gets or sets the marker opacity
        /// </summary>
        public double Opacity { get; set; }

        /// <summary>
        /// Gets or sets the color.
        /// </summary>
        public Color Color { get; set; }

        /// <summary>
        /// Creates the default text context.
        /// </summary>
        public MarkerContext()
            : this(AppConstants.DEFAULT_DRAW_MODE, AppConstants.DEFAULT_PEN_THICKNESS, AppConstants.DEFAULT_PEN_OPACITY, AppConstants.DEFAULT_PEN_COLOR)
        {
        }

        /// <summary>
        /// Creates a custom marker context.
        /// </summary>
        public MarkerContext(DrawMode mode, double size, double opacity, Color color)
        {
            Mode = mode;
            Size = size;
            Opacity = opacity;
            Color = color;
        }
    }
}
