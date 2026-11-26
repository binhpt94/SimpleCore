using System;
using System.Collections.Generic;
using Game.Combat;
using Game.Run;
using Game.SimClock;

namespace Game.Enemy
{
    /// <summary>
    /// Shows the field on the stage. The enemies walk and hit inside the combat tick (the field is a combat actor);
    /// after each tick the presenter pushes where they stand. On each step of the shared clock it works out whether
    /// the stage is frozen (a step that is not gameplay, battle stopped).
    /// </summary>
    internal sealed class EnemyPresenter : IDisposable
    {
        /// <summary>This feature's name in the shared clock's step order.</summary>
        internal const string ClockOwner = "Enemy";

        private readonly EnemyField _field;
        private readonly IRunFlow _runFlow;
        private readonly ICombat _combat;
        private readonly IEnemyStage _stage;
        private readonly IDisposable _stepHandle;
        private readonly List<EnemyLook> _looks = new List<EnemyLook>();

        public EnemyPresenter(EnemyField field, IRunFlow runFlow, ISimClock clock, ICombat combat, IEnemyStage stage)
        {
            _field = field ?? throw new ArgumentNullException(nameof(field));
            _runFlow = runFlow ?? throw new ArgumentNullException(nameof(runFlow));
            if (clock == null) throw new ArgumentNullException(nameof(clock));
            _combat = combat ?? throw new ArgumentNullException(nameof(combat));
            _stage = stage ?? throw new ArgumentNullException(nameof(stage));

            _field.Spawned += OnSpawned;
            _field.Struck += OnStruck;
            _field.Shot += OnShot;
            _field.Rewarded += OnRewarded;
            _field.Hurt += OnHurt;
            _field.Entered += OnEntered;
            _field.Defeated += OnDefeated;
            _field.Cleared += OnCleared;
            _runFlow.StepChanged += OnStepChanged;
            _combat.TickDone += OnTickDone;

            _field.GetLooks(_looks);
            for (var i = 0; i < _looks.Count; i++) _stage.Show(_looks[i]);
            _stage.SetFrozen(_field.IsStopped);

            _stepHandle = clock.OnStep(ClockOwner, OnStep);
        }

        public void Dispose()
        {
            _stepHandle.Dispose();
            _field.Spawned -= OnSpawned;
            _field.Struck -= OnStruck;
            _field.Shot -= OnShot;
            _field.Rewarded -= OnRewarded;
            _field.Hurt -= OnHurt;
            _field.Entered -= OnEntered;
            _field.Defeated -= OnDefeated;
            _field.Cleared -= OnCleared;
            _runFlow.StepChanged -= OnStepChanged;
            _combat.TickDone -= OnTickDone;
        }

        /// <summary>One step of the shared clock: the stage is frozen on a step that is not gameplay or after the battle stopped.</summary>
        /// <remarks>
        /// The stage's frozen state is worked out again on every step, so it never depends on the order in which
        /// the run's signals arrive (on replay StepChanged comes before RunStarted resets the field).
        /// </remarks>
        private void OnStep(SimStep step) =>
            _stage.SetFrozen(!step.Gameplay || _field.IsStopped);

        /// <summary>A combat tick is over: every enemy walked and hit, the fallen are gone; draw where they stand.</summary>
        private void OnTickDone()
        {
            _field.GetLooks(_looks);
            for (var i = 0; i < _looks.Count; i++) _stage.Move(_looks[i]);
        }

        private void OnSpawned(EnemyLook look) => _stage.Show(look);
        private void OnStruck(int handle, int direction) => _stage.Lunge(handle, direction);
        private void OnShot(EnemyShot shot) => _stage.Shoot(shot);
        private void OnRewarded(EnemyReward reward) => _stage.ShowCoins(reward);
        private void OnHurt(int handle) => _stage.Flash(handle);
        private void OnEntered(int handle) => _stage.Enter(handle);
        private void OnDefeated(EnemyDefeat defeat) => _stage.Defeat(defeat.Handle);
        private void OnCleared() => _stage.RemoveAll();
        private void OnStepChanged(RunStep step) => _stage.SetFrozen(_field.IsStopped);
    }
}
