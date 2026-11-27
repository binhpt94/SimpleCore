using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Game.Combat;
using Game.Core;
using Game.Run;
using Game.SimClock;
using Game.Wallet;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using SessionState = Game.Core.SessionState;

namespace Game.Enemy.Tests
{
    public sealed class EnemyInstallerTests
    {
        private const string InstallerPath = "Assets/Features/Enemy/FeatureAssets/Public/EnemyInstaller.asset";

        [Test]
        public void Installing_the_real_installer_publishes_the_enemy_field_and_joins_the_session()
        {
            var installer = AssetDatabase.LoadAssetAtPath<EnemyInstaller>(InstallerPath);
            Assert.That(installer, Is.Not.Null, $"Missing {InstallerPath}");

            var registry = new FakeRegistry();
            var session = new FakeSession();
            registry.Register<ISessionScene>(session);
            registry.Register<IRunFlow>(new FakeRunFlow());
            registry.Register<IWalletIncome>(new FakeIncome());
            registry.Register<ISimClock>(new FakeClock());
            var combat = new FakeCombat();
            registry.Register<ICombat>(combat);
            registry.Register<IArt>(new EnemyFakeArt());

            installer.Install(registry);

            Assert.That(registry.TryResolve<IEnemyField>(out var field), Is.True);
            Assert.That(field.AliveCount, Is.Zero);
            Assert.That(session.Joined.Count, Is.EqualTo(1));

            Assert.That(registry.TryResolve<IEnemyCombat>(out var enemyCombat), Is.True);
            Assert.That(combat.Receivers.Count, Is.EqualTo(1), "the enemies take hits through combat");
            Assert.That(combat.Actors.Count, Is.EqualTo(1), "the enemies walk and hit in the combat tick");
            Assert.That(enemyCombat.CombatReceiver, Is.EqualTo(41), "the number combat gave");
        }

        [Test]
        public void Installing_before_combat_fails_naming_the_missing_service()
        {
            var installer = AssetDatabase.LoadAssetAtPath<EnemyInstaller>(InstallerPath);
            Assert.That(installer, Is.Not.Null, $"Missing {InstallerPath}");

            var registry = new FakeRegistry();
            registry.Register<ISessionScene>(new FakeSession());
            registry.Register<IRunFlow>(new FakeRunFlow());
            registry.Register<IWalletIncome>(new FakeIncome());
            registry.Register<ISimClock>(new FakeClock());

            var error = Assert.Throws<InvalidOperationException>(() => installer.Install(registry));
            StringAssert.Contains(nameof(ICombat), error.Message);
        }

        [Test]
        public void Installing_before_the_shared_clock_fails_naming_the_missing_service()
        {
            var installer = AssetDatabase.LoadAssetAtPath<EnemyInstaller>(InstallerPath);
            Assert.That(installer, Is.Not.Null, $"Missing {InstallerPath}");

            var registry = new FakeRegistry();
            registry.Register<ISessionScene>(new FakeSession());
            registry.Register<IRunFlow>(new FakeRunFlow());
            registry.Register<IWalletIncome>(new FakeIncome());

            var error = Assert.Throws<InvalidOperationException>(() => installer.Install(registry));
            StringAssert.Contains(nameof(ISimClock), error.Message);
        }

        [Test]
        public void Installing_before_the_wallet_fails_naming_the_missing_service()
        {
            var installer = AssetDatabase.LoadAssetAtPath<EnemyInstaller>(InstallerPath);
            Assert.That(installer, Is.Not.Null, $"Missing {InstallerPath}");

            var registry = new FakeRegistry();
            registry.Register<ISessionScene>(new FakeSession());
            registry.Register<IRunFlow>(new FakeRunFlow());

            var error = Assert.Throws<InvalidOperationException>(() => installer.Install(registry));
            StringAssert.Contains(nameof(IWalletIncome), error.Message);
        }

        [Test]
        public void Installing_without_a_config_fails_naming_the_config()
        {
            var installer = ScriptableObject.CreateInstance<EnemyInstaller>();
            try
            {
                var registry = new FakeRegistry();
                registry.Register<ISessionScene>(new FakeSession());
                registry.Register<IRunFlow>(new FakeRunFlow());
                registry.Register<IWalletIncome>(new FakeIncome());

                var error = Assert.Throws<InvalidOperationException>(() => installer.Install(registry));
                StringAssert.Contains(nameof(EnemyConfig), error.Message);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(installer);
            }
        }

