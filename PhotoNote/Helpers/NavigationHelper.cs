using System;
using System.Collections.Generic;
using System.Diagnostics;
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
            if (service == null)
                return;

            var uriString = string.Format("/Pages/MainPage.xaml?{0}=true", AppConstants.PARAM_CLEAR_HISTORY);

            try
            {
                service.Navigate(new Uri(uriString, UriKind.Relative));
            } 
            catch(Exception e)
            {
                Debug.WriteLine("Error:" + e.Message);
                // System.InvalidOperationException: Navigation is not allowed when the task is not in the foreground.
                // Does not really fix the problem...  :-/
            }
        }
    }
}
