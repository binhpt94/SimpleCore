using System;

namespace Game.SimClock
{
    /// <summary>How the shared clock turns frames into steps (feature-sim-clock/clock-step/clock-values).</summary>
    public enum SimMode
    {
        /// <summary>Every step has the same length (0.025 s at 40 steps a second); a frame runs zero, one or several steps.</summary>
        FixedTick = 0,

        /// <summary>Each frame runs exactly one step as long as the frame (clamped).</summary>
        Frame = 1,
    }

    /// <summary>
    /// The one clock of the battle. Gameplay logic steps here instead of reading frame time.
    /// Subscribers run once per step in the fixed order kept in this feature's config (by owner name),
    /// after the queued player actions of that step were applied.
    /// </summary>
    public interface ISimClock
    {
        /// <summary>The running mode, read once from the config when the battle starts.</summary>
        SimMode Mode { get; }

        /// <summary>The nominal step length in seconds (fixed mode: every step; frame mode: the cap is the frame clamp).</summary>
        float StepSeconds { get; }

        /// <summary>Steps run since the run was set up, counting every step (stopped ones too). Player actions are recorded against it.</summary>
        long StepCount { get; }

        /// <summary>Steps run while the run let the battle play. Every gameplay wait counts these, so it stands still once the battle stopped.</summary>
        long GameplayStepCount { get; }

        /// <summary>Battle time so far: the sum of gameplay step lengths, in seconds.</summary>
        double GameplaySeconds { get; }

        /// <summary>
        /// Calls <paramref name="onStep"/> once per step at the place of <paramref name="owner"/> in the step order.
        /// An owner missing from the order is an error raised here. Dispose the handle to stop.
        /// </summary>
        IDisposable OnStep(string owner, Action<SimStep> onStep);

        /// <summary>
        /// Queues a player action; it is applied at the start of the next step, in the order queued, and recorded with that step's number.
        /// The receiving feature still refuses it when its own rules forbid it (for example after the run ended).
        /// </summary>
        void Enqueue(string owner, Action apply);
    }
}
