using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Game.Castle;
using Game.Core;
using Game.Hero;
using Game.Run;
using Game.Wallet;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using SessionState = Game.Core.SessionState; // UnityEditor also has a SessionState

namespace Game.Upgrade.Tests
{
    public class UpgradeInstallerTests
    {
        private const string InstallerPath = "Assets/Features/Upgrade/FeatureAssets/Public/UpgradeInstaller.asset";

        [Test]
        public void The_real_installer_publishes_the_shop_and_joins_the_session()
        {
            var installer = AssetDatabase.LoadAssetAtPath<UpgradeInstaller>(InstallerPath);
            Assert.IsNotNull(installer, $"Missing {InstallerPath}");
            var registry = new FakeRegistry();
            var session = new FakeSession();
            registry.Add<ISessionScene>(session);
            registry.Add<IRunFlow>(new NullRun());
            var wallet = new NullWallet();
            registry.Add<IWalletService>(wallet);
            registry.Add<IWalletBalance>(wallet);
            registry.Add<IHeroSquad>(new NullHeroes());
            registry.Add<ICastleUpgrades>(new NullCastle());

            installer.Install(registry);

            Assert.IsTrue(registry.TryResolve<IUpgradeShop>(out var shop));
            Assert.AreEqual(0, shop.Count);
            Assert.AreEqual(1, session.Joined.Count);
        }

        [Test]
        public void An_installer_without_config_fails_and_names_the_config()
        {
            var installer = ScriptableObject.CreateInstance<UpgradeInstaller>();
            try
            {
                var error = Assert.Throws<InvalidOperationException>(() => installer.Install(new FakeRegistry()));
                StringAssert.Contains("UpgradeConfig", error.Message);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(installer);
            }
        }

        private sealed class FakeRegistry : IServiceRegistry
        {
            private readonly Dictionary<Type, object> _services = new Dictionary<Type, object>();

            public void Add<T>(T service) where T : class => _services[typeof(T)] = service;

            public void Register<T>(T service) where T : class => _services.Add(typeof(T), service);

            public T Resolve<T>() where T : class =>
                TryResolve<T>(out var service) ? service : throw new InvalidOperationException(typeof(T).Name);

            public bool TryResolve<T>(out T service) where T : class
            {
                service = _services.TryGetValue(typeof(T), out var found) ? (T)found : null;
                return service != null;
            }
        }

        private sealed class FakeSession : ISessionScene
        {
            public readonly List<ISessionParticipant> Joined = new List<ISessionParticipant>();

            public SessionState State => SessionState.Init;
            public event Action<SessionState> StateChanged { add { } remove { } }
            public void Join(ISessionParticipant participant) => Joined.Add(participant);
            public UniTask EndAsync() => UniTask.CompletedTask;
        }

        private sealed class NullRun : IRunFlow
        {
            public RunStep Step => RunStep.Home;
            public RunTeam Team => default;
            public bool IsPaused => false;
            public event Action<RunStep> StepChanged { add { } remove { } }
            public event Action<RunTeam> RunStarted { add { } remove { } }
            public event Action BattleStarted { add { } remove { } }
            public event Action BattleStopped { add { } remove { } }
            public event Action BattleCleared { add { } remove { } }
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

        private sealed class NullWallet : IWalletService, IWalletBalance
        {
            public int Coins => 0;
            public event Action<int> CoinsChanged { add { } remove { } }
            public SpendResult TrySpend(int amount) => SpendResult.NotEnoughCoins;
        }

        private sealed class NullHeroes : IHeroSquad
        {
            public void HealAll(float fractionOfMaxHealth) { }
            public void ApplyLevel(int slot, int level) { }
        }

        private sealed class NullCastle : ICastleUpgrades
        {
            public void ApplyLevel(CastleUpgrade item, int level) { }
        }
    }
}
