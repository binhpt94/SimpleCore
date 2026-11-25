using System;
using Game.Core;
using Game.Enemy;
using Game.Run;
using Game.SimClock;

namespace Game.Castle
{
    /// <summary>
    /// The castle's part in the scene session, and its presenter: makes the view in Run, resets the castle when a
    /// run begins, steps the auto shot and the skill cooldowns on the shared clock during a battle and pushes what
    /// happened down to the view. Drawing (effects, the fall, growing back) runs on frame time: it is only to look at.
    /// For the session's clean-up check it reports the view it holds and its running jobs: the clock loop, the skill
    /// cooldowns while any runs down, and the revive grow-back wait.
    /// </summary>
    internal sealed class CastleSession : SessionParticipantBase
    {
        /// <summary>The castle's place in the shared clock's step order (SimClock config).</summary>
        public const string ClockOwner = "Castle";

        private readonly CastleConfig _config;
        private readonly ISimClock _clock;
        private readonly IRunFlow _runFlow;
        private readonly IRunRevive _revive;
        private readonly IEnemyField _enemies;
        private readonly CastleBody _body;
        private readonly CastleSkills _skills;
        private readonly CastleUpgrades _upgrades;
        private readonly CastleAutoShot _autoShot;
        private readonly ICastleViewSpawner _spawner;
        private ICastleView _view;
        private bool _inBattle;
        private bool _reviving;
        private float _reviveLeft;
        private IDisposable _reviveJob;
        private IDisposable _cooldownJob;

        public CastleSession(
            CastleConfig config, IRunFlow runFlow, IRunRevive revive, IEnemyField enemies,
            CastleBody body, CastleSkills skills, CastleUpgrades upgrades, CastleAutoShot autoShot,
            ICastleViewSpawner spawner, ISimClock clock)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _runFlow = runFlow ?? throw new ArgumentNullException(nameof(runFlow));
            _revive = revive ?? throw new ArgumentNullException(nameof(revive));
            _enemies = enemies ?? throw new ArgumentNullException(nameof(enemies));
            _body = body ?? throw new ArgumentNullException(nameof(body));
            _skills = skills ?? throw new ArgumentNullException(nameof(skills));
            _upgrades = upgrades ?? throw new ArgumentNullException(nameof(upgrades));
            _autoShot = autoShot ?? throw new ArgumentNullException(nameof(autoShot));
            _spawner = spawner ?? throw new ArgumentNullException(nameof(spawner));
        }

        /// <summary>True between BattleStarted and BattleStopped: the castle shoots and its cooldowns run.</summary>
        public bool InBattle => _inBattle;

        /// <summary>
        /// False since the PROTOTYPE screen flow: the game opens on the home screen, and the session's check right
        /// after the run step must not demand the castle's view; the castle is seen once a battle is set up.
        /// </summary>
        protected override bool OpensViewsOnRun => false;

        protected override void OnRun()
        {
            _view = _spawner.Spawn();
            var view = _view;
            var viewHandle = TrackView(view.ViewObject);
            WhenDisposed(() =>
            {
                viewHandle.Dispose();
                _spawner.Despawn(view);
            });
            _view.Place(_config.HalfWidth, _config.Height);
            _view.SetVisible(false);

            _view.Ticked += OnTicked;
            WhenDisposed(() => view.Ticked -= OnTicked);
            var steps = _clock.OnStep(ClockOwner, OnStep);
            var stepJob = TrackJob();
            WhenDisposed(() =>
            {
                steps.Dispose();
                stepJob.Dispose();
            });
            WhenDisposed(StopJobs);

            _runFlow.RunStarted += OnRunStarted;
            WhenDisposed(() => _runFlow.RunStarted -= OnRunStarted);
            _runFlow.BattleStarted += OnBattleStarted;
            WhenDisposed(() => _runFlow.BattleStarted -= OnBattleStarted);
            _runFlow.BattleStopped += OnBattleStopped;
            WhenDisposed(() => _runFlow.BattleStopped -= OnBattleStopped);
            _runFlow.BattleCleared += OnBattleCleared;
            WhenDisposed(() => _runFlow.BattleCleared -= OnBattleCleared);
            _revive.ReviveStarted += OnReviveStarted;
            WhenDisposed(() => _revive.ReviveStarted -= OnReviveStarted);
            _enemies.Defeated += OnEnemyDefeated;
            WhenDisposed(() => _enemies.Defeated -= OnEnemyDefeated);
            _body.Struck += OnStruck;
            WhenDisposed(() => _body.Struck -= OnStruck);
            _body.HealthChanged += OnHealthChanged;
            WhenDisposed(() => _body.HealthChanged -= OnHealthChanged);
            _body.Destroyed += OnDestroyed;
            WhenDisposed(() => _body.Destroyed -= OnDestroyed);
            _body.Revived += OnRevived;
            WhenDisposed(() => _body.Revived -= OnRevived);
            _autoShot.Fired += OnFired;
            WhenDisposed(() => _autoShot.Fired -= OnFired);
            _skills.ThunderStruck += OnThunderStruck;
            WhenDisposed(() => _skills.ThunderStruck -= OnThunderStruck);
            _upgrades.LevelApplied += OnLevelApplied;
            WhenDisposed(() => _upgrades.LevelApplied -= OnLevelApplied);
        }

