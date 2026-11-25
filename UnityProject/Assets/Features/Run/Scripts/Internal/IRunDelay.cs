using System;

namespace Game.Run
{
    /// <summary>Runs an action later. Disposing the returned handle cancels it. Tests replace it with a fake.</summary>
    internal interface IRunDelay
    {
        /// <summary>Runs <paramref name="then"/> after <paramref name="seconds"/> (frames: real time; the shared clock: step time).</summary>
        IDisposable Schedule(float seconds, Action then);

        /// <summary>Runs <paramref name="then"/> on the next frame (the shared clock: the run's next place in a step), never in the current call.</summary>
        IDisposable NextFrame(Action then);
    }
}
