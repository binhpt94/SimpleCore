using System;

namespace Game.PopupRevive
{
    /// <summary>
    /// The revive popup's view, found on the popup root through IPopupViews. Shows what the presenter gives it
    /// and forwards taps; it decides nothing.
    /// </summary>
    internal interface IRevivePopupView
    {
        /// <summary>Raised when the player taps "Xem quảng cáo" while the button takes taps.</summary>
        event Action WatchTapped;

        /// <summary>Raised when the player taps "Kết thúc lượt" while the button takes taps.</summary>
        event Action EndTapped;

        void SetTexts(string title, string gain, string waiting, string watch, string end);

        /// <summary>
        /// True: show the waiting line, dim both buttons and stop them taking taps.
        /// False: hide the waiting line, buttons bright and tappable.
        /// </summary>
        void SetWaiting(bool waiting);
    }
}
