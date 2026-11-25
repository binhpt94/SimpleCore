using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Game.Core;
using Game.Enemy;
using Game.Hero;
using Game.Run;

namespace Game.Castle.Tests
{
    internal sealed class FakeSessionScene : ISessionScene
    {
        public readonly List<ISessionParticipant> Joined = new List<ISessionParticipant>();
        public SessionState State => SessionState.Init;
        public event Action<SessionState> StateChanged { add { } remove { } }
        public void Join(ISessionParticipant participant) => Joined.Add(participant);
        public UniTask EndAsync() => UniTask.CompletedTask;
    }

    internal sealed class FakeRunFlow : IRunFlow
    {
        public int LoseCalls;
        public Action OnLose;
        public RunStep Step => default;
        public event Action<RunStep> StepChanged { add { } remove { } }
        public RunTeam Team => default;
        public event Action<RunTeam> RunStarted;
        public event Action BattleStarted;
        public event Action BattleStopped;
        public event Action BattleCleared;
        public void StartBattle() => BattleStarted?.Invoke();
        public void StopBattle() => BattleStopped?.Invoke();
        public void ClearBattle() => BattleCleared?.Invoke();
        public bool IsPaused { get; set; }
        public void Play() { }
        public void EnterBattle(RunTeam team) { }
        public void Pause() { }
        public void Resume() { }
        public void Abandon() { }
        public void Replay() { }
        public void GoHome() { }
        public void Lose() { LoseCalls++; OnLose?.Invoke(); }
        public void CountClearedWave(int wave) { }
        public void StartRun() => RunStarted?.Invoke(new RunTeam(new[] { "a", "b", "c" }));
    }

    internal sealed class FakeEnemyField : IEnemyField
    {
        public readonly List<IEnemyFoes> Foes = new List<IEnemyFoes>();
        public readonly List<EnemyTarget> Alive = new List<EnemyTarget>();
        public readonly List<(int handle, float amount)> Damaged = new List<(int, float)>();
        public readonly List<float> Slows = new List<float>();
        public int AliveCount => Alive.Count;
        public event Action<EnemyDefeat> Defeated;
        public void Defeat(int handle) => Defeated?.Invoke(new EnemyDefeat(handle, default(EnemyType), default(EnemySide)));
        public void Spawn(EnemySpawn spawn) { }
        public void GetAlive(List<EnemyTarget> into) { into.Clear(); into.AddRange(Alive); }
        public bool Damage(int handle, float amount) { Damaged.Add((handle, amount)); return false; }
        public void SlowAll(float seconds) => Slows.Add(seconds);
        public void AddFoes(IEnemyFoes foes) => Foes.Add(foes);
        public void AddEnemy(int handle, float x, float halfWidth = 0.4f)
            => Alive.Add(new EnemyTarget(handle, default(EnemySide), x, halfWidth, handle));
    }

    internal sealed class FakeView : ICastleView
    {
        public readonly List<string> Calls = new List<string>();
        public bool Visible;
        public float FlashSeconds, CollapseSeconds, ShotSeconds, ThunderSeconds;
        public event Action<float> Ticked;
        public void Tick(float seconds) => Ticked?.Invoke(seconds);
        public void Place(float halfWidth, float height) => Calls.Add($"place {halfWidth} {height}");
        public void SetVisible(bool visible) => Visible = visible;
        public void ResetBody() => Calls.Add("reset");
        public void FlashHit(float seconds) { FlashSeconds = seconds; Calls.Add("flash"); }
        public void PopLevelUp(float scale, float seconds) => Calls.Add($"pop {scale} {seconds}");
        public void Collapse(float seconds) { CollapseSeconds = seconds; Calls.Add("collapse"); }
        public void ShowShot(int targetHandle, float fromX, float toX, float seconds) { ShotSeconds = seconds; Calls.Add($"shot {targetHandle}"); }
        public void DropShotsAt(int targetHandle) => Calls.Add($"drop {targetHandle}");
        public void ShowThunder(float x, float seconds) { ThunderSeconds = seconds; Calls.Add($"thunder {x}"); }
        public void Advance(float seconds) => Calls.Add("advance");
        public void ClearEffects() => Calls.Add("clear");
    }

    internal sealed class FakeSpawner : ICastleViewSpawner
    {
        public readonly FakeView View = new FakeView();
        public bool Despawned;
        public ICastleView Spawn() => View;
        public void Despawn(ICastleView view) => Despawned = true;
    }

    internal sealed class FakeHeroSquad : IHeroSquad
    {
        public readonly List<float> Heals = new List<float>();
        public void HealAll(float fractionOfMaxHealth) => Heals.Add(fractionOfMaxHealth);
        public void ApplyLevel(int slot, int level) { }
    }
}
