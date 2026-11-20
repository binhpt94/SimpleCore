namespace Game.Services.Popup
{
    /// <summary>Shows popups one at a time, in the order they were requested.</summary>
    public interface IPopupService
    {
        /// <summary>
        /// Queues a popup. It opens immediately when nothing else is open.
        /// Returns an invalid handle when the request has no prefab.
        /// </summary>
        PopupHandle Show(PopupRequest request);

        /// <summary>Closes an open popup or cancels a queued one. Unknown handles are ignored.</summary>
        void Close(PopupHandle handle);
    }
}
