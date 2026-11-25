using System;
using Game.Run;

namespace Game.Combat
{
    /// <summary>
    /// Runs combat ticks from frame time while the battle is on: from BattleStarted until BattleStopped or
    /// BattleCleared, and never while the run is paused (pause popup, waiting for a revive).
    /// </summary>
    internal sealed class CombatLoop : IDisposable
    {
        private readonly CombatTick _tick;
        private readonly FixedStepClock _clock;
        private readonly IRunFlow _run;
        private readonly ITime _time;
        private bool _inBattle;

        public CombatLoop(CombatTick tick, FixedStepClock clock, IRunFlow run, ITime time)
        {
            _tick = tick ?? throw new ArgumentNullException(nameof(tick));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _run = run ?? throw new ArgumentNullException(nameof(run));
            _time = time ?? throw new ArgumentNullException(nameof(time));
            _run.RunStarted += OnRunStarted;
            _run.BattleStarted += OnBattleStarted;
            _run.BattleStopped += OnBattleEnded;
            _run.BattleCleared += OnBattleEnded;
        }

        /// <summary>Called once per frame; returns how many ticks ran.</summary>
        public int Frame()
        {
            if (!CanTick) return 0;
            var steps = _clock.Advance(_time.DeltaTime);
            var ran = 0;
            // A tick can end the run or freeze it (castle destroyed): no further tick in this frame.
            while (ran < steps && CanTick)
            {
                _tick.Run(_clock.StepSeconds);
                ran++;
            }
            return ran;
        }

        public void Dispose()
        {
            _run.RunStarted -= OnRunStarted;
            _run.BattleStarted -= OnBattleStarted;
            _run.BattleStopped -= OnBattleEnded;
            _run.BattleCleared -= OnBattleEnded;
        }

        private bool CanTick => _inBattle && !_run.IsPaused;

        private void OnRunStarted(RunTeam team)
        {
            _inBattle = false;
            _clock.Reset();
            _tick.ClearPending();
        }

        private void OnBattleStarted()
        {
            _inBattle = true;
            _clock.Reset();
        }

        private void OnBattleEnded()
        {
            _inBattle = false;
            _clock.Reset();
            _tick.ClearPending();
        }
    }
}
