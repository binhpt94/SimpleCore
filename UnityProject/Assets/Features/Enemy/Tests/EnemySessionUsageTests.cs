using System;
using System.Collections.Generic;
using Game.Combat;
using Game.Core;
using Game.Run;
using Game.SimClock;
using Game.Wallet;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Game.Enemy.Tests
{
    /// <summary>
    /// What the enemy feature reports to the scene session's clean-up check (proposal 2026-11-25-session-usage-tracking):
    /// the stage and every enemy, shot and coin text it draws count as views, following the shared clock counts as
    /// one job, and everything is back to 0 once the session is over. Uses the real stage prefab.
    /// </summary>
    public sealed class EnemySessionUsageTests
    {
        private const string InstallerPath = "Assets/Features/Enemy/FeatureAssets/Public/EnemyInstaller.asset";

        private readonly List<Object> _opened = new List<Object>();
        private EnemyField _field;
        private FakeClock _clock;
        private EnemyParticipant _participant;
        private ISessionParticipantUsage _usage;

        [SetUp]
        public void SetUp()
        {
            var installer = AssetDatabase.LoadAssetAtPath<EnemyInstaller>(InstallerPath);
            Assert.That(installer, Is.Not.Null, $"Missing {InstallerPath}");
            var so = new SerializedObject(installer);
            var config = so.FindProperty("_config").objectReferenceValue as EnemyConfig;
            var prefab = so.FindProperty("_stagePrefab").objectReferenceValue as GameObject;
            Assert.That(config, Is.Not.Null, "EnemyInstaller.asset has no EnemyConfig assigned");
            Assert.That(prefab, Is.Not.Null, "EnemyInstaller.asset has no stage prefab assigned");

            _field = new EnemyField(config, new FakeIncome());
            _clock = new FakeClock();
            _participant = new EnemyParticipant(_field, new FakeRunFlow(), _clock, _clock, () =>
            {
                var stage = Object.Instantiate(prefab);
                _opened.Add(stage);
                return stage.GetComponent<IEnemyStage>();
            });
            _usage = _participant;
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var opened in _opened)
            {
                if (opened != null) Object.DestroyImmediate(opened);
            }

            _opened.Clear();
        }

        [Test]
        public void Enemies_are_not_expected_to_hold_a_view_right_after_the_run_step()
        {
            // Enemies come only when a wave sends them (ISessionParticipantUsage.OpensViewsOnRun).
            Assert.That(_usage.OpensViewsOnRun, Is.False);
        }

        [Test]
        public void Before_the_session_runs_the_enemies_hold_nothing()
        {
            Assert.That(_usage.HeldViews, Is.Zero);
            Assert.That(_usage.RunningJobs, Is.Zero);
        }

        [Test]
        public void While_the_session_runs_the_stage_and_each_enemy_drawn_count_as_views_and_the_clock_as_one_job()
        {
            _participant.Run();
            Assert.That(_usage.HeldViews, Is.EqualTo(1), "the stage");
            Assert.That(_usage.RunningJobs, Is.EqualTo(1), "following the shared clock");

            _field.Spawn(new EnemySpawn(EnemyType.Soldier, EnemySide.Right, 1f));
            _field.Spawn(new EnemySpawn(EnemyType.Wolf, EnemySide.Left, 1f));

            Assert.That(_usage.HeldViews, Is.EqualTo(3), "the stage and two enemies");
            Assert.That(_clock.Subscribers, Is.EqualTo(1));
        }

        [Test]
        public void A_defeated_enemy_and_its_coin_text_are_counted_until_the_session_is_over_then_everything_is_0()
        {
            _participant.Run();
            _field.Spawn(new EnemySpawn(EnemyType.Soldier, EnemySide.Right, 1f));
            var alive = new List<EnemyTarget>();
            _field.GetAlive(alive);

            _field.Damage(alive[0].Handle, 1000f);
            // Shrinking away (0.3s) and the rising "+5" are still on screen.
            Assert.That(_usage.HeldViews, Is.EqualTo(3), "the stage, the shrinking enemy, the coin text");

            _participant.DisposeAsync().GetAwaiter().GetResult();

            Assert.That(_usage.HeldViews, Is.Zero);
            Assert.That(_usage.RunningJobs, Is.Zero);
            Assert.That(_clock.Subscribers, Is.Zero, "no longer steps the enemies");
        }

        [Test]
        public void Leaving_the_battle_releases_every_enemy_drawn_and_keeps_only_the_stage()
        {
            _participant.Run();
            _field.Spawn(new EnemySpawn(EnemyType.Soldier, EnemySide.Right, 1f));
            _field.Spawn(new EnemySpawn(EnemyType.Giant, EnemySide.Left, 1f));

            _field.Clear();

            Assert.That(_usage.HeldViews, Is.EqualTo(1), "the stage stays for the next battle");
        }

        [Test]
        public void After_a_session_with_enemies_on_the_field_ends_the_enemies_hold_nothing()
        {
            _participant.Run();
            for (var i = 0; i < 5; i++) _field.Spawn(new EnemySpawn(EnemyType.Soldier, EnemySide.Right, 1f));
            _clock.StepOnce();

            _participant.DisposeAsync().GetAwaiter().GetResult();

            Assert.That(_usage.HeldViews, Is.Zero);
            Assert.That(_usage.RunningJobs, Is.Zero);
        }

        /// <summary>The shared clock and combat; combat here only tells when a tick is done.</summary>
        private sealed class FakeClock : ISimClock, ICombat
        {
            private readonly List<Action<SimStep>> _subscribers = new List<Action<SimStep>>();
            private long _steps;

            public event Action TickDone;
            public int AddReceiver(ICombatReceiver receiver) => 0;
            public void AddActor(ICombatActor actor) { }
            public void Hit(in CombatHit hit) { }

            public SimMode Mode => SimMode.FixedTick;
            public float StepSeconds => 0.025f;
            public long StepCount => _steps;
            public long GameplayStepCount => _steps;
            public double GameplaySeconds => _steps * 0.025;
            public int Subscribers => _subscribers.Count;

            public IDisposable OnStep(string owner, Action<SimStep> onStep)
            {
                _subscribers.Add(onStep);
                return new Handle(() => _subscribers.Remove(onStep));
            }

            public void Enqueue(string owner, Action apply) => throw new NotSupportedException();

            public void StepOnce()
            {
                _steps++;
                var step = new SimStep(_steps, _steps, StepSeconds, true);
                foreach (var subscriber in _subscribers.ToArray()) subscriber(step);
                TickDone?.Invoke();
            }

            private sealed class Handle : IDisposable
            {
                private Action _dispose;
                public Handle(Action dispose) => _dispose = dispose;

                public void Dispose()
                {
                    _dispose?.Invoke();
                    _dispose = null;
                }
            }
        }

        private sealed class FakeIncome : IWalletIncome
        {
            public void Add(int amount) { }
        }

        private sealed class FakeRunFlow : IRunFlow
        {
            public RunStep Step => RunStep.Playing;
            public RunTeam Team => default;
            public bool IsPaused => false;
#pragma warning disable 67
            public event Action<RunStep> StepChanged;
            public event Action<RunTeam> RunStarted;
            public event Action BattleStarted;
            public event Action BattleStopped;
            public event Action BattleCleared;
#pragma warning restore 67
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
    }
}
