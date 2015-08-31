using PhoneKit.Framework.Core.Themeing;

namespace PhotoNote.ViewModel
{
    public class ThemedImageSourceViewModel : ThemedImageSourceBase
    {
        public ThemedImageSourceViewModel()
            : base("/Assets/Images")
        {
        }

        public string PinchImage
        {
            get
            {
                return GetImagePath("pinch.png");
            }
        }
    }
}
