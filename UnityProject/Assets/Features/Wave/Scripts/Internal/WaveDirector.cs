using System;
using System.Collections.Generic;
using Game.Enemy;
using Game.Run;
using Game.SimClock;
using Game.Wallet;

namespace Game.Wave
{
    /// <summary>
    /// Runs the waves of a battle: starts wave 1 on the first gameplay step after the battle starts, sends the enemies
    /// of the wave one by one, notices the clear, pays the reward, counts the wave for the run, then waits the break
    /// and starts the next wave.
    /// Driven by <see cref="Step"/>, one call per step of the shared clock (owner "Wave", first in the step order);
    /// knows nothing of Unity and never reads frame time.
    /// </summary>
    internal sealed class WaveDirector : IWaveProgress
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        , IWaveCheat
#endif
    {
        /// <summary>Slack for comparing summed step lengths (0.025 s is not exact in binary) with a wait.</summary>
        private const double Epsilon = 1e-6;

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
        private double _untilNext;
        private bool _held;
        private int _alive;
        private double _breakLeft;
        private bool _clearPending;
        private bool _startPending;

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

        /// <summary>Raised at the end of every <see cref="Step"/>, after Wave did its part of that step.</summary>
        internal event Action Stepped;

        /// <summary>Enemies of the current wave that came in and were not defeated yet.</summary>
        internal int Alive => _alive;

        /// <summary>Enemies of the current wave still waiting to come in.</summary>
        internal int Waiting => _phase == Phase.Sending ? _list.Count - _nextIndex : 0;

        internal bool InBreak => _phase == Phase.Break;

        /// <summary>True while a wave is being sent or the break runs: a send or break clock is then waiting on the steps.</summary>
        internal bool IsRunning => _phase == Phase.Sending || _phase == Phase.Break;

        /// <summary>Raised when <see cref="IsRunning"/> turns on (a wave began) or off (the battle stopped or a new run reset).</summary>
        internal event Action<bool> RunningChanged;

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

        /// <summary>
        /// Wave's part of one step of the shared clock. Wave runs first in the step, so it sees the defeats and the
        /// run's verdict of the previous step (spawn-after-results). Waits only count gameplay steps: on a step that is
        /// not gameplay (paused, waiting for a revive) the send clock and the break stand still.
        /// </summary>
        public void Step(SimStep step)
        {
            // A clear noticed during the last step is settled now, after every feature had its say in that step:
            // if the castle fell in the same step the battle is already stopped and the clear does not count.
            // If the castle fell for the first time in that step, the run is in its revive flow: the clear is held until
            // the revive is done (then it pays and the break starts) or dropped when the run is lost (BattleStopped).
            var clearedNow = false;
            if (_clearPending && !HeldByRevive)
            {
                _clearPending = false;
                if (_phase == Phase.Sending)
                {
                    ClearWave();
                    clearedNow = true;
                }
            }

            if (step.Gameplay)
            {
                if (_startPending)
                {
                    _startPending = false;
                    BeginWave(CurrentWave, 0);
                }
                else if (!clearedNow)
                {
                    // The break starts counting from the step after the clear.
                    Advance(step.Seconds);
                }
            }

            Stepped?.Invoke();
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        /// <summary>
        /// Dev cheat: drops the rest of the current wave (or the break) and starts <paramref name="wave"/> at once.
        /// Enemies still on the field stay and keep counting as alive, so the clear of the new wave still waits for them.
        /// Skipped waves give no reward and no score. Ignored outside a running battle.
        /// </summary>
        public void StartWave(int wave)
        {
            if (wave < 1) throw new ArgumentOutOfRangeException(nameof(wave), wave, "Waves start at 1.");
            if (!IsRunning) return;

            var stillOnField = _phase == Phase.Sending ? _alive : 0;
            _clearPending = false;
            CurrentWave = wave;
            CurrentWaveChanged?.Invoke(CurrentWave);
            BeginWave(wave, stillOnField);
        }
#endif

        private void OnRunStarted(RunTeam team)
        {
            SetPhase(Phase.Idle);
            _list.Clear();
            _nextIndex = 0;
            _alive = 0;
            _clearPending = false;
            _startPending = false;
            CurrentWave = 1;
            CurrentWaveChanged?.Invoke(CurrentWave);
        }

        private void OnBattleStarted()
        {
            // Wave 1 begins on the first gameplay step, not in the middle of whatever raised the event.
            if (_phase == Phase.Idle) _startPending = true;
        }

        private void OnBattleStopped()
        {
            SetPhase(Phase.Stopped);
            _clearPending = false;
            _startPending = false;
            _list.Clear();
            _nextIndex = 0;
        }

        private void OnDefeated(EnemyDefeat defeat)
        {
            if (_phase != Phase.Sending || _alive <= 0) return;
            _alive--;
            if (_alive == 0 && _nextIndex >= _list.Count) _clearPending = true;
        }

        /// <param name="alreadyAlive">Enemies already on the field that the clear of this wave must also wait for.</param>
        private void BeginWave(int wave, int alreadyAlive)
        {
            _plan = _formula.Plan(wave);
            // Kinds and sides are all drawn here, once per wave: an enemy held at the alive cap keeps what was drawn for it.
            _list = _order.Build(_plan);
            _nextIndex = 0;
            _alive = alreadyAlive;
            _untilNext = 0d;
            _held = false;
            SetPhase(Phase.Sending);
            WaveStarted?.Invoke(wave);
            Send(0d);
        }

        /// <summary>
        /// True while the run is in its revive flow: the run holds the clear of that step
        /// (feature-wave/wave-clear/clear-rules#edge-thanh-bi-pha-cung-luc-con-cuoi-bi, feature-run/run-flow/flow-rules).
        /// </summary>
        private bool HeldByRevive =>
            _run.Step == RunStep.ReviveWait || _run.Step == RunStep.ReviveOffer || _run.Step == RunStep.Reviving;

        private void SetPhase(Phase phase)
        {
            var wasRunning = IsRunning;
            _phase = phase;
            if (IsRunning != wasRunning) RunningChanged?.Invoke(IsRunning);
        }

        private void Advance(double seconds)
        {
            if (_phase == Phase.Sending) Send(seconds);
            else if (_phase == Phase.Break)
            {
                _breakLeft -= seconds;
                if (_breakLeft <= Epsilon) BeginWave(CurrentWave, 0);
            }
        }

        private void Send(double seconds)
        {
            if (!_held) _untilNext -= seconds;
            while (_phase == Phase.Sending && _nextIndex < _list.Count && _untilNext <= Epsilon)
            {
                if (_alive >= _rules.AliveCap)
                {
                    // Too many on the field: the clock stands still until one falls, then the next comes at once.
                    _held = true;
                    _untilNext = 0d;
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
            SetPhase(Phase.Break);
            _breakLeft = _rules.GapSeconds;

            _income.Add(_rules.ClearRewardBase + _rules.ClearRewardPerWave * wave);
            _run.CountClearedWave(wave);
            WaveCleared?.Invoke(wave);

            CurrentWave = wave + 1;
            CurrentWaveChanged?.Invoke(CurrentWave);
        }
    }
}
