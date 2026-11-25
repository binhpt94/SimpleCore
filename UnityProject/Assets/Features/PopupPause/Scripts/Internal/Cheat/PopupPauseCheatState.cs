namespace Game.PopupPause
{
    /// <summary>
    /// States of feature-popup-pause/cheat. Chosen on the installer asset; only dev builds act on it
    /// (see <c>PausePopupCheat</c>). The field stays serialized in every build so the asset is the same.
    /// </summary>
    internal enum PopupPauseCheatState
    {
        None,

        /// <summary>The battle is paused and the pause popup is at its main step.</summary>
        PausedMain,

        /// <summary>The battle is paused and the pause popup is at the confirm-quit step.</summary>
        PausedConfirm,
    }
}
