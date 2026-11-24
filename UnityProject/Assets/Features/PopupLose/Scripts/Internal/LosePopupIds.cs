using Game.Services.Popup;

namespace Game.PopupLose
{
    /// <summary>The popups this feature owns. Each id is declared once, here.</summary>
    internal static class LosePopupIds
    {
        /// <summary>The lose popup (prefab LoseMain in FeatureAssets/Public, listed in the Game scene's popup catalog).</summary>
        internal static readonly PopupId Main = new PopupId("PopupLose/Main");
    }
}
