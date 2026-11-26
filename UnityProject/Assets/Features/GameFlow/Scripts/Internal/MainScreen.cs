namespace Game.GameFlow
{
    /// <summary>The three main screens of the Game scene; exactly one shows at a time once the scene is ready.</summary>
    internal enum MainScreen
    {
        /// <summary>Nothing shown yet: the Game scene has not reported ready.</summary>
        None,
        /// <summary>The start screen.</summary>
        Home,
        /// <summary>The team select screen.</summary>
        TeamSelect,
        /// <summary>The battle screen.</summary>
        Battle,
    }

    /// <summary>What the player chose when leaving a battle; handed over by the run after it cleared the battle.</summary>
    internal enum LeaveChoice
    {
        /// <summary>Lose popup: Play again.</summary>
        Replay,
        /// <summary>Lose popup: Back to start.</summary>
        GoHome,
    }

    /// <summary>Corner of the screen a button sits in.</summary>
    internal enum ScreenCorner
    {
        TopLeft,
        TopRight,
        BottomLeft,
        BottomRight,
    }
}
