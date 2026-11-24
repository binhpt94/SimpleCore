using System;

namespace Game.Hud
{
    /// <summary>Calls <see cref="Ticked"/> once per frame with the seconds since the last frame, between Start and Stop.</summary>
    internal interface IHudClock
    {
        event Action<float> Ticked;

        void Start();

        void Stop();
    }
}
