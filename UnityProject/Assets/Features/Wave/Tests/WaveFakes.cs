using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Game.Core;
using Game.Enemy;
using Game.Run;
using Game.Wallet;

namespace Game.Wave.Tests
{
    internal sealed class FakeRun : IRunFlow
    {
        public readonly List<string> Log;
        public FakeRun(List<string> log) { Log = log; }

        public RunStep Step => default;
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
        public void Pause() { }
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

    internal sealed class FakeSession : ISessionScene
    {
        public readonly List<ISessionParticipant> Joined = new List<ISessionParticipant>();
        public SessionState State => SessionState.Init;
        public event Action<SessionState> StateChanged { add { } remove { } }
        public void Join(ISessionParticipant participant) => Joined.Add(participant);
        public UniTask EndAsync() => UniTask.CompletedTask;
    }
}
