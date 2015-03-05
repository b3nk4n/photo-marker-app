using PhoneKit.Framework.Core.MVVM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace PhotoNote.ViewModel
{
    /// <summary>
    /// The fonts view model, which is reqired for the list picker full page view.
    /// </summary>
    public class FontsViewModel : ViewModelBase
    {
        public static readonly FontItemViewModel DEFAULT = new FontItemViewModel("Default", string.Empty);

        public static readonly FontItemViewModel ARIAL = new FontItemViewModel("Arial", "Arial");

        public static readonly FontItemViewModel BLACK_JACK = new FontItemViewModel("BlackJack", "/Assets/Fonts/black_jack.ttf#BlackJack");

        public static readonly FontItemViewModel COMIC_RELIEF = new FontItemViewModel("Comic Relief", "/Assets/Fonts/ComicRelief.ttf#Comic Relief");

        public static readonly FontItemViewModel CALIBRI = new FontItemViewModel("Calibri", "Calibri");

        public static readonly FontItemViewModel TIMES_NEW_ROMAN = new FontItemViewModel("Times New Roman", "Times New Roman");

        public static readonly FontItemViewModel TAHOMA = new FontItemViewModel("Tahoma", "Tahoma");

        private static readonly IEnumerable<FontItemViewModel> FontCollection = new List<FontItemViewModel>() {
                    DEFAULT,
                    ARIAL,
                    BLACK_JACK,
                    CALIBRI,
                    COMIC_RELIEF,
                    TAHOMA,
                    TIMES_NEW_ROMAN
                };

        public IEnumerable<FontItemViewModel> Fonts
        {
            get {
                return FontCollection;
            }
        }

        public static FontItemViewModel GetItemByFont(FontFamily font)
        {
            // default fallback
            if (font == null)
                return DEFAULT;

            foreach (var f in FontCollection)
            {
                if (f.Font.Equals(font))
                {
                    return f;
                }
            }

            return null;
        }
    }
}
