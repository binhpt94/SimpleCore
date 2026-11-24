using System;
using System.Threading;
using Game.Core;

namespace Game.Wave
{
    /// <summary>Wave's part of the scene session: listens to the run and the enemies, and ticks the waves each frame.</summary>
    internal sealed class WaveParticipant : SessionParticipantBase
    {
        private readonly WaveDirector _director;
        private readonly IWaveClock _clock;

        public WaveParticipant(WaveDirector director, IWaveClock clock)
        {
            _director = director ?? throw new ArgumentNullException(nameof(director));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        }

        protected override void OnRun()
        {
            _director.Attach();
            WhenDisposed(_director.Detach);

            var stop = new CancellationTokenSource();
            _clock.Start(_director.Tick, stop.Token);
            WhenDisposed(() =>
            {
                stop.Cancel();
                stop.Dispose();
            });
        }
    }
}
