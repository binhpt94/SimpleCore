namespace Game.Services.Popup
{
    /// <summary>
    /// What the caller needs from a popup it opened: its view, to fill content and hear buttons,
    /// and whether it is the top popup, the only one that may react to input.
    /// </summary>
    public interface IPopupViews
    {
        /// <summary>
        /// Finds a component of type <typeparamref name="T"/> on the root of the open popup behind
        /// <paramref name="handle"/>. T is normally an interface the caller declares in its own Internal
        /// and implements in its View. False when the popup is not open or has no such component.
        /// </summary>
        bool TryGetView<T>(PopupHandle handle, out T view) where T : class;

        /// <summary>True while the popup behind <paramref name="handle"/> is open and on top of the stack.</summary>
        bool IsOnTop(PopupHandle handle);
    }
}
