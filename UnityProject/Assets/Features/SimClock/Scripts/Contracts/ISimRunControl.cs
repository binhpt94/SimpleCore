namespace Game.SimClock
{
    /// <summary>
    /// Run milestones the clock acts on (feature-sim-clock: init-on-run, first-step-on-start, clear-on-dispose).
    /// The owner of the run decides when; the clock only does the set-up and clean-up.
    /// </summary>
    public interface ISimRunControl
    {
        /// <summary>A new run is being set up: step counts and left-over time go to 0, a seed is chosen and every random stream is reset.</summary>
        void SetUpRun();

        /// <summary>The run has started: the first gameplay step may run from now on, and the run's seed is recorded once.</summary>
        void StartRun();

        /// <summary>Whether the battle plays on the coming steps (false after the run ended).</summary>
        void SetGameplay(bool playing);

        /// <summary>The battle is being torn down: queued actions, left-over time and the run's streams are cleared.</summary>
        void ClearRun();

        /// <summary>The seed of the current run (valid after <see cref="SetUpRun"/>).</summary>
        int Seed { get; }
    }
}
