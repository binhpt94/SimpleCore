namespace Game.Run
{
    /// <summary>Where the player is in the Game scene. Only the run decides the next step.</summary>
    public enum RunStep
    {
        /// <summary>The start screen.</summary>
        Home,
        /// <summary>The team select screen.</summary>
        TeamSelect,
        /// <summary>In battle, the battle is running.</summary>
        Playing,
        /// <summary>In battle, everything on the battlefield stands still; the pause popup is up.</summary>
        Paused,
        /// <summary>The castle was destroyed: the battle stopped and the run waits before showing the result.</summary>
        Ending,
        /// <summary>The run is over and the lose popup is up.</summary>
        Lost,
        /// <summary>
        /// The castle fell for the first time in the run and a revive will be offered: the battle stands still
        /// (<see cref="IRunFlow.IsPaused"/>) and the run waits before the revive popup shows.
        /// </summary>
        ReviveWait,
        /// <summary>The battle stands still and the revive popup is up.</summary>
        ReviveOffer,
        /// <summary>
        /// The player earned the revive: the castle stands up again while the battle still stands still, until the
        /// castle reports it is shown through <see cref="IRunRevive.ReviveShown"/>.
        /// </summary>
        Reviving,
    }
}
