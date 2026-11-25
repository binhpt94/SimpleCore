using System;
using System.Collections.Generic;
using Game.Run;

namespace Game.Enemy
{
    /// <summary>
    /// Moves the field on every frame while the battle runs and is not paused, and pushes what changed to the stage.
    /// </summary>
    internal sealed class EnemyPresenter : IDisposable
    {
        private readonly EnemyField _field;
        private readonly IRunFlow _runFlow;
        private readonly IEnemyStage _stage;
        private readonly List<EnemyLook> _looks = new List<EnemyLook>();

        public EnemyPresenter(EnemyField field, IRunFlow runFlow, IEnemyStage stage)
        {
            _field = field ?? throw new ArgumentNullException(nameof(field));
            _runFlow = runFlow ?? throw new ArgumentNullException(nameof(runFlow));
            _stage = stage ?? throw new ArgumentNullException(nameof(stage));

            _field.Spawned += OnSpawned;
            _field.Struck += OnStruck;
            _field.Shot += OnShot;
            _field.Rewarded += OnRewarded;
            _field.Hurt += OnHurt;
            _field.Defeated += OnDefeated;
            _field.Cleared += OnCleared;
            _runFlow.StepChanged += OnStepChanged;
            _stage.Frame += OnFrame;

            _field.GetLooks(_looks);
            for (var i = 0; i < _looks.Count; i++) _stage.Show(_looks[i]);
            _stage.SetPaused(_runFlow.IsPaused);
        }

        public void Dispose()
        {
            _field.Spawned -= OnSpawned;
            _field.Struck -= OnStruck;
            _field.Shot -= OnShot;
            _field.Rewarded -= OnRewarded;
            _field.Hurt -= OnHurt;
            _field.Defeated -= OnDefeated;
            _field.Cleared -= OnCleared;
            _runFlow.StepChanged -= OnStepChanged;
            _stage.Frame -= OnFrame;
        }

        /// <summary>One frame: nothing moves while paused or after the battle stopped.</summary>
        /// <remarks>
        /// The stage's frozen state is worked out again on every frame, so it never depends on the order in which
        /// the run's signals arrive (on replay StepChanged comes before RunStarted resets the field).
        /// </remarks>
        public void OnFrame(float seconds)
        {
            var frozen = _runFlow.IsPaused || _field.IsStopped;
            _stage.SetPaused(frozen);
            if (frozen) return;
            _field.Tick(seconds);
            _field.GetLooks(_looks);
            for (var i = 0; i < _looks.Count; i++) _stage.Move(_looks[i]);
        }

        private void OnSpawned(EnemyLook look) => _stage.Show(look);
        private void OnStruck(int handle, int direction) => _stage.Lunge(handle, direction);
        private void OnShot(EnemyShot shot) => _stage.Shoot(shot);
        private void OnRewarded(EnemyReward reward) => _stage.ShowCoins(reward);
        private void OnHurt(int handle) => _stage.Flash(handle);
        private void OnDefeated(EnemyDefeat defeat) => _stage.Defeat(defeat.Handle);
        private void OnCleared() => _stage.RemoveAll();
        private void OnStepChanged(RunStep step) => _stage.SetPaused(_runFlow.IsPaused || _field.IsStopped);
    }
}
