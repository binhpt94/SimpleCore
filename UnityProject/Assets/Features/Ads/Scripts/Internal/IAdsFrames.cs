using System;

namespace Game.Ads
{
    /// <summary>
    /// The frame beat, pushed up by the view: real (unscaled) seconds since the last frame, and whether the game is
    /// on screen (false while the app is paused or has lost focus: an ad, or the player in another app).
    /// </summary>
    internal interface IAdsFrames
    {
        event Action<float, bool> Frame;
    }
}
