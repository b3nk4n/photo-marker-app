using System.Windows.Navigation;
using Microsoft.Phone.Controls;

namespace PhotoNote.Pages
{
    public partial class SettingsPage : PhoneApplicationPage
    {
        public SettingsPage()
        {
            InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            TouchHoldToggle.IsChecked = AppSettings.TouchHoldPenOptimization.Value;
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);

            AppSettings.TouchHoldPenOptimization.Value = TouchHoldToggle.IsChecked.Value;
        }
    }
}