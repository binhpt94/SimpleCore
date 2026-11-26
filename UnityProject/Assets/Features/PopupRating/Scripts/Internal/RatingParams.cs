using Game.Services.Popup;
using UnityEngine;

namespace Game.PopupRating
{
    /// <summary>The popups this feature owns. Each id is declared once, here.</summary>
    internal static class RatingPopupIds
    {
        /// <summary>The rating invite (prefab PopupRatingMain in FeatureAssets/Public, listed in the Game scene's popup catalog).</summary>
        internal static readonly PopupId Main = new PopupId("PopupRating/Main");
    }

    /// <summary>Where the game's Google Play page is tried, in order (InviteParams.store_open_order).</summary>
    internal enum StoreOpenOrder
    {
        /// <summary>The Google Play app first; when it cannot open the page, a browser.</summary>
        AppThenBrowser,
    }
}
