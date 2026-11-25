using System;

namespace Game.Run
{
    /// <summary>
    /// Turns device signals into pause requests (feature-run/run-pause/pause-rules#pause-enter, flow-rules back key).
    /// Only a running battle pauses: the run ignores the request on the start screen, in team select, while paused,
    /// while waiting after the castle fell and on the lose popup. On the pause popup the back key belongs to the popup.
    /// Coming back to the game never resumes on its own.
    /// </summary>
    internal sealed class RunInterrupts : IDisposable
    {
        private readonly IRunFlow _flow;
        private readonly IAppSignals _signals;
        private readonly bool _pauseOnLeave;
        private readonly bool _backKeyPauses;
        private bool _disposed;

        public RunInterrupts(IRunFlow flow, IAppSignals signals, bool pauseOnLeave, bool backKeyPauses)
        {
            _flow = flow ?? throw new ArgumentNullException(nameof(flow));
            _signals = signals ?? throw new ArgumentNullException(nameof(signals));
            _pauseOnLeave = pauseOnLeave;
            _backKeyPauses = backKeyPauses;
            _signals.LeftApp += OnLeftApp;
            _signals.BackPressed += OnBackPressed;
        }

        private void OnLeftApp()
        {
            if (_pauseOnLeave) _flow.Pause();
        }

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
            _signals.BackPressed -= OnBackPressed;
        }
    }
}
