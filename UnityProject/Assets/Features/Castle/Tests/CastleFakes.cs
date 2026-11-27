using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Game.Combat;
using Game.Core;
using Game.Enemy;
using Game.Hero;
using Game.Run;
using Game.SimClock;

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
        public RunStep Step { get; set; }
        public event Action<RunStep> StepChanged { add { } remove { } }
        public RunTeam Team => default;
        public event Action<RunTeam> RunStarted;
        public event Action BattleStarted;
        public event Action BattleStopped;
        public event Action BattleCleared;
        public void StartBattle() => BattleStarted?.Invoke();
        public void StopBattle() => BattleStopped?.Invoke();
        public void ClearBattle() => BattleCleared?.Invoke();
        public void Play() { }
        public void EnterBattle(RunTeam team) { }
        public void Replay() { }
        public void GoHome() { }
        public void Lose() { LoseCalls++; OnLose?.Invoke(); }
        public void CountClearedWave(int wave) { }
        public void StartRun() => RunStarted?.Invoke(new RunTeam(new[] { "a", "b", "c" }));
    }

    /// <summary>The enemies; also stands in for the castle's hits on them, recording each in <see cref="Damaged"/>.</summary>
    internal sealed class FakeEnemyField : IEnemyField, ICastleEnemyHits
    {
        void ICastleEnemyHits.Hit(int enemyHandle, float damage) => Damaged.Add((enemyHandle, damage));
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
        public float FlashSeconds, CollapseSeconds, ShotSeconds, ThunderSeconds, ShotFromX, ShotToX, ShakeSeconds;
        public bool Damaged;
        public event Action<float> Ticked;
        private UnityEngine.GameObject _object;

        /// <summary>A real Unity object so the session can count the view; tests destroy it in TearDown.</summary>
        public UnityEngine.Object ViewObject => _object ? _object : (_object = new UnityEngine.GameObject("FakeCastleView"));
        public void DestroyObject() { if (_object) UnityEngine.Object.DestroyImmediate(_object); }
        public void ShakeHit(float seconds) { ShakeSeconds = seconds; Calls.Add("shake"); }
        public void ShowDamaged(bool damaged) => Damaged = damaged;
        public void Tick(float seconds) => Ticked?.Invoke(seconds);
        public Game.Core.IArt Art;
        public void UseArt(Game.Core.IArt art) => Art = art;
        public void Place(float halfWidth, float height) => Calls.Add($"place {halfWidth} {height}");
        public void SetVisible(bool visible) => Visible = visible;
        public void ResetBody() => Calls.Add("reset");
        public void FlashHit(float seconds) { FlashSeconds = seconds; Calls.Add("flash"); }
        public void PopLevelUp(float scale, float seconds) => Calls.Add($"pop {scale} {seconds}");
        public void Collapse(float seconds) { CollapseSeconds = seconds; Calls.Add("collapse"); }
        public void ShowShot(int targetHandle, float fromX, float toX, float seconds)
        {
            ShotSeconds = seconds;
            ShotFromX = fromX;
            ShotToX = toX;
            Calls.Add($"shot {targetHandle}");
        }
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

    /// <summary>The shared clock, stepped by hand: 0.025 s steps, gameplay unless told otherwise.</summary>
    internal sealed class FakeSimClock : ISimClock
    {
        public const float Step = 0.025f;
        public readonly List<string> Owners = new List<string>();
        private readonly List<Action<SimStep>> _subscribers = new List<Action<SimStep>>();
        private long _number, _gameplayNumber;

        public SimMode Mode => SimMode.FixedTick;
        public float StepSeconds => Step;
        public long StepCount => _number;
        public long GameplayStepCount => _gameplayNumber;
        public double GameplaySeconds => _gameplayNumber * (double)Step;
        public int SubscriberCount => _subscribers.Count;

        public IDisposable OnStep(string owner, Action<SimStep> onStep)
        {
            Owners.Add(owner);
            _subscribers.Add(onStep);
            return new Unsubscribe(() => _subscribers.Remove(onStep));
        }

        public void Enqueue(string owner, Action apply) => apply();

        /// <summary>Runs <paramref name="count"/> steps.</summary>
        public void Steps(int count, bool gameplay = true)
        {
            for (var i = 0; i < count; i++)
            {
                _number++;
                if (gameplay) _gameplayNumber++;
                var step = new SimStep(_number, _gameplayNumber, Step, gameplay);
                foreach (var subscriber in _subscribers.ToArray()) subscriber(step);
            }
        }

        /// <summary>Runs as many steps as fit in <paramref name="seconds"/> (rounded to the nearest step).</summary>
        public void Run(float seconds, bool gameplay = true) => Steps((int)Math.Round(seconds / Step), gameplay);

        private sealed class Unsubscribe : IDisposable
        {
            private Action _undo;
            public Unsubscribe(Action undo) => _undo = undo;
            public void Dispose() { _undo?.Invoke(); _undo = null; }
        }
    }

    internal sealed class FakeCombat : ICombat
    {
        public readonly List<ICombatReceiver> Receivers = new List<ICombatReceiver>();
        public readonly List<CombatHit> Hits = new List<CombatHit>();
        public event Action TickDone { add { } remove { } }

        /// <summary>Receiver numbers start at 10 so a test can tell them from keys.</summary>
        public int AddReceiver(ICombatReceiver receiver)
        {
            Receivers.Add(receiver);
            return 10 + Receivers.Count - 1;
        }

        public void AddActor(ICombatActor actor) { }
        public void Hit(in CombatHit hit) => Hits.Add(hit);
    }

    internal sealed class FakeEnemyCombat : IEnemyCombat
    {
        public IEnemyFoes Foes;
        public int FoesReceiver = -1;
        public int CombatReceiver => 7;
        public void AddCombatFoes(IEnemyFoes foes, int combatReceiver) { Foes = foes; FoesReceiver = combatReceiver; }
    }

    internal sealed class FakeHeroSpawnPoints : IHeroSpawnPoints
    {
        public IReadOnlyList<float> SpawnX;
        public void SetSpawnX(IReadOnlyList<float> xBySlot) => SpawnX = xBySlot;
    }

    internal sealed class FakeHeroSquad : IHeroSquad
    {
        public readonly List<float> Heals = new List<float>();
        public void HealAll(float fractionOfMaxHealth) => Heals.Add(fractionOfMaxHealth);
        public void ApplyLevel(int slot, int level) { }
    }

    /// <summary>
    /// Core's art with a hand-made catalog: a key given a sprite answers Simple with it, any other key GreyBox.
    /// </summary>
    internal sealed class FakeArt : IArt
    {
        public readonly Dictionary<string, UnityEngine.Sprite> Simple = new Dictionary<string, UnityEngine.Sprite>();
        public readonly List<string> Asked = new List<string>();

        public UnityEngine.Sprite Sprite(string key, GreyBox.Look look)
        {
            Asked.Add(key);
            return Simple.TryGetValue(key, out var sprite) ? sprite : GreyBox.Sprite(look);
        }

        public UnityEngine.GameObject Model(string key, UnityEngine.Vector3 size) => GreyBox.Model(size);

        public ArtTier TierOf(string key) => Simple.ContainsKey(key) ? ArtTier.Simple : ArtTier.GreyBox;
    }
}