        /// <summary>
        /// One step of the shared clock. Only gameplay steps of a battle move the castle (the clock keeps stepping
        /// while paused or waiting for a revive, with <see cref="SimStep.Gameplay"/> false): cooldowns run down
        /// first, then the auto shot, in the same order the frame loop used.
        /// </summary>
        internal void OnStep(SimStep step)
        {
            if (step.Gameplay && _inBattle && !_runFlow.IsPaused && step.Seconds > 0f)
            {
                _skills.Tick(step.Seconds);
                _autoShot.Tick(step.Seconds);
            }
            RefreshCooldownJob();
        }

        /// <summary>
        /// One frame of <paramref name="seconds"/>, for drawing only. Nothing moves in the pause popup. In every other
        /// step where the battle stands still (the wait before the revive or lose popup, the revive popup, reviving)
        /// the castle's own drawing goes on: the fall shrinks all the way to 0 in its half second whichever step the
        /// run has moved on to, and the castle grows back while reviving; the run is told once it has grown back.
        /// </summary>
        internal void OnTicked(float seconds)
        {
            if (seconds <= 0f) return;
            if (_runFlow.IsPaused)
            {
                if (_runFlow.Step == RunStep.Paused) return;
                _view?.Advance(seconds);
                if (_reviving)
                {
                    _reviveLeft -= seconds;
                    if (_reviveLeft <= 0f) FinishRevive();
                }
                return;
            }
            _view?.Advance(seconds);
        }

        private void OnReviveStarted()
        {
            _body.Revive();
            _reviving = true;
            _reviveLeft = _config.ReviveGrowSeconds;
            if (_reviveJob == null) _reviveJob = TrackJob();
            if (_reviveLeft <= 0f) FinishRevive();
        }

        private void FinishRevive()
        {
            StopRevive();
            _revive.ReviveShown();
        }

        private void StopRevive()
        {
            _reviving = false;
            _reviveLeft = 0f;
            _reviveJob?.Dispose();
            _reviveJob = null;
        }

        private void RefreshCooldownJob()
        {
            if (_skills.AnyCooling)
            {
                if (_cooldownJob == null) _cooldownJob = TrackJob();
            }
            else if (_cooldownJob != null)
            {
                _cooldownJob.Dispose();
                _cooldownJob = null;
            }
        }

        private void StopJobs()
        {
            StopRevive();
            _cooldownJob?.Dispose();
            _cooldownJob = null;
        }

        private void OnRunStarted(RunTeam team)
        {
            _inBattle = false;
            StopRevive();
            _upgrades.Reset();
            _skills.Reset();
            _autoShot.Reset();
            _body.Reset();
            RefreshCooldownJob();
            if (_view == null) return;
            _view.ClearEffects();
            _view.ResetBody();
            _view.ShowDamaged(false);
            _view.SetVisible(true);
        }

        private void OnBattleStarted() => _inBattle = true;

        private void OnBattleStopped() => _inBattle = false;

        private void OnBattleCleared()
        {
            _inBattle = false;
            StopRevive();
            if (_view == null) return;
            _view.ClearEffects();
            _view.SetVisible(false);
        }

        private void OnEnemyDefeated(EnemyDefeat defeat) => _view?.DropShotsAt(defeat.Handle);

        private void OnStruck()
        {
            if (_view == null) return;
            _view.FlashHit(_config.HitFlashSeconds);
            if (_config.HitShakeSeconds > 0f) _view.ShakeHit(_config.HitShakeSeconds);
        }

        /// <summary>The damaged picture shows while the standing castle is under the damaged fraction of its max health.</summary>
        private void OnHealthChanged()
            => _view?.ShowDamaged(!_body.IsDestroyed && _body.Health < _body.MaxHealth * _config.DamagedHealthFraction);

        private void OnDestroyed() => _view?.Collapse(_config.CollapseSeconds);

        private void OnRevived() => _view?.GrowBack(_config.ReviveGrowSeconds);

        private void OnFired(int handle, float fromX, float toX) => _view?.ShowShot(handle, fromX, toX, _config.ShotFlightSeconds);

        private void OnThunderStruck(float x) => _view?.ShowThunder(x, _config.ThunderEffectSeconds);

        private void OnLevelApplied() => _view?.PopLevelUp(_config.LevelUpPopScale, _config.LevelUpPopSeconds);
    }
}
