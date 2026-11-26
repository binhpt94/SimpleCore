using System;
using Game.SimClock;

namespace Game.Run
{
    /// <summary>The run's milestones and queued actions on the shared clock of feature-sim-clock.</summary>
    internal sealed class SimRunClock : IRunClock
    {
        /// <summary>The run's place in the step order of the clock (last: it judges the loss after combat).</summary>
        internal const string Owner = "Run";

        private readonly ISimRunControl _control;
        private readonly ISimClock _clock;

        public SimRunClock(ISimRunControl control, ISimClock clock)
        {
            _control = control ?? throw new ArgumentNullException(nameof(control));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        }

        public void SetUpRun() => _control.SetUpRun();

        public void StartRun() => _control.StartRun();

        public void SetGameplay(bool playing) => _control.SetGameplay(playing);

        public void ClearRun() => _control.ClearRun();
    }
}
