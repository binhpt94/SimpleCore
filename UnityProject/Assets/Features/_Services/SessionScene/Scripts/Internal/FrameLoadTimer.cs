using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Game.Services.SessionScene
{
    /// <summary>
    /// Counts frame time, ignoring time scale. Each frame counts at most Time.maximumDeltaTime, so the time the
    /// player spent outside the game while it was paused is not counted toward the limit.
    /// </summary>
    internal sealed class FrameLoadTimer : ILoadTimer
    {
        public async UniTask WaitAsync(TimeSpan limit, CancellationToken cancellation)
        {
            var elapsed = 0.0;
            while (elapsed < limit.TotalSeconds)
            {
                await UniTask.Yield(PlayerLoopTiming.Update, cancellation);
                elapsed += Math.Min(Time.unscaledDeltaTime, Time.maximumDeltaTime);
            }
        }
    }
}
