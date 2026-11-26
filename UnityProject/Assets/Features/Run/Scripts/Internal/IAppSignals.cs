using System;

namespace Game.Run
{
    /// <summary>What the device tells the run: the game left or came back to the screen. Tests use a fake.</summary>
    internal interface IAppSignals : IDisposable
    {
        /// <summary>The game is no longer on the screen (app switched, screen locked, call, focus lost).</summary>
        event Action LeftApp;

        /// <summary>The game is on the screen again after <see cref="LeftApp"/> (app resumed, focus back).</summary>
        event Action CameBack;
    }
}
