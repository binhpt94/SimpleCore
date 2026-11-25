using System;

namespace Game.Run
{
    /// <summary>What the device tells the run: the game left the screen, or the device back key was pressed. Tests use a fake.</summary>
    internal interface IAppSignals : IDisposable
    {
        /// <summary>The game is no longer on the screen (app switched, screen locked, call, focus lost).</summary>
        event Action LeftApp;

        /// <summary>The device back key (Android) was pressed this frame.</summary>
        event Action BackPressed;
    }
}
