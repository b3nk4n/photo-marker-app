namespace PhotoNote
{
    public class TextBoxContext
    {
        /// <summary>
        /// The content text.
        /// </summary>
        public string Text { get; set; }

        /// <summary>
        /// Gets or sets the X position (LEFT).
        /// </summary>
        public double X { get; set; }

        /// <summary>
        /// Gets or sets the Y position (TOP).
        /// </summary>
        public double Y { get; set; }

        /// <summary>
        /// Gets or sets the text rotation angle.
        /// </summary>
        public int RotationAngle { get; set; }

        /// <summary>
        /// Gets the context.
        /// </summary>
        public TextContext Context{ get; set; }

        /// <summary>
        /// Creates the default text context.
        /// </summary>
        public TextBoxContext()
            : this(string.Empty, 0.0, 0.0, 0, new TextContext())
        {
        }

        /// <summary>
        /// Creates a custom text context.
        /// </summary>
        public TextBoxContext(string text, double x, double y, int rotationAngle, TextContext context)
        {
            Text = text;
            X = x;
            Y = y;
            RotationAngle = rotationAngle;
            Context = context;
        }
    }
}
