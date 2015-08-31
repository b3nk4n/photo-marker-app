using PhoneKit.Framework.Controls;
using PhotoNote.Resources;
using System;
using System.Collections.Generic;

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
                new ContributorModel("/Assets/Images/icon.png", "Jeff Portaro (The Noun Project)"),
                new ContributorModel("/Assets/Languages/spanish.png", "Atteneri Cimadevilla"),
                new ContributorModel("/Assets/Languages/italiano.png", "Antonio"),
                new ContributorModel("/Assets/Languages/french.png", "Bernard Monon"),
                new ContributorModel("/Assets/Languages/russia.png", "Александр Чуркин"),
                new ContributorModel("/Assets/Languages/arabic.png", "Rafael Yousuf"),
                new ContributorModel("/Assets/Languages/persian.png", "Mahmud Karimi"),
                new ContributorModel("/Assets/Languages/vietnam.png", "Dương Duy Nhật"),
                new ContributorModel("/Assets/Languages/turkish.png", "Mücahit Çakır")
            });
        }
    }
}
