using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Game.GameFlow
{
    /// <summary>Per-frame time for the build limit, and whether the app is in front. Tests use a fake.</summary>
    internal interface IFlowClock
    {
        /// <summary>
        /// Calls <paramref name="tick"/> every frame with the real seconds since the last frame, and
        /// <paramref name="appActive"/> when the app goes to the background (false) or comes back (true),
        /// until the returned handle is disposed.
        /// </summary>
        IDisposable Start(Action<float> tick, Action<bool> appActive);
    }

    /// <summary>Runs on Unity's player loop (no scene object) and listens to the app losing or regaining focus.</summary>
    internal sealed class UnityFlowClock : IFlowClock
    {
        public IDisposable Start(Action<float> tick, Action<bool> appActive)
        {
            if (tick == null) throw new ArgumentNullException(nameof(tick));
            if (appActive == null) throw new ArgumentNullException(nameof(appActive));

            var stop = new Stop(appActive);
            Application.focusChanged += appActive;
            Loop(tick, stop.Token).Forget();
            return stop;
        }

        private static async UniTaskVoid Loop(Action<float> tick, CancellationToken token)
        {
            while (true)
            {
                var cancelled = await UniTask.Yield(PlayerLoopTiming.Update, token).SuppressCancellationThrow();
                if (cancelled) return;
                tick(Time.unscaledDeltaTime);
            }
        }

        private sealed class Stop : IDisposable
        {
            private readonly CancellationTokenSource _cts = new CancellationTokenSource();
            private Action<bool> _appActive;

            public Stop(Action<bool> appActive) => _appActive = appActive;

            public CancellationToken Token => _cts.Token;

            public void Dispose()
            {
                if (_appActive == null) return;
                Application.focusChanged -= _appActive;
                _appActive = null;
                _cts.Cancel();
                _cts.Dispose();
            }
        }
    }
}
