using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Game.Hud
{
    /// <summary>Frame clock as a UniTask loop on the Update step; no MonoBehaviour.</summary>
    internal sealed class UnityHudClock : IHudClock
    {
        private CancellationTokenSource _loop;

        public event Action<float> Ticked;

        public void Start()
        {
            if (_loop != null) return;
            _loop = new CancellationTokenSource();
            Loop(_loop.Token).Forget();
        }

        public void Stop()
        {
            if (_loop == null) return;
            _loop.Cancel();
            _loop.Dispose();
            _loop = null;
        }

        private async UniTaskVoid Loop(CancellationToken cancellation)
        {
            while (true)
            {
                var cancelled = await UniTask.Yield(PlayerLoopTiming.Update, cancellation).SuppressCancellationThrow();
                if (cancelled) return;
                Ticked?.Invoke(Time.deltaTime);
            }
        }
    }
}
