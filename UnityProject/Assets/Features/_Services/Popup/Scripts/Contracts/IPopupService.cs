namespace Game.Services.Popup
{
    /// <summary>
    /// Opens popups as a stack: the newest is on top and is the only one that takes input.
    /// A popup closes only when its caller asks, or when the scene ends.
    /// </summary>
    public interface IPopupService
    {
        /// <summary>
        /// Opens the popup named <paramref name="id"/> on top of the stack, at once.
        /// Throws when the id is the default value or is not in the current scene's popup catalog.
        /// Refuses (logs, returns an invalid handle) when that id is already open or the stack is full.
        /// </summary>
        PopupHandle Show(PopupId id);

        /// <summary>Closes an open popup, wherever it is in the stack. Unknown or closed handles are ignored.</summary>
        void Close(PopupHandle handle);
    }
}
