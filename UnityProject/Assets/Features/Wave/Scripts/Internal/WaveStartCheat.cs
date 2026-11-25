#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Threading;
using Game.Core;
using Game.Run;

namespace Game.Wave
{
    /// <summary>
    /// Dev cheat set on the installer: one frame after the battle starts, jumps to a wave and pauses the run,
    /// so a paused mid-battle state is there without playing up to it (state "battle stands at wave 5, paused").
    /// Fires once per session; goes through the normal paths (<see cref="IWaveCheat.StartWave"/>, <see cref="IRunFlow.Pause"/>).
    /// </summary>
    internal sealed class WaveStartCheat : SessionParticipantBase
    {
        private readonly IWaveCheat _waves;
        private readonly IRunFlow _run;
        private readonly IWaveClock _clock;
        private readonly int _wave;

        private bool _armed;
        private bool _done;
        private CancellationTokenSource _stop;

        public WaveStartCheat(IWaveCheat waves, IRunFlow run, IWaveClock clock, int wave)
        {
            if (wave < 1) throw new ArgumentOutOfRangeException(nameof(wave), wave, "Waves start at 1.");
            _waves = waves ?? throw new ArgumentNullException(nameof(waves));
            _run = run ?? throw new ArgumentNullException(nameof(run));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _wave = wave;
        }

        protected override void OnRun()
        {
            _run.BattleStarted += OnBattleStarted;
            WhenDisposed(() => _run.BattleStarted -= OnBattleStarted);

            _stop = new CancellationTokenSource();
            _clock.Start(Tick, _stop.Token);
            WhenDisposed(StopClock);
        }

        private void OnBattleStarted()
        {
            if (!_done) _armed = true;
        }

        private void Tick(float deltaSeconds)
        {
            if (!_armed) return;
            _armed = false;
            _done = true;
            StopClock();
            _run.BattleStarted -= OnBattleStarted;

            _waves.StartWave(_wave);
            _run.Pause();
        }

        private void StopClock()
        {
            if (_stop == null) return;
            _stop.Cancel();
            _stop.Dispose();
            _stop = null;
        }
    }
}
#endif
