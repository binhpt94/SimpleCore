using System;
using System.Collections.Generic;
using Game.Core;

namespace Game.Run
{
    /// <summary>
    /// feature-run/run-start/replay-checks: a new run must start with no popup open. A popup left open is a bug of
    /// the feature that opened it: the run logs its id and does not close it. Checked on the frame after the battle
    /// starts, so popups closed by the press that started the run (Play again) are already gone.
    /// </summary>
    internal sealed class LeftoverPopupCheck : SessionParticipantBase
    {
        private readonly IRunFlow _flow;
        private readonly Func<IReadOnlyList<string>> _openPopups;
        private readonly IRunDelay _delay;
        private readonly IRunLog _log;
        private IDisposable _pending;
        private IDisposable _pendingJob;

        public LeftoverPopupCheck(IRunFlow flow, Func<IReadOnlyList<string>> openPopups, IRunDelay delay, IRunLog log)
        {
            _flow = flow ?? throw new ArgumentNullException(nameof(flow));
            _openPopups = openPopups ?? throw new ArgumentNullException(nameof(openPopups));
            _delay = delay ?? throw new ArgumentNullException(nameof(delay));
            _log = log ?? throw new ArgumentNullException(nameof(log));
        }

        protected override void OnRun()
        {
            _flow.BattleStarted += OnBattleStarted;
            WhenDisposed(() =>
            {
                _flow.BattleStarted -= OnBattleStarted;
                DropPending();
            });
        }

        private void OnBattleStarted()
        {
            DropPending();
            // The wait for the next frame counts as a running job for the session's clean-up check.
            _pendingJob = TrackJob();
            _pending = _delay.NextFrame(Check);
        }

        private void DropPending()
        {
            _pending?.Dispose();
            _pending = null;
            _pendingJob?.Dispose();
            _pendingJob = null;
        }

        private void Check()
        {
            _pending = null;
            _pendingJob?.Dispose();
            _pendingJob = null;
            var open = _openPopups();
            if (open == null || open.Count == 0) return;
            _log.Error($"A new run started with popups still open: {string.Join(", ", open)}. "
                + "The feature that opened them must close them before the run goes on; the run does not close them.");
        }
    }
}
