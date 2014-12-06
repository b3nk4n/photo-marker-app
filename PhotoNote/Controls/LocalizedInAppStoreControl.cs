﻿using PhoneKit.Framework.Controls;
using PhotoNote.Resources;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace PhotoNote.Controls
{
    public class LocalizedInAppStoreControl : InAppStoreControlBase
    {
        public LocalizedInAppStoreControl()
        {
        }

        /// <summary>
        /// Localizes the user control content and texts.
        /// </summary>
        protected override void LocalizeContent()
        {
            InAppStoreLoadingText = AppResources.InAppStoreLoading;
            InAppStoreNoProductsText = AppResources.InAppStoreNoProducts;
            InAppStorePurchasedText = AppResources.InAppStorePurchased;
            SupportedProductIds = AppConstants.IAP_PREMIUM_VERSION;
        }
    }
}
