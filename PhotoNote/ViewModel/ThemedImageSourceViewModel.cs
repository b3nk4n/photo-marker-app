using PhoneKit.Framework.Core.Themeing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
