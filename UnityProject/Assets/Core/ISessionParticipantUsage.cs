namespace Game.Core
{
    /// <summary>
    /// What one session participant holds right now: views it shows and jobs still running (timers, pending
    /// waits, per-frame loops). Read by the scene session to check that a participant let go of everything
    /// after the dispose step. Counted only in the Editor and development builds; release builds read 0.
    /// <see cref="SessionParticipantBase"/> implements it from what the subclass tracked.
    /// </summary>
    public interface ISessionParticipantUsage
    {
        /// <summary>Views tracked and not yet released or destroyed. The scene-transition cover is never counted.</summary>
        int HeldViews { get; }

        /// <summary>Jobs tracked and not yet finished.</summary>
        int RunningJobs { get; }
    }
}
