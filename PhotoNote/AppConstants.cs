using System;

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
    }
}
