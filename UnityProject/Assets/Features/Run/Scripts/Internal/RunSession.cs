using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Game.Core;

namespace Game.Run
{
    /// <summary>
    /// The run inside the Game scene session: reads the saved best before play, begins the run on the frame after
    /// Run (participants installed after the run get their Run call later in the same frame, so they already listen
    /// to RunStarted by then), holds its timed waits while the game is off the screen, and drops
    /// pending waits at the end.
    /// </summary>
    internal sealed class RunSession : SessionParticipantBase
    {
        private readonly RunFlow _flow;
        private readonly RunScore _score;
        private readonly IRunDelay _delay;
        private readonly Func<IAppSignals> _createSignals;
        private readonly SimClockRunDelay _clockWaits;

        /// <param name="delay">Frame waits of the session (begin on the frame after Run); not the clock.</param>
        /// <param name="createSignals">The device signals, made when the session runs; held waits need <paramref name="clockWaits"/>. Null: no device (tests).</param>
        /// <param name="clockWaits">The run's waits on the shared clock; held while away, stopped when the session ends.</param>
        public RunSession(RunFlow flow, RunScore score, IRunDelay delay,
            Func<IAppSignals> createSignals = null, SimClockRunDelay clockWaits = null)
        {
            _clockWaits = clockWaits;
            _flow = flow ?? throw new ArgumentNullException(nameof(flow));
            _score = score ?? throw new ArgumentNullException(nameof(score));
            _delay = delay ?? throw new ArgumentNullException(nameof(delay));
            _createSignals = createSignals;
            if (createSignals != null && clockWaits == null)
                throw new ArgumentNullException(nameof(clockWaits), "Holding the waits while the game is away needs the run's clock waits.");
        }

        protected override UniTask OnLoadAsync(CancellationToken cancellation)
        {
            _score.LoadBest();
            return UniTask.CompletedTask;
        }

        protected override void OnRun()
        {
            // Jobs counted for the session's clean-up check: listening to the clock, each pending clock wait
            // (before the lose popup, cleared waves), and the wait for the frame the run begins on.
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

            if (_createSignals == null) return;
            var signals = _createSignals();
            WhenDisposed(signals.Dispose);
            var interrupts = new RunInterrupts(signals, _clockWaits);
            WhenDisposed(interrupts.Dispose);
        }
    }
}
