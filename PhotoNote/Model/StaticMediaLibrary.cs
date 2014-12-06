using Microsoft.Xna.Framework.Media;

namespace PhotoNote.Model
{
    /// <summary>
    /// Global access to the media library.
    /// </summary>
    public static class StaticMediaLibrary
    {
        private static MediaLibrary _mediaLibrary;

        /// <summary>
        /// Gets the media library
        /// </summary>
        public static MediaLibrary Instance
        {
            get
            {
                if (_mediaLibrary == null || _mediaLibrary.IsDisposed)
                    _mediaLibrary = new MediaLibrary();
                return _mediaLibrary;
            }
        }
    }
}
