using Game.Services.Popup;

namespace Game.Shop
{
    /// <summary>The shop's popups. Each id is written once, here; the prefab behind it is in the scene's popup catalog.</summary>
    internal static class ShopPopups
    {
        /// <summary>FeatureAssets/Public/PurchasedPopup.prefab.</summary>
        internal static readonly PopupId Purchased = new PopupId("Shop/Purchased");
    }
}
