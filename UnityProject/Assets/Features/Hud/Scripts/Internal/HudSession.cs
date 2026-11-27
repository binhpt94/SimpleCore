using System;
using Game.Castle;
using Game.Core;
using Game.Run;
using Game.Wallet;
using Game.Wave;
using UnityEngine;

namespace Game.Hud
{
    /// <summary>
    /// The HUD's part of the scene session: creates the view when the session runs, shows it only on battle steps,
    /// drives both presenters every frame, and removes everything when the session ends.
    /// </summary>
    internal sealed class HudSession : SessionParticipantBase
    {
        private readonly IHudViewSpawner _spawner;
        private readonly IHudClock _clock;
        private readonly ICastle _castle;
        private readonly ICastleSkills _skills;
        private readonly IWalletBalance _wallet;
        private readonly IWaveProgress _waves;
        private readonly IRunFlow _run;
        private readonly HudTimings _timings;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        /// <summary>The cheat state this scene builds; null when none. Applied once, on the first frame of a running battle.</summary>
        private HudCheat _cheat;
#endif
        private IHudView _view;
        private HudStatusPresenter _status;
        private HudSkillPresenter _skillButtons;

        public HudSession(IHudViewSpawner spawner, IHudClock clock, ICastle castle, ICastleSkills skills,
            IWalletBalance wallet, IWaveProgress waves, IRunFlow run, HudTimings timings
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            , HudCheat cheat = null
#endif
            )
        {
            _spawner = spawner ?? throw new ArgumentNullException(nameof(spawner));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _castle = castle ?? throw new ArgumentNullException(nameof(castle));
            _skills = skills ?? throw new ArgumentNullException(nameof(skills));
            _wallet = wallet ?? throw new ArgumentNullException(nameof(wallet));
            _waves = waves ?? throw new ArgumentNullException(nameof(waves));
            _run = run ?? throw new ArgumentNullException(nameof(run));
            _timings = timings;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            _cheat = cheat;
#endif
        }

        /// <summary>The view on screen while the session runs; null before and after.</summary>
        internal IHudView View => _view;
#if UNITY_EDITOR || DEVELOPMENT_BUILD

        /// <summary>True while a cheat state was built for this scene and is not applied yet.</summary>
        internal bool CheatPending => _cheat != null;
#endif

        protected override void OnRun()
        {
            _view = _spawner.Spawn();
            var view = _view;
            var heldView = TrackView(view.Root);
            WhenDisposed(() =>
            {
                heldView.Dispose();
                view.Destroy();
                _view = null;
            });

            _status = new HudStatusPresenter(view, _castle, _wallet, _waves, _run, _timings);
            _status.Start();
            WhenDisposed(_status.Stop);

            _skillButtons = new HudSkillPresenter(view, _skills, _run, _timings);
            _skillButtons.Start();
            WhenDisposed(_skillButtons.Stop);
            _run.StepChanged += OnStepChanged;
            WhenDisposed(() => _run.StepChanged -= OnStepChanged);
            OnStepChanged(_run.Step);

            _clock.Ticked += OnTick;
            _clock.Start();
            var frameLoop = TrackJob();
            WhenDisposed(() =>
            {
                _clock.Stop();
                frameLoop.Dispose();
                _clock.Ticked -= OnTick;
            });
        }

        private void OnStepChanged(RunStep step) => _view?.SetVisible(HudText.IsBattle(step));

        private void OnTick(float deltaSeconds)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            ApplyCheatOnceBattleRuns();
#endif
            _status?.Tick(deltaSeconds);
            _skillButtons?.Tick(deltaSeconds);
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        /// <summary>
        /// Builds the cheat state on the first frame the battle is being played, after every owner has set its own
        /// start values (so wave 1 starting cannot undo wave 7), then shows it the same frame.
        /// </summary>
        private void ApplyCheatOnceBattleRuns()
        {
            if (_cheat == null || _run.Step != RunStep.Playing) return;
            var cheat = _cheat;
            _cheat = null;
            var missing = cheat.ApplyMidBattle();
            if (missing.Count > 0)
                Debug.LogWarning("Hud cheat '" + HudCheat.MidBattle + "': not applied: " + string.Join(", ", missing));
        }
#endif
    }
}
