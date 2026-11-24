using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Game.Hero
{
    /// <summary>The game's frames; behind an interface so tests step time by hand.</summary>
    internal interface IFrameClock
    {
        /// <summary>Completes at the next frame's update.</summary>
        UniTask NextFrame(CancellationToken cancellation);

        /// <summary>Seconds since the previous frame.</summary>
        float DeltaSeconds { get; }
    }

    internal sealed class UnityFrameClock : IFrameClock
    {
        public UniTask NextFrame(CancellationToken cancellation) => UniTask.Yield(PlayerLoopTiming.Update, cancellation);

        public float DeltaSeconds => Time.deltaTime;
    }
}
