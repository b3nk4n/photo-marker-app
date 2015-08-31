using PhoneKit.Framework.Core.MVVM;
using System.Collections.Generic;
using System.Windows.Media;

namespace PhotoNote.ViewModel
{
    /// <summary>
    /// The fonts view model, which is reqired for the list picker full page view.
    /// </summary>
    public class FontsViewModel : ViewModelBase
    {
        public static readonly FontItemViewModel DEFAULT = new FontItemViewModel("Default", "Portable User Interface");

        public static readonly FontItemViewModel AMETIC_SC = new FontItemViewModel("Amatic SC", "/Assets/Fonts/AmaticSC-Regular.ttf#Amatic SC");

        public static readonly FontItemViewModel ARCHISTICO = new FontItemViewModel("Archistico", "/Assets/Fonts/Archistico_Simple.ttf#Archistico");

        public static readonly FontItemViewModel ARIAL = new FontItemViewModel("Arial", "Arial");

        public static readonly FontItemViewModel BLACK_JACK = new FontItemViewModel("BlackJack", "/Assets/Fonts/black_jack.ttf#BlackJack");

        public static readonly FontItemViewModel CALIBRI = new FontItemViewModel("Calibri", "Calibri");

        public static readonly FontItemViewModel COMIC_RELIEF = new FontItemViewModel("Comic Relief", "/Assets/Fonts/ComicRelief.ttf#Comic Relief");

        public static readonly FontItemViewModel COMIC_SANS_MS = new FontItemViewModel("Comic Sans MS", "Comic Sans MS");

        public static readonly FontItemViewModel COURIER_NEW = new FontItemViewModel("Courier New", "Courier New");

        public static readonly FontItemViewModel GEORGIA = new FontItemViewModel("Georgia", "Georgia");

        public static readonly FontItemViewModel HARABARA_HAND = new FontItemViewModel("Harabara Hand", "/Assets/Fonts/HarabaraHand.ttf#HarabaraHand");

        public static readonly FontItemViewModel LACIDA_SANS = new FontItemViewModel("Lucida Sans Unicode", "Lucida Sans Unicode");

        public static readonly FontItemViewModel PACIFICO = new FontItemViewModel("Pacifico", "/Assets/Fonts/Pacifico.ttf#Pacifico");

        public static readonly FontItemViewModel SEGEO_WP_LIGHT = new FontItemViewModel("Segoe WP", "Segoe WP Light"); // when option is set to BOLD, it is like 'Segoe WP Black'

        public static readonly FontItemViewModel TAHOMA = new FontItemViewModel("Tahoma", "Tahoma");

        public static readonly FontItemViewModel TIMES_NEW_ROMAN = new FontItemViewModel("Times New Roman", "Times New Roman");

        public static readonly FontItemViewModel TREBUCHET_MS = new FontItemViewModel("Trebuchet MS", "Trebuchet MS");

        public static readonly FontItemViewModel VERDANA = new FontItemViewModel("Verdana", "Verdana");

        public static readonly FontItemViewModel REPORT_1942 = new FontItemViewModel("1942 Report", "/Assets/Fonts/1942.ttf#1942 report");

        public static readonly FontItemViewModel DUMB2 = new FontItemViewModel("2Dumb", "/Assets/Fonts/2Dumb.ttf#2Dumb");

        public static readonly FontItemViewModel DUMB3 = new FontItemViewModel("3Dumb", "/Assets/Fonts/3Dumb.ttf#3Dumb");

        private static readonly IEnumerable<FontItemViewModel> FontCollection = new List<FontItemViewModel>() {
                    DEFAULT,
                    AMETIC_SC,
                    ARCHISTICO,
                    ARIAL,
                    BLACK_JACK,
                    CALIBRI,
                    COMIC_RELIEF,
                    COMIC_SANS_MS,
                    COURIER_NEW,
                    GEORGIA,
                    HARABARA_HAND,
                    LACIDA_SANS,
                    PACIFICO,
                    SEGEO_WP_LIGHT,
                    TAHOMA,
                    TIMES_NEW_ROMAN,
                    TREBUCHET_MS,
                    VERDANA,
                    REPORT_1942,
                    DUMB2,
                    DUMB3
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
            if (font == null || string.IsNullOrEmpty(font.Source))
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
