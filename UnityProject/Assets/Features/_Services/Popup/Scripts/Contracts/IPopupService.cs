namespace Game.Services.Popup
{
    /// <summary>Shows popups one at a time, in the order they were requested.</summary>
    public interface IPopupService
    {
        /// <summary>
        /// Queues the popup named <paramref name="id"/>. It opens immediately when nothing else is open.
        /// Throws when the id is the default value or is not in the current scene's popup catalog.
        /// </summary>
        PopupHandle Show(PopupId id);

        /// <summary>Closes an open popup or cancels a queued one. Unknown handles are ignored.</summary>
        void Close(PopupHandle handle);
    }
}
