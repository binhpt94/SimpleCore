using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Game.Wave
{
    /// <summary>Frame time for the waves; behind an interface so tests step time by hand.</summary>
    internal interface IWaveClock
    {
        /// <summary>Calls <paramref name="tick"/> once per frame with the frame's seconds until <paramref name="cancellation"/> fires.</summary>
        void Start(Action<float> tick, CancellationToken cancellation);
    }

    /// <summary>Unity's frame loop, without a MonoBehaviour: a UniTask loop on the Update step.</summary>
    internal sealed class UnityWaveClock : IWaveClock
    {
        public void Start(Action<float> tick, CancellationToken cancellation)
        {
            if (tick == null) throw new ArgumentNullException(nameof(tick));
            Loop(tick, cancellation).Forget();
        }

        private static async UniTaskVoid Loop(Action<float> tick, CancellationToken cancellation)
        {
            while (!cancellation.IsCancellationRequested)
            {
                var cancelled = await UniTask.Yield(PlayerLoopTiming.Update, cancellation).SuppressCancellationThrow();
                if (cancelled) return;
                tick(Time.deltaTime);
            }
        }
    }
}
