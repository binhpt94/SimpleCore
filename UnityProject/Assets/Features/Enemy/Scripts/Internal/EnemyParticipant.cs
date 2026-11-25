using System;
using Game.Combat;
using Game.Core;
using Game.Run;
using Game.SimClock;

namespace Game.Enemy
{
    /// <summary>
    /// Enemy's part in the scene session: follows the run's battle signals, steps the enemies on the shared clock
    /// and opens the stage that draws them while the session runs.
    /// </summary>
    internal sealed class EnemyParticipant : SessionParticipantBase
    {
        private readonly EnemyField _field;
        private readonly IRunFlow _runFlow;
        private readonly ISimClock _clock;
        private readonly ICombat _combat;
        private readonly Func<IEnemyStage> _openStage;

        public EnemyParticipant(EnemyField field, IRunFlow runFlow, ISimClock clock, ICombat combat,
            Func<IEnemyStage> openStage)
        {
            _field = field ?? throw new ArgumentNullException(nameof(field));
            _runFlow = runFlow ?? throw new ArgumentNullException(nameof(runFlow));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _combat = combat ?? throw new ArgumentNullException(nameof(combat));
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
            stage.TrackWith(TrackView);
            WhenDisposed(stage.Close);

            // Following the shared clock and the combat ticks is the one job that keeps running.
            var stepJob = TrackJob();
            var presenter = new EnemyPresenter(_field, _runFlow, _clock, _combat, stage);
            WhenDisposed(() =>
            {
                presenter.Dispose();
                stepJob.Dispose();
            });
        }
    }
}