        [Test]
        public void Installing_without_a_stage_prefab_fails_naming_the_prefab()
        {
            var real = AssetDatabase.LoadAssetAtPath<EnemyInstaller>(InstallerPath);
            var config = new SerializedObject(real).FindProperty("_config").objectReferenceValue;
            var installer = ScriptableObject.CreateInstance<EnemyInstaller>();
            try
            {
                var so = new SerializedObject(installer);
                so.FindProperty("_config").objectReferenceValue = config;
                so.ApplyModifiedPropertiesWithoutUndo();

                var registry = new FakeRegistry();
                registry.Register<ISessionScene>(new FakeSession());
                registry.Register<IRunFlow>(new FakeRunFlow());
                registry.Register<IWalletIncome>(new FakeIncome());

                var error = Assert.Throws<InvalidOperationException>(() => installer.Install(registry));
                StringAssert.Contains("stage prefab", error.Message);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(installer);
            }
        }

        [Test]
        public void The_real_installer_has_a_stage_prefab_that_draws_enemies()
        {
            var installer = AssetDatabase.LoadAssetAtPath<EnemyInstaller>(InstallerPath);
            var prefab = new SerializedObject(installer).FindProperty("_stagePrefab").objectReferenceValue as GameObject;
            Assert.That(prefab, Is.Not.Null, "EnemyInstaller.asset has no stage prefab assigned");
            Assert.That(prefab.GetComponent<IEnemyStage>(), Is.Not.Null);
        }

        [Test]
        public void The_real_config_has_the_base_numbers_of_every_enemy_type()
        {
            var installer = AssetDatabase.LoadAssetAtPath<EnemyInstaller>(InstallerPath);
            var config = new SerializedObject(installer).FindProperty("_config").objectReferenceValue as EnemyConfig;
            Assert.That(config, Is.Not.Null, "EnemyInstaller.asset has no EnemyConfig assigned");
            Assert.That(config.MissingKinds(), Is.Empty);

            var soldier = config.Kind(EnemyType.Soldier);
            Assert.That(soldier.Health, Is.EqualTo(40f));
            Assert.That(soldier.Coins, Is.EqualTo(5));
            Assert.That(config.Kind(EnemyType.Boss).Health, Is.EqualTo(400f));
            Assert.That(config.SpawnDistance, Is.EqualTo(17f));
        }

        // width, height, health, damage, seconds per hit, range, walk speed, coins
        // (live: enemy-type-values, enemy-stat-values, reward-values)
        [TestCase(EnemyType.Soldier, 1f, 1.2f, 40f, 5f, 1.0f, 1.0f, 1.5f, 5)]
        [TestCase(EnemyType.Wolf, 1.2f, 0.8f, 20f, 3f, 0.8f, 1.0f, 3.0f, 4)]
        [TestCase(EnemyType.Giant, 2f, 2.5f, 150f, 15f, 1.5f, 1.2f, 0.8f, 15)]
        [TestCase(EnemyType.Archer, 1f, 1.2f, 25f, 6f, 1.2f, 5f, 1.5f, 6)]
        [TestCase(EnemyType.Boss, 3f, 3.5f, 400f, 40f, 2.0f, 1.5f, 0.6f, 100)]
        public void The_real_config_matches_the_design_table_of_each_enemy_type(EnemyType type, float width,
            float height, float health, float damage, float secondsPerHit, float range, float moveSpeed, int coins)
        {
            var installer = AssetDatabase.LoadAssetAtPath<EnemyInstaller>(InstallerPath);
            var config = new SerializedObject(installer).FindProperty("_config").objectReferenceValue as EnemyConfig;
            Assert.That(config, Is.Not.Null, "EnemyInstaller.asset has no EnemyConfig assigned");

            var kind = config.Kind(type);
            Assert.That(kind.Width, Is.EqualTo(width).Within(1e-4f), "width");
            Assert.That(kind.Height, Is.EqualTo(height).Within(1e-4f), "height");
            Assert.That(kind.Health, Is.EqualTo(health).Within(1e-4f), "health");
            Assert.That(kind.Damage, Is.EqualTo(damage).Within(1e-4f), "damage");
            Assert.That(kind.SecondsPerHit, Is.EqualTo(secondsPerHit).Within(1e-4f), "seconds per hit");
            Assert.That(kind.Range, Is.EqualTo(range).Within(1e-4f), "range");
            Assert.That(kind.MoveSpeed, Is.EqualTo(moveSpeed).Within(1e-4f), "walk speed");
            Assert.That(kind.Coins, Is.EqualTo(coins), "coins");
        }

