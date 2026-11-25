using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Game.Core;

namespace Game.SimClock
{
    /// <summary>
    /// Drives the clock once a frame while the scene runs: no MonoBehaviour, a UniTask loop at PreUpdate, so the
    /// steps of a frame run before views (Update) draw the latest state, and input handled in the previous frame's
    /// Update is applied at the first step after it (clock-values#input-next-step, #view-latest).
    /// </summary>
    internal sealed class SimClockParticipant : SessionParticipantBase
    {
        private readonly SimClockService _clock;
        private readonly IFrameTime _time;

        internal SimClockParticipant(SimClockService clock, IFrameTime time)
        {
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _time = time ?? throw new ArgumentNullException(nameof(time));
        }

        protected override void OnRun()
        {
            var stop = new CancellationTokenSource();
            var job = TrackJob();
            WhenDisposed(() =>
            {
                stop.Cancel();
                stop.Dispose();
                job.Dispose();
            });
            Loop(stop.Token).Forget();
        }

        private async UniTaskVoid Loop(CancellationToken stop)
        {
            while (true)
            {
                if (await UniTask.Yield(PlayerLoopTiming.PreUpdate, stop).SuppressCancellationThrow()) return;
                _clock.Advance(_time.FrameSeconds);
            }
        }
    }
}
