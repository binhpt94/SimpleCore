namespace Game.Run
{
    /// <summary>
    /// Stops the run's timed waits from counting while the game is off the screen
    /// (feature-run/run-flow/flow-rules#edge-roi-game-dung-luc-thanh-vua-bi-pha: time away does not count toward the
    /// 1 second before the revive or lose popup). Hold and Release may each come more than once in a row.
    /// </summary>
    internal interface IRunWaitHold
    {
        /// <summary>The game left the screen: timed waits stop counting.</summary>
        void Hold();

        /// <summary>The game is back: timed waits count again from where they stood.</summary>
        void Release();
    }
}
