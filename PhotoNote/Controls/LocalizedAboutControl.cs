using PhoneKit.Framework.Controls;
using PhotoNote.Resources;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PhotoNote.Controls
{
    /// <summary>
    /// The localized about control.
    /// </summary>
    public class LocalizedAboutControl : AboutControlBase
    {
        protected override void LocalizeContent()
        {
            // app
            ApplicationIconSource = new Uri("/Assets/Tiles/FlipCycleTileSmall.png", UriKind.Relative);
            ApplicationTitle = AppResources.ApplicationTitle;
            ApplicationVersion = AppResources.ApplicationVersion;
            ApplicationAuthor = AppResources.ApplicationAuthor;
            ApplicationDescription = AppResources.ApplicationDescription;

            // buttons
            SupportAndFeedbackText = AppResources.SupportAndFeedback;
            SupportAndFeedbackEmail = "apps@bsautermeister.de";
            PrivacyInfoText = AppResources.PrivacyInfo;
            PrivacyInfoLink = "http://bsautermeister.de/privacy.php";
            RateAndReviewText = AppResources.RateAndReview;
            MoreAppsText = AppResources.MoreApps;
            MoreAppsSearchTerms = "Benjamin Sautermeister";

            // contributors
            ContributorsListVisibility = System.Windows.Visibility.Visible;

            SetContributorsList(new List<ContributorModel>
            {
                new ContributorModel("/Assets/Languages/spanish.png", "Atteneri Cimadevilla"),
                new ContributorModel("/Assets/Languages/italiano.png", "Antonio")
            });
        }
    }
}
