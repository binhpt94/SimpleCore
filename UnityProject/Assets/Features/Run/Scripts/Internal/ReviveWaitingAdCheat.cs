#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using Game.Ads;
using Game.Castle;
using Game.Core;
using Game.SimClock;
using Game.Wave;

namespace Game.Run
{
    /// <summary>
    /// Dev-only cheat state feature-popup-revive/cheat#waiting-ad, built by the run as owner of the battle: the battle
    /// stands still at wave 8, the castle has just fallen for the first time, and the revive popup waits for the ad.
    /// Everything goes through the game's own paths: into battle with the default team (from the start screen when
    /// menus are shown), then on the first gameplay step (wave 1 has started, the run's place is last in the step)
    /// <see cref="IWaveCheat.StartWave"/> 8, <see cref="IAdsCheat.HoldNextAd"/>, <see cref="ICastleFallCheat.Fall"/>.
    /// The run then offers the revive by its own rules (reviveOnce) and the revive popup opens waiting for the ad.
    /// Once per scene session. Not in release builds.
    /// </summary>
    internal sealed class ReviveWaitingAdCheat : SessionParticipantBase
    {
        /// <summary>Wave the battle stands at (feature-popup-revive/cheat#waiting-ad: "trận đứng yên ở đợt 8").</summary>
        internal const int WaveNumber = 8;

        private readonly RunFlow _flow;
        private readonly ISimClock _clock;
        private readonly IRunDelay _frames;
        private readonly RunTeam _team;
        private readonly Func<IWaveCheat> _waveCheat;
        private readonly Func<IAdsCheat> _adsCheat;
        private readonly Func<ICastleFallCheat> _castleCheat;

        private IWaveCheat _wave;
        private IAdsCheat _ads;
        private ICastleFallCheat _castle;
        private bool _armed;
        private bool _done;
        private IDisposable _job;

        /// <param name="waveCheat">Resolved when the session runs: Wave, Castle and Ads install after the run.</param>
        public ReviveWaitingAdCheat(RunFlow flow, ISimClock clock, IRunDelay frames, RunTeam team,
            Func<IWaveCheat> waveCheat, Func<IAdsCheat> adsCheat, Func<ICastleFallCheat> castleCheat)
        {
            _flow = flow ?? throw new ArgumentNullException(nameof(flow));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _frames = frames ?? throw new ArgumentNullException(nameof(frames));
            _team = team;
            _waveCheat = waveCheat ?? throw new ArgumentNullException(nameof(waveCheat));
            _adsCheat = adsCheat ?? throw new ArgumentNullException(nameof(adsCheat));
            _castleCheat = castleCheat ?? throw new ArgumentNullException(nameof(castleCheat));
        }

        protected override void OnRun()
        {
            // Every feature is installed by now; a missing cheat fails here, naming it.
            _wave = _waveCheat() ?? throw new InvalidOperationException($"Cheat waiting-ad needs {nameof(IWaveCheat)} (Wave).");
            _ads = _adsCheat() ?? throw new InvalidOperationException($"Cheat waiting-ad needs {nameof(IAdsCheat)} (Ads).");
            _castle = _castleCheat() ?? throw new InvalidOperationException($"Cheat waiting-ad needs {nameof(ICastleFallCheat)} (Castle).");
            if (!_flow.ReviveOnce)
                throw new InvalidOperationException("Cheat waiting-ad needs the run's reviveOnce on (RunConfig): without it the fall loses the run.");

            _job = TrackJob();
            _flow.BattleStarted += OnBattleStarted;
            var steps = _clock.OnStep(SimRunClock.Owner, OnStep);
            // The run begins on the frame after Run (RunSession, joined first): go into battle after it.
            var enter = _frames.NextFrame(EnterBattleIfOnTheStartScreen);
            WhenDisposed(() =>
            {
                _flow.BattleStarted -= OnBattleStarted;
                enter.Dispose();
                steps.Dispose();
                Finish();
            });
        }

        private void EnterBattleIfOnTheStartScreen()
        {
            if (_done || _flow.Step != RunStep.Home) return;
            _flow.Play();
            _flow.EnterBattle(_team);
        }

        private void OnBattleStarted()
        {
            if (!_done) _armed = true;
        }

        private void OnStep(SimStep step)
        {
            if (!_armed || _done || !step.Gameplay || _flow.Step != RunStep.Playing) return;
            Finish();
            _wave.StartWave(WaveNumber);
            _ads.HoldNextAd();
            _castle.Fall();
        }

        private void Finish()
        {
            _done = true;
            _armed = false;
            _job?.Dispose();
            _job = null;
        }
    }
}
#endif
