using System;

namespace Game.Run
{
    /// <summary>
    /// Turns the game leaving and coming back to the screen into holding the run's timed waits: time away does not
    /// count toward the 1 second before the lose popup (feature-run/run-flow/flow-rules#edge-roi-game-dung-luc-thanh-vua-bi-pha);
    /// coming back lets the wait finish and the lose popup shows. Leaving the game changes no step.
    /// Coming back is also one more try to hand an unsaved best to feature-save
    /// (feature-run/run-score/score-rules#record-retry).
    /// </summary>
    internal sealed class RunInterrupts : IDisposable
    {
        private readonly IAppSignals _signals;
        private readonly IRunWaitHold _waits;
        private readonly Action _cameBack;
        private bool _disposed;

        /// <param name="cameBack">Also run each time the game is on the screen again (retry an unsaved best). Null: nothing.</param>
        public RunInterrupts(IAppSignals signals, IRunWaitHold waits, Action cameBack = null)
        {
            _cameBack = cameBack;
            _signals = signals ?? throw new ArgumentNullException(nameof(signals));
            _waits = waits ?? throw new ArgumentNullException(nameof(waits));
            _signals.LeftApp += OnLeftApp;
            _signals.CameBack += OnCameBack;
        }

        private void OnLeftApp() => _waits.Hold();

        private void OnCameBack()
        {
            _waits.Release();
            _cameBack?.Invoke();
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _signals.LeftApp -= OnLeftApp;
            _signals.CameBack -= OnCameBack;
            // The session ends: nothing may stay held.
            _waits.Release();
        }
    }
}
