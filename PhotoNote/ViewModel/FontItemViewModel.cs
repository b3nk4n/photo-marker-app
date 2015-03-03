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
    /// The font item view model.
    /// </summary>
    public class FontItemViewModel : ViewModelBase
    {
        /// <summary>
        /// The font family.
        /// </summary>
        public FontFamily Font { get; set; }

        /// <summary>
        /// The display name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Creates a FontItemViewModel instance.
        /// </summary>
        /// <param name="name">The display name.</param>
        /// <param name="font">The font family.</param>
        public FontItemViewModel(string name, string font)
            : this(name, new FontFamily(font))
        {
        }

        /// <summary>
        /// Creates a FontItemViewModel instance.
        /// </summary>
        /// <param name="name">The display name.</param>
        /// <param name="font">The font family.</param>
        public FontItemViewModel(string name, FontFamily font)
        {
            Name = name;
            Font = font;
        }
    }
}
