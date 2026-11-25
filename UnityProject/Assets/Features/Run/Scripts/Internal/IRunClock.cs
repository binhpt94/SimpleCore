using System;

namespace Game.Run
{
    /// <summary>
    /// What the run asks of the shared clock (feature-sim-clock: init-on-run, first-step-on-start, continue-after-revive,
    /// clear-on-dispose, input-next-step). The run decides when; the clock does the set-up, the queue and the clean-up.
    /// </summary>
    internal interface IRunClock
    {
        /// <summary>A new run is being set up (before RunStarted): steps, left-over time, seed and streams reset.</summary>
        void SetUpRun();

        /// <summary>The run has started (after BattleStarted): the first gameplay step may run.</summary>
        void StartRun();

        /// <summary>Whether the battle plays on the coming steps: true only while the run is Playing.</summary>
        void SetGameplay(bool playing);

        /// <summary>The battle is torn down (before BattleCleared): queue, left-over time and streams cleared.</summary>
        void ClearRun();

        /// <summary>A player action of the run (pause, continue): applied at the start of the next step.</summary>
        void Enqueue(Action apply);
    }

    /// <summary>No shared clock: actions apply at once and the milestones do nothing. Used when a test builds the run alone.</summary>
    internal sealed class ImmediateRunClock : IRunClock
    {
        public void SetUpRun() { }

        public void StartRun() { }

        public void SetGameplay(bool playing) { }

        public void ClearRun() { }

        public void Enqueue(Action apply)
        {
            if (apply == null) throw new ArgumentNullException(nameof(apply));
            apply();
        }
    }
}
