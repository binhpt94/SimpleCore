using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Game.Core;
using Game.Enemy;
using Game.Run;
using UnityEngine;

namespace Game.Hero.Tests
{
    /// <summary>Stand-ins for the session, the run, the enemy field and the Unity side of the hero feature.</summary>
    internal static class HeroFakes
    {
        public const string ConfigPath = "Assets/Features/Hero/FeatureAssets/Private/HeroConfig.asset";

        public static HeroConfig LoadConfig()
        {
            var config = UnityEditor.AssetDatabase.LoadAssetAtPath<HeroConfig>(ConfigPath);
            if (config == null) throw new InvalidOperationException($"Missing {ConfigPath}");
            return config;
        }

        public sealed class Session : ISessionScene
        {
            public readonly List<ISessionParticipant> Participants = new List<ISessionParticipant>();

            public SessionState State => SessionState.Init;

            public event Action<SessionState> StateChanged { add { } remove { } }

            public void Join(ISessionParticipant participant) => Participants.Add(participant);

            public UniTask EndAsync() => UniTask.CompletedTask;
        }

        public sealed class RunFlow : IRunFlow
        {
            public RunStep Step => IsPaused ? RunStep.Paused : RunStep.Playing;
            public event Action<RunStep> StepChanged { add { } remove { } }
            public RunTeam Team { get; private set; }
            public event Action<RunTeam> RunStarted;
            public event Action BattleStarted { add { } remove { } }
            public event Action BattleStopped;
            public event Action BattleCleared;
            public bool IsPaused { get; set; }

            public void StartRun(params string[] heroCodes)
            {
                Team = new RunTeam(heroCodes);
                RunStarted?.Invoke(Team);
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
            public void Lose() { }
            public void CountClearedWave(int wave) { }
        }

        /// <summary>Enemies standing still where the test puts them; Damage takes health and removes them at 0.</summary>
        public sealed class EnemyField : IEnemyField
        {
            private sealed class Enemy
            {
                public int Handle;
                public EnemySide Side;
                public float X;
                public float HalfWidth;
                public float Health;
                public int SpawnOrder;
            }

            private readonly List<Enemy> _enemies = new List<Enemy>();
            private int _nextHandle = 1;

            public readonly List<IEnemyFoes> Foes = new List<IEnemyFoes>();

            /// <summary>Health taken from each enemy, by handle, over the test.</summary>
            public readonly Dictionary<int, float> DamageTaken = new Dictionary<int, float>();

            public int Add(float x, float health = 1000f, float halfWidth = 0.4f)
            {
                var handle = _nextHandle++;
                _enemies.Add(new Enemy
                {
                    Handle = handle,
                    Side = x < 0f ? EnemySide.Left : EnemySide.Right,
                    X = x,
                    HalfWidth = halfWidth,
                    Health = health,
                    SpawnOrder = handle,
                });
                return handle;
            }

            public void Move(int handle, float x)
            {
                foreach (var enemy in _enemies)
                    if (enemy.Handle == handle) enemy.X = x;
            }

            public void Remove(int handle) => _enemies.RemoveAll(e => e.Handle == handle);

            public float Taken(int handle) => DamageTaken.TryGetValue(handle, out var taken) ? taken : 0f;

            public void Spawn(EnemySpawn spawn) { }

            public int AliveCount => _enemies.Count;

            public void GetAlive(List<EnemyTarget> into)
            {
                into.Clear();
                foreach (var e in _enemies) into.Add(new EnemyTarget(e.Handle, e.Side, e.X, e.HalfWidth, e.SpawnOrder));
            }

            public bool Damage(int handle, float amount)
            {
                var enemy = _enemies.Find(e => e.Handle == handle);
                if (enemy == null) return false;
                DamageTaken[handle] = Taken(handle) + amount;
                enemy.Health -= amount;
                if (enemy.Health > 0f) return false;
                _enemies.Remove(enemy);
                return true;
            }

            public void SlowAll(float seconds) { }

            public event Action<EnemyDefeat> Defeated { add { } remove { } }

            public void AddFoes(IEnemyFoes foes) => Foes.Add(foes);
        }

        public sealed class Display : IHeroDisplay
        {
            public string HeroCode;
            public float X;
            public int Facing;
            public bool Moving;
            public bool Removed;
            public int Strikes, Hurts, Falls, Respawns, LevelUps;
            public float Animated;

            public void Setup(string heroCode, Sprite sprite, Color placeholderColor, float width, float height) => HeroCode = heroCode;

            public void SetPose(float x, int facing, bool moving)
            {
                X = x;
                Facing = facing;
                Moving = moving;
            }

            public void Animate(float deltaSeconds) => Animated += deltaSeconds;
            public void PlayStrike(HeroStrike strike) => Strikes++;
            public void PlayHurt() => Hurts++;
            public void PlayFall(float seconds) => Falls++;
            public void PlayRespawn(float seconds) => Respawns++;
            public void PlayLevelUp() => LevelUps++;
            public void Remove() => Removed = true;
        }

        public sealed class DisplayFactory : IHeroDisplayFactory
        {
            public readonly List<Display> Made = new List<Display>();

            public IHeroDisplay Create()
            {
                var display = new Display();
                Made.Add(display);
                return display;
            }

            public void RemoveAll() { }
        }

        /// <summary>A clock that never ticks by itself; tests call the participant's Step.</summary>
        public sealed class StillClock : IFrameClock
        {
            public UniTask NextFrame(CancellationToken cancellation) =>
                UniTask.Never(cancellation);

            public float DeltaSeconds => 0f;
        }
    }
}
