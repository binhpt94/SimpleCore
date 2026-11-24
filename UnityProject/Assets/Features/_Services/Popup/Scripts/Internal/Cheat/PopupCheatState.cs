namespace Game.Services.Popup
{
    /// <summary>
    /// States of feature-popup/cheat, picked on the installer asset. The type exists in every build so the
    /// installer keeps one serialized layout; only dev builds act on it.
    /// </summary>
    internal enum PopupCheatState
    {
        None,

        /// <summary>sample-popup: the service opens its own sample popup Popup/DevSample on the running scene.</summary>
        SamplePopup,
    }
}