        [TestCase("_bobHeight", 0.1f)]
        [TestCase("_bobPeriod", 0.4f)]
        [TestCase("_lungeDistance", 0.2f)]
        [TestCase("_lungeSeconds", 0.15f)]
        [TestCase("_flashSeconds", 0.1f)]
        [TestCase("_defeatSeconds", 0.3f)]
        [TestCase("_shotSeconds", 0.15f)]
        [TestCase("_coinRise", 1f)]
        [TestCase("_coinSeconds", 0.6f)]
        [TestCase("_entranceScale", 1.2f)]
        [TestCase("_entranceSeconds", 0.5f)]
        public void The_real_stage_plays_the_unit_motions_with_the_design_numbers(string field, float expected)
        {
            // Also: an enemy archer's shot reaches its target in 0.15s (live: enemy-type-values); the "+coins" text
            // rises 1u in 0.6s then is gone (live: reward-values).
            // bob 0.1u each 0.4s while walking, lunge 0.2u in 0.15s on a hit, flash 0.1s when hit,
            // shrink to nothing in 0.3s when defeated (live: enemy-move-values, enemy-stat-values).
            var installer = AssetDatabase.LoadAssetAtPath<EnemyInstaller>(InstallerPath);
            var prefab = new SerializedObject(installer).FindProperty("_stagePrefab").objectReferenceValue as GameObject;
            Assert.That(prefab, Is.Not.Null, "EnemyInstaller.asset has no stage prefab assigned");
            var stage = prefab.GetComponent<IEnemyStage>() as Component;
            Assert.That(stage, Is.Not.Null, "stage prefab has no IEnemyStage component");

            var property = new SerializedObject(stage).FindProperty(field);
            Assert.That(property, Is.Not.Null, $"stage has no field {field}");
            Assert.That(property.floatValue, Is.EqualTo(expected).Within(1e-4f));
        }

        [TestCase(EnemyType.Soldier, false)]
        [TestCase(EnemyType.Wolf, false)]
        [TestCase(EnemyType.Giant, false)]
        [TestCase(EnemyType.Archer, true)]
        [TestCase(EnemyType.Boss, false)]
        public void Only_the_enemy_archer_hits_from_afar_in_the_real_config(EnemyType type, bool ranged)
        {
            var installer = AssetDatabase.LoadAssetAtPath<EnemyInstaller>(InstallerPath);
            var config = new SerializedObject(installer).FindProperty("_config").objectReferenceValue as EnemyConfig;
            Assert.That(config, Is.Not.Null, "EnemyInstaller.asset has no EnemyConfig assigned");

            Assert.That(config.Kind(type).Ranged, Is.EqualTo(ranged));
        }

        // PROTOTYPE: all five types are in the game (live: enemy-type-values, Thông số theo giai đoạn).
        [TestCase(EnemyType.Soldier)]
        [TestCase(EnemyType.Wolf)]
        [TestCase(EnemyType.Giant)]
        [TestCase(EnemyType.Archer)]
        [TestCase(EnemyType.Boss)]
        public void The_real_config_has_every_enemy_type_in_the_game_at_PROTOTYPE(EnemyType type)
        {
            var installer = AssetDatabase.LoadAssetAtPath<EnemyInstaller>(InstallerPath);
            var config = new SerializedObject(installer).FindProperty("_config").objectReferenceValue as EnemyConfig;
            Assert.That(config, Is.Not.Null, "EnemyInstaller.asset has no EnemyConfig assigned");

            Assert.That(config.Kind(type).Available, Is.True);
        }

        // Boss coming on screen: grows 1.2 times and back in 0.5s when its centre reaches x=±16u (live: enemy-type-values).
        [TestCase(EnemyType.Soldier, false)]
        [TestCase(EnemyType.Wolf, false)]
        [TestCase(EnemyType.Giant, false)]
        [TestCase(EnemyType.Archer, false)]
        [TestCase(EnemyType.Boss, true)]
        public void Only_the_boss_has_an_entrance_at_16u_in_the_real_config(EnemyType type, bool entrance)
        {
            var installer = AssetDatabase.LoadAssetAtPath<EnemyInstaller>(InstallerPath);
            var config = new SerializedObject(installer).FindProperty("_config").objectReferenceValue as EnemyConfig;
            Assert.That(config, Is.Not.Null, "EnemyInstaller.asset has no EnemyConfig assigned");

            Assert.That(config.Kind(type).Entrance, Is.EqualTo(entrance));
            Assert.That(config.EntranceDistance, Is.EqualTo(16f));
        }

