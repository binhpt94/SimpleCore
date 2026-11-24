using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Game.Run
{
    /// <summary>Real-time wait (not slowed by time scale), cancelled when the handle is disposed.</summary>
    internal sealed class UniTaskRunDelay : IRunDelay
    {
        public IDisposable Schedule(float seconds, Action then)
        {
            if (then == null) throw new ArgumentNullException(nameof(then));
            var cancel = new CancellationTokenSource();
            WaitThenRun(seconds, then, cancel.Token).Forget();
            return new Handle(cancel);
        }

        public IDisposable NextFrame(Action then)
        {
            if (then == null) throw new ArgumentNullException(nameof(then));
            var cancel = new CancellationTokenSource();
            YieldThenRun(then, cancel.Token).Forget();
            return new Handle(cancel);
        }

        private static async UniTaskVoid YieldThenRun(Action then, CancellationToken token)
        {
            var cancelled = await UniTask.NextFrame(token).SuppressCancellationThrow();
            if (cancelled) return;
            then();
        }

        private static async UniTaskVoid WaitThenRun(float seconds, Action then, CancellationToken token)
        {
            var cancelled = await UniTask.Delay(TimeSpan.FromSeconds(seconds), ignoreTimeScale: true, cancellationToken: token)
                .SuppressCancellationThrow();
            if (cancelled) return;
            then();
        }

        private sealed class Handle : IDisposable
        {
            private CancellationTokenSource _cancel;

            public Handle(CancellationTokenSource cancel) => _cancel = cancel;

            public void Dispose()
            {
                if (_cancel == null) return;
                _cancel.Cancel();
                _cancel.Dispose();
                _cancel = null;
            }
        }
    }
}
