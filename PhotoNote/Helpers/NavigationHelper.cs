using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Navigation;

namespace PhotoNote.Helpers
{
    public static class NavigationHelper
    {
        public static void BackToMainPageWithHistoryClear(NavigationService service)
        {
            var uriString = string.Format("/Pages/MainPage.xaml?{0}=true", AppConstants.PARAM_CLEAR_HISTORY);
            service.Navigate(new Uri(uriString, UriKind.Relative));
        }
    }
}
