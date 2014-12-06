using System;

namespace PhotoNote
{
    /// <summary>
    /// Global app constants.
    /// </summary>
    public static class AppConstants
    {
        /// <summary>
        /// The placeholder string.
        /// </summary>
        public const string PARAM_FILE_TOKEN = "token";

        /// <summary>
        /// The file name.
        /// </summary>
        public const string PARAM_FILE_NAME = "fileName";

        /// <summary>
        /// The selected file name.
        /// </summary>
        public const string PARAM_SELECTED_FILE_NAME = "selectedFileName";

        /// <summary>
        /// The photo prefix to recognize edited photos in the library.
        /// </summary>
        public const string IMAGE_PREFIX = "PhotoNote_";
    }
}
