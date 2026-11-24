using System;
using Game.Core;
using Game.Run;

namespace Game.Enemy
{
    /// <summary>
    /// Enemy's part in the scene session: follows the run's battle signals, and opens the stage that draws and
    /// moves the enemies while the session runs.
    /// </summary>
    internal sealed class EnemyParticipant : SessionParticipantBase
    {
        private readonly EnemyField _field;
        private readonly IRunFlow _runFlow;
        private readonly Func<IEnemyStage> _openStage;

        public EnemyParticipant(EnemyField field, IRunFlow runFlow, Func<IEnemyStage> openStage)
        {
            _field = field ?? throw new ArgumentNullException(nameof(field));
            _runFlow = runFlow ?? throw new ArgumentNullException(nameof(runFlow));
            _openStage = openStage ?? throw new ArgumentNullException(nameof(openStage));
        }

        protected override void OnRun()
        {
            Action<RunTeam> onRunStarted = _ => _field.ResetForRun();
            Action onStopped = _field.Stop;
            Action onCleared = _field.Clear;

            _runFlow.RunStarted += onRunStarted;
            _runFlow.BattleStopped += onStopped;
            _runFlow.BattleCleared += onCleared;
            WhenDisposed(() => _runFlow.RunStarted -= onRunStarted);
            WhenDisposed(() => _runFlow.BattleStopped -= onStopped);
            WhenDisposed(() => _runFlow.BattleCleared -= onCleared);

            var stage = _openStage();
            WhenDisposed(stage.Close);
            var presenter = new EnemyPresenter(_field, _runFlow, stage);
            WhenDisposed(presenter.Dispose);
        }
    }
}
