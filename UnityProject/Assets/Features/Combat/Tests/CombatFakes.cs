using System;
using System.Collections.Generic;
using Game.Run;

namespace Game.Combat.Tests
{
    /// <summary>Shared call log, so tests can check the order of a tick's parts across fakes.</summary>
    internal sealed class CallLog
    {
        public readonly List<string> Calls = new List<string>();

        public void Add(string call) => Calls.Add(call);
    }

    /// <summary>Stand-in for a feature that owns hit points (heroes, enemies, the castle).</summary>
    internal sealed class FakeOwner : ICombatReceiver
    {
        private readonly string _name;
        private readonly CallLog _log;
        private readonly Dictionary<int, float> _health = new Dictionary<int, float>();
        private readonly HashSet<int> _unhittable = new HashSet<int>();
        private readonly List<int> _fellThisTick = new List<int>();

        public FakeOwner(string name, CallLog log = null)
        {
            _name = name;
            _log = log ?? new CallLog();
        }

        public readonly List<(int key, float total)> Totals = new List<(int, float)>();
        public readonly List<int> Fallen = new List<int>();

        /// <summary>Health of every side, as seen at the moment the owner reported a fall.</summary>
        public Action<FakeOwner, int> OnFall;

        public void Add(int key, float health) => _health[key] = health;

        public void SetUnhittable(int key) => _unhittable.Add(key);

        public float Health(int key) => _health[key];

        public void Heal(int key, float amount) => _health[key] += amount;

        public bool CanBeHit(int key)
        {
            _log.Add($"{_name}.CanBeHit({key})");
            return _health.ContainsKey(key) && _health[key] > 0f && !_unhittable.Contains(key);
        }

        public void TakeTotal(int key, float total)
        {
            _log.Add($"{_name}.TakeTotal({key})");
            Totals.Add((key, total));
            var before = _health[key];
            _health[key] = before - total;
            if (before > 0f && _health[key] <= 0f) _fellThisTick.Add(key);
        }

        public void ReportFallen()
        {
            _log.Add($"{_name}.ReportFallen");
            foreach (var key in _fellThisTick)
            {
                Fallen.Add(key);
                OnFall?.Invoke(this, key);
            }
            _fellThisTick.Clear();
        }
    }

    /// <summary>Stand-in for a feature that moves and attacks.</summary>
    internal sealed class FakeActor : ICombatActor
    {
        private readonly string _name;
        private readonly CallLog _log;

        public FakeActor(string name, CallLog log = null)
        {
            _name = name;
            _log = log ?? new CallLog();
        }

        public Action OnAim;
        public Action OnStrike;
        public readonly List<float> Steps = new List<float>();

        public void Aim(float stepSeconds)
        {
            _log.Add($"{_name}.Aim");
            Steps.Add(stepSeconds);
            OnAim?.Invoke();
        }

        public void Strike(float stepSeconds)
        {
            _log.Add($"{_name}.Strike");
            OnStrike?.Invoke();
        }
    }

    internal sealed class FakeTime : ITime
    {
        public float DeltaTime { get; set; }
    }

    /// <summary>Stand-in for the run feature: only what combat reads.</summary>
    internal sealed class FakeRunFlow : IRunFlow
    {
        public RunStep Step { get; set; } = RunStep.Playing;
        public RunTeam Team => default;
        public bool IsPaused => Step == RunStep.Paused || Step == RunStep.ReviveWait
                                || Step == RunStep.ReviveOffer || Step == RunStep.Reviving;

        public event Action<RunStep> StepChanged;
        public event Action<RunTeam> RunStarted;
        public event Action BattleStarted;
        public event Action BattleStopped;
        public event Action BattleCleared;

        public void StartRun() => RunStarted?.Invoke(default);
        public void StartBattle() => BattleStarted?.Invoke();
        public void StopBattle() => BattleStopped?.Invoke();
        public void ClearBattle() => BattleCleared?.Invoke();

        public void SetStep(RunStep step)
        {
            Step = step;
            StepChanged?.Invoke(step);
        }

        public void Play() { }
        public void EnterBattle(RunTeam team) { }
        public void Pause() => SetStep(RunStep.Paused);
        public void Resume() => SetStep(RunStep.Playing);
        public void Abandon() { }
        public void Replay() { }
        public void GoHome() { }
        public void Lose() => SetStep(RunStep.ReviveWait);
        public void CountClearedWave(int wave) { }
    }
}
