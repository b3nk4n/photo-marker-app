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
        public static readonly FontItemViewModel Default = new FontItemViewModel("Default", string.Empty);

        public static readonly FontItemViewModel Arial = new FontItemViewModel("Arial", "Arial");

        public static readonly FontItemViewModel BlackJack = new FontItemViewModel("BlackJack", "/Assets/Fonts/black_jack.ttf#BlackJack");

        public static readonly FontItemViewModel ComicRelief = new FontItemViewModel("Comic Relief", "/Assets/Fonts/ComicRelief.ttf#Comic Relief");

        public static readonly FontItemViewModel Calibri = new FontItemViewModel("Calibri", "Calibri");

        public static readonly FontItemViewModel TimesNewRoman = new FontItemViewModel("Times New Roman", "Times New Roman");

        public static readonly FontItemViewModel Tahoma = new FontItemViewModel("Tahoma", "Tahoma");

        private static readonly IEnumerable<FontItemViewModel> FontCollection = new List<FontItemViewModel>() {
                    Default,
                    Arial,
                    BlackJack,
                    Calibri,
                    ComicRelief,
                    Tahoma,
                    TimesNewRoman
                };

        public IEnumerable<FontItemViewModel> Fonts
        {
            get {
                return FontCollection;
            }
        }

        public static FontItemViewModel GetItemByFont(FontFamily font)
        {
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
