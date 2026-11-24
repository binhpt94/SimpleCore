using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Game.Core;
using Game.Run;
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

            installer.Install(registry);

            Assert.That(registry.TryResolve<IEnemyField>(out var field), Is.True);
            Assert.That(field.AliveCount, Is.Zero);
            Assert.That(session.Joined.Count, Is.EqualTo(1));
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

        private sealed class FakeRunFlow : IRunFlow
        {
            public RunStep Step => RunStep.Home;
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
