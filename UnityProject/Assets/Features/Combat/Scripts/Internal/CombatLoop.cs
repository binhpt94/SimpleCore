using System;
using Game.Run;
using Game.SimClock;

namespace Game.Combat
{
    /// <summary>
    /// Runs exactly one combat tick on every step of the shared battle clock (feature-sim-clock), at the place of
    /// <see cref="Owner"/> in the clock's step order. Combat keeps no clock of its own: the step length comes from
    /// the clock. No tick runs before BattleStarted, after BattleStopped or BattleCleared, on a step that is not
    /// gameplay.
    /// </summary>
    internal sealed class CombatLoop : IDisposable
    {
        /// <summary>Name of combat in the shared clock's step order.</summary>
        public const string Owner = "Combat";

        private readonly CombatTick _tick;
        private readonly IRunFlow _run;
        private readonly IDisposable _step;
        private bool _inBattle;

        public CombatLoop(CombatTick tick, ISimClock clock, IRunFlow run)
        {
            _tick = tick ?? throw new ArgumentNullException(nameof(tick));
            _run = run ?? throw new ArgumentNullException(nameof(run));
            if (clock == null) throw new ArgumentNullException(nameof(clock));
            _run.RunStarted += OnRunStarted;
            _run.BattleStarted += OnBattleStarted;
            _run.BattleStopped += OnBattleEnded;
            _run.BattleCleared += OnBattleEnded;
            _step = clock.OnStep(Owner, OnStep);
        }

        /// <summary>Ticks run since this loop was made; read by tests and dev tools only.</summary>
        public long TicksRun { get; private set; }

        public void Dispose()
        {
            _step?.Dispose();
            _run.RunStarted -= OnRunStarted;
            _run.BattleStarted -= OnBattleStarted;
            _run.BattleStopped -= OnBattleEnded;
            _run.BattleCleared -= OnBattleEnded;
        }

        private void OnStep(SimStep step)
        {
            if (!step.Gameplay || !_inBattle) return;
            _tick.Run(step.Seconds);
            TicksRun++;
        }

        private void OnRunStarted(RunTeam team)
        {
            _inBattle = false;
            _tick.ClearPending();
        }

        private void OnBattleStarted() => _inBattle = true;

        private void OnBattleEnded()
        {
            _inBattle = false;
            _tick.ClearPending();
        }
    }
}
