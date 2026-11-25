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

        public RunSession(RunFlow flow, RunScore score, IRunDelay delay,
            Func<IAppSignals> createSignals = null, bool pauseOnLeave = false, bool backKeyPauses = false)
        {
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
            var begin = _delay.NextFrame(_flow.Begin);
            WhenDisposed(begin.Dispose);
            WhenDisposed(_flow.Shutdown);

            if (!_pauseOnLeave && !_backKeyPauses) return;
            var signals = _createSignals();
            WhenDisposed(signals.Dispose);
            var interrupts = new RunInterrupts(_flow, signals, _pauseOnLeave, _backKeyPauses);
            WhenDisposed(interrupts.Dispose);
        }
    }
}
