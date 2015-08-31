using System.Windows;
using System.Windows.Controls;

namespace PhotoNote.Controls
{
    public partial class InfoControl : UserControl
    {
        private bool _isInfoVisible;

        public InfoControl()
        {
            InitializeComponent();

            Loaded += (s, e) =>
            {
                ShowSliderButton.Begin();
            };
        }

        /// <summary>
        /// Handles the back button pressed event.
        /// </summary>
        /// <returns>Returs the cancel value.</returns>
        public bool HandleBack()
        {
            if (_isInfoVisible)
            {
                VisualStateManager.GoToState(this, "NormalState", true);
                _isInfoVisible = false;
                return true;
            }
            return false;
        }

        private void InfoArrowClicked(object sender, RoutedEventArgs e)
        {
            _isInfoVisible = !_isInfoVisible;

            if (_isInfoVisible)
            {
                VisualStateManager.GoToState(this, "InfoState", true);
            }
            else
            {
                VisualStateManager.GoToState(this, "NormalState", true);
            }
        }
    }
}
