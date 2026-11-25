using System;
using Game.Core;
using Game.Run;
using Game.SimClock;

namespace Game.Combat
{
    /// <summary>
    /// Combat's part in the scene session. It has no view and loads nothing; on run it starts the combat loop on
    /// the shared clock (one tracked job), and when the session ends it stops the loop and drops waiting hits.
    /// </summary>
    internal sealed class CombatParticipant : SessionParticipantBase
    {
        private readonly CombatTick _tick;
        private readonly ISimClock _clock;
        private readonly IRunFlow _run;

        public CombatParticipant(CombatTick tick, ISimClock clock, IRunFlow run)
        {
            _tick = tick ?? throw new ArgumentNullException(nameof(tick));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _run = run ?? throw new ArgumentNullException(nameof(run));
        }

        /// <summary>The running loop, from run until the session ends; read by tests.</summary>
        internal CombatLoop Loop { get; private set; }

        protected override void OnRun()
        {
            var job = TrackJob();
            Loop = new CombatLoop(_tick, _clock, _run);
            WhenDisposed(() =>
            {
                Loop.Dispose();
                Loop = null;
                _tick.ClearPending();
                job.Dispose();
            });
        }
    }
}