        [Test]
        public void The_real_stage_tints_slowed_enemies_with_a_visible_slow_mark()
        {
            // PROTOTYPE (enemy-move-values): a slowed enemy shows a mark all the time it is slowed.
            var installer = AssetDatabase.LoadAssetAtPath<EnemyInstaller>(InstallerPath);
            var prefab = new SerializedObject(installer).FindProperty("_stagePrefab").objectReferenceValue as GameObject;
            Assert.That(prefab, Is.Not.Null, "EnemyInstaller.asset has no stage prefab assigned");
            var stage = prefab.GetComponent<IEnemyStage>() as Component;

            var tint = new SerializedObject(stage).FindProperty("_slowTint").colorValue;
            Assert.That(tint.a, Is.EqualTo(1f).Within(1e-4f), "the tint does not fade the enemy");
            Assert.That(tint.r < 0.95f || tint.g < 0.95f || tint.b < 0.95f, Is.True, "the tint is not plain white");
        }

        [Test]
        public void The_real_config_slows_enemies_by_half_under_Frost()
        {
            var installer = AssetDatabase.LoadAssetAtPath<EnemyInstaller>(InstallerPath);
            var config = new SerializedObject(installer).FindProperty("_config").objectReferenceValue as EnemyConfig;

            Assert.That(config.SlowMoveFactor, Is.EqualTo(0.5f));
            Assert.That(config.SlowHitFactor, Is.EqualTo(2f));
        }

        private sealed class FakeRegistry : IServiceRegistry
        {
            private readonly Dictionary<Type, object> _services = new Dictionary<Type, object>();

            public void Register<T>(T service) where T : class => _services.Add(typeof(T), service);

            public T Resolve<T>() where T : class
            {
                if (TryResolve<T>(out var service)) return service;
                throw new InvalidOperationException($"{typeof(T).Name} is not installed.");
            }

            public bool TryResolve<T>(out T service) where T : class
            {
                var found = _services.TryGetValue(typeof(T), out var value);
                service = found ? (T)value : null;
                return found;
            }
        }

        private sealed class FakeSession : ISessionScene
        {
            public readonly List<ISessionParticipant> Joined = new List<ISessionParticipant>();
            public SessionState State => SessionState.Init;
#pragma warning disable 67
            public event Action<SessionState> StateChanged;
#pragma warning restore 67
            public void Join(ISessionParticipant participant) => Joined.Add(participant);
            public UniTask EndAsync() => UniTask.CompletedTask;
        }

        private sealed class FakeIncome : IWalletIncome
        {
            public readonly List<int> Added = new List<int>();
            public void Add(int amount) => Added.Add(amount);
        }

        private sealed class FakeCombat : ICombat
        {
            public readonly List<ICombatReceiver> Receivers = new List<ICombatReceiver>();
            public readonly List<ICombatActor> Actors = new List<ICombatActor>();
#pragma warning disable 67
            public event Action TickDone;
#pragma warning restore 67

            public int AddReceiver(ICombatReceiver receiver)
            {
                Receivers.Add(receiver);
                return 40 + Receivers.Count;
            }

            public void AddActor(ICombatActor actor) => Actors.Add(actor);
            public void Hit(in CombatHit hit) { }
        }

        private sealed class FakeClock : ISimClock
        {
            public SimMode Mode => SimMode.FixedTick;
            public float StepSeconds => 0.025f;
            public long StepCount => 0;
            public long GameplayStepCount => 0;
            public double GameplaySeconds => 0;
            public IDisposable OnStep(string owner, Action<SimStep> onStep) => new Nothing();
            public void Enqueue(string owner, Action apply) { }

            private sealed class Nothing : IDisposable
            {
                public void Dispose() { }
            }
        }

        private sealed class FakeRunFlow : IRunFlow
        {
            public RunStep Step => RunStep.Home;
            public RunTeam Team => default;
#pragma warning disable 67
            public event Action<RunStep> StepChanged;
            public event Action<RunTeam> RunStarted;
            public event Action BattleStarted;
            public event Action BattleStopped;
            public event Action BattleCleared;
#pragma warning restore 67
            public void Play() { }
            public void EnterBattle(RunTeam team) { }
            public void Replay() { }
            public void GoHome() { }
            public void Lose() { }
            public void CountClearedWave(int wave) { }
        }
    }
}
