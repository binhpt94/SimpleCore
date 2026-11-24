using System;
using System.Collections.Generic;
using Game.Enemy;
using Game.Run;
using Game.Wallet;

namespace Game.Wave
{
    /// <summary>
    /// Runs the waves of a battle: starts wave 1 when the battle starts, sends the enemies of the wave one by one,
    /// notices the clear, pays the reward, counts the wave for the run, then waits the break and starts the next wave.
    /// Driven by <see cref="Tick"/>; knows nothing of Unity.
    /// </summary>
    internal sealed class WaveDirector : IWaveProgress
    {
        private enum Phase
        {
            Idle,
            Sending,
            Break,
            Stopped,
        }

        private readonly WaveRules _rules;
        private readonly WaveFormula _formula;
        private readonly SpawnOrder _order;
        private readonly IRunFlow _run;
        private readonly IEnemyField _enemies;
        private readonly IWalletIncome _income;

        private Phase _phase = Phase.Idle;
        private List<SpawnEntry> _list = new List<SpawnEntry>();
        private WavePlan _plan;
        private int _nextIndex;
        private float _untilNext;
        private bool _held;
        private int _alive;
        private float _breakLeft;
        private bool _clearPending;

        public WaveDirector(WaveRules rules, IRunFlow run, IEnemyField enemies, IWalletIncome income, IWaveRandom random)
        {
            _rules = rules ?? throw new ArgumentNullException(nameof(rules));
            _run = run ?? throw new ArgumentNullException(nameof(run));
            _enemies = enemies ?? throw new ArgumentNullException(nameof(enemies));
            _income = income ?? throw new ArgumentNullException(nameof(income));
            _formula = new WaveFormula(rules);
            _order = new SpawnOrder(rules, random ?? throw new ArgumentNullException(nameof(random)));
        }

        public int CurrentWave { get; private set; } = 1;

        public event Action<int> CurrentWaveChanged;
        public event Action<int> WaveStarted;
        public event Action<int> WaveCleared;

        /// <summary>Enemies of the current wave that came in and were not defeated yet.</summary>
        internal int Alive => _alive;

        /// <summary>Enemies of the current wave still waiting to come in.</summary>
        internal int Waiting => _phase == Phase.Sending ? _list.Count - _nextIndex : 0;

        internal bool InBreak => _phase == Phase.Break;

        public void Attach()
        {
            _run.RunStarted += OnRunStarted;
            _run.BattleStarted += OnBattleStarted;
            _run.BattleStopped += OnBattleStopped;
            _run.BattleCleared += OnBattleStopped;
            _enemies.Defeated += OnDefeated;
        }

        public void Detach()
        {
            _run.RunStarted -= OnRunStarted;
            _run.BattleStarted -= OnBattleStarted;
            _run.BattleStopped -= OnBattleStopped;
            _run.BattleCleared -= OnBattleStopped;
            _enemies.Defeated -= OnDefeated;
        }

        /// <summary>One update step of <paramref name="deltaSeconds"/> of battle time.</summary>
        public void Tick(float deltaSeconds)
        {
            // A clear noticed during the last step is settled now, after every feature had its say in that step:
            // if the castle fell in the same step the battle is already stopped and the clear does not count.
            if (_clearPending)
            {
                _clearPending = false;
                if (_phase == Phase.Sending) ClearWave();
            }

            if (_run.IsPaused || deltaSeconds <= 0f) return;

            if (_phase == Phase.Sending) Send(deltaSeconds);
            else if (_phase == Phase.Break)
            {
                _breakLeft -= deltaSeconds;
                if (_breakLeft <= 0f) StartWave(CurrentWave);
            }
        }

        private void OnRunStarted(RunTeam team)
        {
            _phase = Phase.Idle;
            _list.Clear();
            _nextIndex = 0;
            _alive = 0;
            _clearPending = false;
            CurrentWave = 1;
            CurrentWaveChanged?.Invoke(CurrentWave);
        }

        private void OnBattleStarted()
        {
            if (_phase != Phase.Idle) return;
            StartWave(CurrentWave);
        }

        private void OnBattleStopped()
        {
            _phase = Phase.Stopped;
            _clearPending = false;
            _list.Clear();
            _nextIndex = 0;
        }

        private void OnDefeated(EnemyDefeat defeat)
        {
            if (_phase != Phase.Sending || _alive <= 0) return;
            _alive--;
            if (_alive == 0 && _nextIndex >= _list.Count) _clearPending = true;
        }

        private void StartWave(int wave)
        {
            _plan = _formula.Plan(wave);
            _list = _order.Build(_plan);
            _nextIndex = 0;
            _alive = 0;
            _untilNext = 0f;
            _held = false;
            _phase = Phase.Sending;
            WaveStarted?.Invoke(wave);
            Send(0f);
        }

        private void Send(float deltaSeconds)
        {
            if (!_held) _untilNext -= deltaSeconds;
            while (_phase == Phase.Sending && _nextIndex < _list.Count && _untilNext <= 0f)
            {
                if (_alive >= _rules.AliveCap)
                {
                    // Too many on the field: the clock stands still until one falls, then the next comes at once.
                    _held = true;
                    _untilNext = 0f;
                    return;
                }

                var entry = _list[_nextIndex++];
                _alive++;
                _untilNext = _held ? _plan.Interval : _untilNext + _plan.Interval;
                _held = false;
                _enemies.Spawn(new EnemySpawn(entry.Type, entry.Side, _plan.StatScale));
            }
        }

        private void ClearWave()
        {
            var wave = _plan.Number;
            _phase = Phase.Break;
            _breakLeft = _rules.GapSeconds;

            _income.Add(_rules.ClearRewardBase + _rules.ClearRewardPerWave * wave);
            _run.CountClearedWave(wave);
            WaveCleared?.Invoke(wave);

            CurrentWave = wave + 1;
            CurrentWaveChanged?.Invoke(CurrentWave);
        }
    }
}
