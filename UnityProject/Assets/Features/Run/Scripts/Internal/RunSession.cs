using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Game.Core;

namespace Game.Run
{
    /// <summary>
    /// The run inside the Game scene session: reads the saved best before play, begins the run on the frame after
    /// Run (participants installed after the run get their Run call later in the same frame, so they already listen
    /// to RunStarted by then), listens to the device (leaving the game, back key) when the config asks, and drops
    /// pending waits at the end.
    /// </summary>
    internal sealed class RunSession : SessionParticipantBase
    {
        private readonly RunFlow _flow;
        private readonly RunScore _score;
        private readonly IRunDelay _delay;
        private readonly Func<IAppSignals> _createSignals;
        private readonly bool _pauseOnLeave;
        private readonly bool _backKeyPauses;
        private readonly SimClockRunDelay _clockWaits;

        /// <param name="delay">Frame waits of the session (begin on the frame after Run); not the clock.</param>
        /// <param name="clockWaits">The run's waits on the shared clock; stopped when the session ends.</param>
        public RunSession(RunFlow flow, RunScore score, IRunDelay delay,
            Func<IAppSignals> createSignals = null, bool pauseOnLeave = false, bool backKeyPauses = false,
            SimClockRunDelay clockWaits = null)
        {
            _clockWaits = clockWaits;
            _flow = flow ?? throw new ArgumentNullException(nameof(flow));
            _score = score ?? throw new ArgumentNullException(nameof(score));
            _delay = delay ?? throw new ArgumentNullException(nameof(delay));
            _createSignals = createSignals;
            _pauseOnLeave = pauseOnLeave;
            _backKeyPauses = backKeyPauses;
            if ((pauseOnLeave || backKeyPauses) && createSignals == null)
                throw new ArgumentNullException(nameof(createSignals), "Pausing on leaving the game or on the back key needs the device signals.");
        }

        protected override UniTask OnLoadAsync(CancellationToken cancellation)
        {
            _score.LoadBest();
            return UniTask.CompletedTask;
        }

        protected override void OnRun()
        {
            // Jobs counted for the session's clean-up check: listening to the clock, each pending clock wait
            // (before the lose or revive popup, cleared waves), and the wait for the frame the run begins on.
            if (_clockWaits != null)
            {
                _clockWaits.Listen(TrackJob);
                WhenDisposed(_clockWaits.Dispose);
            }
            var beginJob = TrackJob();
            var begin = _delay.NextFrame(() =>
            {
                beginJob.Dispose();
                _flow.Begin();
            });
            WhenDisposed(() =>
            {
                begin.Dispose();
                beginJob.Dispose();
            });
            WhenDisposed(_flow.Shutdown);

            if (!_pauseOnLeave && !_backKeyPauses) return;
            var signals = _createSignals();
            WhenDisposed(signals.Dispose);
            var interrupts = new RunInterrupts(_flow, signals, _pauseOnLeave, _backKeyPauses, _clockWaits);
            WhenDisposed(interrupts.Dispose);
        }
    }
}
