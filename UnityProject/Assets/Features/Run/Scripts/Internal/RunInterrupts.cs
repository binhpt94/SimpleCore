using System;

namespace Game.Run
{
    /// <summary>
    /// Turns device signals into pause requests (feature-run/run-pause/pause-rules#pause-enter, flow-rules back key).
    /// Only a running battle pauses: the run ignores the request on the start screen, in team select, while paused,
    /// while waiting after the castle fell and on the lose popup. On the pause popup the back key belongs to the popup.
    /// Coming back to the game never resumes on its own.
    /// While the game is off the screen the run's timed waits are held, so time away does not count toward the
    /// 1 second before the revive or lose popup (flow-rules#edge-roi-game-dung-luc-thanh-vua-bi-pha); coming back lets
    /// the wait finish and the popup that was due shows.
    /// </summary>
    internal sealed class RunInterrupts : IDisposable
    {
        private readonly IRunFlow _flow;
        private readonly IAppSignals _signals;
        private readonly IRunWaitHold _waits;
        private readonly bool _pauseOnLeave;
        private readonly bool _backKeyPauses;
        private bool _disposed;

        /// <param name="waits">The run's timed waits, held while away. Null: nothing to hold (tests of the pause alone).</param>
        public RunInterrupts(IRunFlow flow, IAppSignals signals, bool pauseOnLeave, bool backKeyPauses,
            IRunWaitHold waits = null)
        {
            _flow = flow ?? throw new ArgumentNullException(nameof(flow));
            _signals = signals ?? throw new ArgumentNullException(nameof(signals));
            _waits = waits;
            _pauseOnLeave = pauseOnLeave;
            _backKeyPauses = backKeyPauses;
            _signals.LeftApp += OnLeftApp;
            _signals.CameBack += OnCameBack;
            _signals.BackPressed += OnBackPressed;
        }

        private void OnLeftApp()
        {
            _waits?.Hold();
            if (_pauseOnLeave) _flow.Pause();
        }

        private void OnCameBack() => _waits?.Release();

        private void OnBackPressed()
        {
            // While paused the popup reads the key itself (Continue / No); only a running battle pauses here.
            if (_backKeyPauses && _flow.Step == RunStep.Playing) _flow.Pause();
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _signals.LeftApp -= OnLeftApp;
            _signals.CameBack -= OnCameBack;
            _signals.BackPressed -= OnBackPressed;
            // The session ends: nothing may stay held.
            _waits?.Release();
        }
    }
}
