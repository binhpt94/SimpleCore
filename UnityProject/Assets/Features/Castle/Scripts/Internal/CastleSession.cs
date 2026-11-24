using System;
using Game.Core;
using Game.Enemy;
using Game.Run;

namespace Game.Castle
{
    /// <summary>
    /// The castle's part in the scene session, and its presenter: makes the view in Run, resets the castle when a
    /// run begins, runs the castle's clock during a battle and pushes what happened down to the view.
    /// </summary>
    internal sealed class CastleSession : SessionParticipantBase
    {
        private readonly CastleConfig _config;
        private readonly IRunFlow _runFlow;
        private readonly IEnemyField _enemies;
        private readonly CastleBody _body;
        private readonly CastleSkills _skills;
        private readonly CastleUpgrades _upgrades;
        private readonly CastleAutoShot _autoShot;
        private readonly ICastleViewSpawner _spawner;
        private ICastleView _view;
        private bool _inBattle;

        public CastleSession(
            CastleConfig config, IRunFlow runFlow, IEnemyField enemies,
            CastleBody body, CastleSkills skills, CastleUpgrades upgrades, CastleAutoShot autoShot,
            ICastleViewSpawner spawner)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _runFlow = runFlow ?? throw new ArgumentNullException(nameof(runFlow));
            _enemies = enemies ?? throw new ArgumentNullException(nameof(enemies));
            _body = body ?? throw new ArgumentNullException(nameof(body));
            _skills = skills ?? throw new ArgumentNullException(nameof(skills));
            _upgrades = upgrades ?? throw new ArgumentNullException(nameof(upgrades));
            _autoShot = autoShot ?? throw new ArgumentNullException(nameof(autoShot));
            _spawner = spawner ?? throw new ArgumentNullException(nameof(spawner));
        }

        /// <summary>True between BattleStarted and BattleStopped: the castle shoots and its cooldowns run.</summary>
        public bool InBattle => _inBattle;

        protected override void OnRun()
        {
            _view = _spawner.Spawn();
            var view = _view;
            WhenDisposed(() => _spawner.Despawn(view));
            _view.Place(_config.HalfWidth, _config.Height);
            _view.SetVisible(false);

            _view.Ticked += OnTicked;
            WhenDisposed(() => view.Ticked -= OnTicked);
            _runFlow.RunStarted += OnRunStarted;
            WhenDisposed(() => _runFlow.RunStarted -= OnRunStarted);
            _runFlow.BattleStarted += OnBattleStarted;
            WhenDisposed(() => _runFlow.BattleStarted -= OnBattleStarted);
            _runFlow.BattleStopped += OnBattleStopped;
            WhenDisposed(() => _runFlow.BattleStopped -= OnBattleStopped);
            _runFlow.BattleCleared += OnBattleCleared;
            WhenDisposed(() => _runFlow.BattleCleared -= OnBattleCleared);
            _enemies.Defeated += OnEnemyDefeated;
            WhenDisposed(() => _enemies.Defeated -= OnEnemyDefeated);
            _body.Struck += OnStruck;
            WhenDisposed(() => _body.Struck -= OnStruck);
            _body.Destroyed += OnDestroyed;
            WhenDisposed(() => _body.Destroyed -= OnDestroyed);
            _autoShot.Fired += OnFired;
            WhenDisposed(() => _autoShot.Fired -= OnFired);
            _skills.ThunderStruck += OnThunderStruck;
            WhenDisposed(() => _skills.ThunderStruck -= OnThunderStruck);
            _upgrades.LevelApplied += OnLevelApplied;
            WhenDisposed(() => _upgrades.LevelApplied -= OnLevelApplied);
        }

        /// <summary>One frame of <paramref name="seconds"/>. Nothing moves while paused.</summary>
        internal void OnTicked(float seconds)
        {
            if (seconds <= 0f || _runFlow.IsPaused) return;
            if (_inBattle)
            {
                _skills.Tick(seconds);
                _autoShot.Tick(seconds);
            }
            _view?.Advance(seconds);
        }

        private void OnRunStarted(RunTeam team)
        {
            _inBattle = false;
            _upgrades.Reset();
            _skills.Reset();
            _autoShot.Reset();
            _body.Reset();
            if (_view == null) return;
            _view.ClearEffects();
            _view.ResetBody();
            _view.SetVisible(true);
        }

        private void OnBattleStarted() => _inBattle = true;

        private void OnBattleStopped() => _inBattle = false;

        private void OnBattleCleared()
        {
            _inBattle = false;
            if (_view == null) return;
            _view.ClearEffects();
            _view.SetVisible(false);
        }

        private void OnEnemyDefeated(EnemyDefeat defeat) => _view?.DropShotsAt(defeat.Handle);

        private void OnStruck() => _view?.FlashHit(_config.HitFlashSeconds);

        private void OnDestroyed() => _view?.Collapse(_config.CollapseSeconds);

        private void OnFired(int handle, float fromX, float toX) => _view?.ShowShot(handle, fromX, toX, _config.ShotFlightSeconds);

        private void OnThunderStruck(float x) => _view?.ShowThunder(x, _config.ThunderEffectSeconds);

        private void OnLevelApplied() => _view?.PopLevelUp(_config.LevelUpPopScale, _config.LevelUpPopSeconds);
    }
}
