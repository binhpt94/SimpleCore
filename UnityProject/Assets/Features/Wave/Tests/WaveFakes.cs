using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Game.Core;
using Game.Enemy;
using Game.Run;
using Game.SimClock;
using Game.Wallet;

namespace Game.Wave.Tests
{
    internal sealed class FakeRun : IRunFlow
    {
        public readonly List<string> Log;
        public FakeRun(List<string> log) { Log = log; }

        public RunStep Step { get; set; } = RunStep.Playing;
        public RunTeam Team => default;
        public bool IsPaused { get; set; }

        public event Action<RunStep> StepChanged { add { } remove { } }
        public event Action<RunTeam> RunStarted;
        public event Action BattleStarted;
        public event Action BattleStopped;
        public event Action BattleCleared;

        public void StartBattle()
        {
            RunStarted?.Invoke(new RunTeam(new[] { "hero" }));
            BattleStarted?.Invoke();
        }

        public void StopBattle() => BattleStopped?.Invoke();
        public void ClearBattle() => BattleCleared?.Invoke();

        public void Play() { }
        public void EnterBattle(RunTeam team) { }
        public void Pause()
        {
            IsPaused = true;
            Log.Add("pause");
        }
        public void Resume() { }
        public void Abandon() { }
        public void Replay() { }
        public void GoHome() { }
        public void Lose() => BattleStopped?.Invoke();
        public void CountClearedWave(int wave) => Log.Add("score " + wave);
    }

    internal sealed class FakeEnemies : IEnemyField
    {
        public readonly List<EnemySpawn> Spawned = new List<EnemySpawn>();
        private int _defeated;

        public int AliveCount => Spawned.Count - _defeated;
        public event Action<EnemyDefeat> Defeated;

        public void Spawn(EnemySpawn spawn) => Spawned.Add(spawn);

        /// <summary>Defeats <paramref name="count"/> enemies that are on the field.</summary>
        public void Defeat(int count)
        {
            for (var i = 0; i < count; i++)
            {
                _defeated++;
                Defeated?.Invoke(new EnemyDefeat(_defeated, EnemyType.Soldier, EnemySide.Left));
            }
        }

        public void DefeatAll() => Defeat(AliveCount);

        public void GetAlive(List<EnemyTarget> into) => into.Clear();
        public bool Damage(int handle, float amount) => false;
        public void SlowAll(float seconds) { }
        public void AddFoes(IEnemyFoes foes) { }
    }

    internal sealed class FakeIncome : IWalletIncome
    {
        public readonly List<string> Log;
        public FakeIncome(List<string> log) { Log = log; }
        public void Add(int amount) => Log.Add("coins " + amount);
    }

    /// <summary>Always the first choice: keeps orders the same from run to run.</summary>
    internal sealed class FirstRandom : IWaveRandom
    {
        public int Next(int maxExclusive) => 0;
    }

    /// <summary>Seeded test numbers (System.Random), standing in for a stream of the run in order-only tests.</summary>
    internal sealed class SystemWaveRandom : IWaveRandom
    {
        private readonly Random _random;
        public SystemWaveRandom(int seed) { _random = new Random(seed); }
        public int Next(int maxExclusive) => _random.Next(maxExclusive);
    }

    /// <summary>Stand-in for SimClock's seeded streams: one stream per name, made from the seed and the name; counts draws.</summary>
    internal sealed class FakeSimRandom : ISimRandom
    {
        public readonly List<string> Asked = new List<string>();
        private readonly Dictionary<string, FakeStream> _streams = new Dictionary<string, FakeStream>();
        private readonly int _seed;

        public FakeSimRandom(int seed) { _seed = seed; }

        public ISimRandomStream Stream(string name)
        {
            Asked.Add(name);
            if (!_streams.TryGetValue(name, out var stream))
            {
                var mixed = _seed;
                foreach (var c in name) mixed = unchecked(mixed * 31 + c);
                _streams[name] = stream = new FakeStream(mixed);
            }
            return stream;
        }

        public int Draws(string name) => _streams.TryGetValue(name, out var s) ? s.Draws : 0;

        private sealed class FakeStream : ISimRandomStream
        {
            private readonly Random _random;
            public int Draws;
            public FakeStream(int seed) { _random = new Random(seed); }
            public int Next(int maxExclusive)
            {
                Draws++;
                return _random.Next(maxExclusive);
            }
        }
    }

    /// <summary>
    /// The shared clock stepped by hand, like the real one in fixed-tick mode: 0.025 s steps, left-over time kept
    /// (counted in whole microseconds so it never drifts). A step is gameplay unless <see cref="Paused"/> says otherwise.
    /// </summary>
    internal sealed class FakeSimClock : ISimClock
    {
        private const long StepMicros = 25000;

        public readonly List<string> Owners = new List<string>();
        private readonly List<Action<SimStep>> _steps = new List<Action<SimStep>>();
        private long _leftMicros;

        public Func<bool> Paused = () => false;

        public SimMode Mode => SimMode.FixedTick;
        public float StepSeconds => 0.025f;
        public long StepCount { get; private set; }
        public long GameplayStepCount { get; private set; }
        public double GameplaySeconds => GameplayStepCount * 0.025;
        public int Subscribers => _steps.Count;

        public IDisposable OnStep(string owner, Action<SimStep> onStep)
        {
            Owners.Add(owner);
            _steps.Add(onStep);
            return new Handle(() => _steps.Remove(onStep));
        }

        public void Enqueue(string owner, Action apply) => apply();

        /// <summary>Runs every whole step that fits in <paramref name="seconds"/> plus the time left over before.</summary>
        public void Seconds(double seconds)
        {
            _leftMicros += (long)Math.Round(seconds * 1e6);
            while (_leftMicros >= StepMicros)
            {
                _leftMicros -= StepMicros;
                Steps(1);
            }
        }

        /// <summary>Runs exactly <paramref name="count"/> steps.</summary>
        public void Steps(int count)
        {
            for (var i = 0; i < count; i++)
            {
                var gameplay = !Paused();
                StepCount++;
                if (gameplay) GameplayStepCount++;
                var step = new SimStep(StepCount, GameplayStepCount, StepSeconds, gameplay);
                foreach (var onStep in _steps.ToArray()) onStep(step);
            }
        }

        private sealed class Handle : IDisposable
        {
            private Action _dispose;
            public Handle(Action dispose) { _dispose = dispose; }
            public void Dispose()
            {
                _dispose?.Invoke();
                _dispose = null;
            }
        }
    }

    internal sealed class FakeSession : ISessionScene
    {
        public readonly List<ISessionParticipant> Joined = new List<ISessionParticipant>();
        public SessionState State => SessionState.Init;
        public event Action<SessionState> StateChanged { add { } remove { } }
        public void Join(ISessionParticipant participant) => Joined.Add(participant);
        public UniTask EndAsync() => UniTask.CompletedTask;
    }
}
