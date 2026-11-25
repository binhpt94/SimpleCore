#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using Game.Core;
using Game.Run;

namespace Game.Wave
{
    /// <summary>
    /// Dev cheat set on the installer: on the first step after the battle started and wave 1 began, jumps to a wave
    /// and pauses the run, so a paused mid-battle state is there without playing up to it
    /// (state "battle stands at wave 5, paused").
    /// Fires once per session; goes through the normal paths (<see cref="IWaveCheat.StartWave"/>, <see cref="IRunFlow.Pause"/>)
    /// and follows the shared clock through <see cref="WaveDirector.Stepped"/>, never frame time.
    /// </summary>
    internal sealed class WaveStartCheat : SessionParticipantBase
    {
        private readonly WaveDirector _waves;
        private readonly IRunFlow _run;
        private readonly int _wave;

        private bool _armed;
        private bool _done;
        private IDisposable _job;

        public WaveStartCheat(WaveDirector waves, IRunFlow run, int wave)
        {
            if (wave < 1) throw new ArgumentOutOfRangeException(nameof(wave), wave, "Waves start at 1.");
            _waves = waves ?? throw new ArgumentNullException(nameof(waves));
            _run = run ?? throw new ArgumentNullException(nameof(run));
            _wave = wave;
        }

        protected override void OnRun()
        {
            // A pending wait until it fires (or the session ends).
            _job = TrackJob();
            _run.BattleStarted += OnBattleStarted;
            _waves.Stepped += OnStepped;
            WhenDisposed(Unhook);
        }

        private void OnBattleStarted()
        {
            if (!_done) _armed = true;
        }

        private void OnStepped()
        {
            if (!_armed || !_waves.IsRunning) return;
            _armed = false;
            _done = true;
            Unhook();

            _waves.StartWave(_wave);
            _run.Pause();
        }

        private void Unhook()
        {
            _run.BattleStarted -= OnBattleStarted;
            _waves.Stepped -= OnStepped;
            _job?.Dispose();
            _job = null;
        }
    }
}
#endif
