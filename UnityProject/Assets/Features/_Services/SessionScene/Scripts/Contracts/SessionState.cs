namespace Game.Services.SessionScene
{
    /// <summary>Where one scene's session is in its life. Disposed and Failed are final.</summary>
    public enum SessionState
    {
        /// <summary>Installers are running; features may still join.</summary>
        Init,

        /// <summary>Every participant is loading what it needs before play.</summary>
        Loading,

        /// <summary>Every participant loaded and was told to run; the player can act.</summary>
        Running,

        /// <summary>Participants are letting go of what they hold.</summary>
        Disposing,

        /// <summary>The session ended normally.</summary>
        Disposed,

        /// <summary>A participant could not load or run; the session never became playable.</summary>
        Failed
    }
}
