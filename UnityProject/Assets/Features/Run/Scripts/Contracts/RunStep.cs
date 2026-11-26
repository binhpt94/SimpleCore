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
        /// <summary>The castle was destroyed: the battle stopped and the run waits before showing the result.</summary>
        Ending,
        /// <summary>The run is over and the lose popup is up.</summary>
        Lost,
    }
}
