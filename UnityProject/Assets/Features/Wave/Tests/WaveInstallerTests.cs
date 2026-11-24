using System;
using System.Collections.Generic;
using Game.Core;
using Game.Enemy;
using Game.Run;
using Game.Wallet;
using NUnit.Framework;
using UnityEditor;

namespace Game.Wave.Tests
{
    public class WaveInstallerTests
    {
        private const string InstallerPath = "Assets/Features/Wave/FeatureAssets/Public/WaveInstaller.asset";

        private static ServiceRegistry RegistryWithDependencies(FakeSession session)
        {
            var log = new List<string>();
            var registry = new ServiceRegistry();
            registry.Register<ISessionScene>(session);
            registry.Register<IRunFlow>(new FakeRun(log));
            registry.Register<IEnemyField>(new FakeEnemies());
            registry.Register<IWalletIncome>(new FakeIncome(log));
            return registry;
        }

        [Test]
        public void RealInstallerAsset_PublishesWaveProgress_AndJoinsTheSession()
        {
            var installer = AssetDatabase.LoadAssetAtPath<InstallerAsset>(InstallerPath);
            Assert.That(installer, Is.Not.Null, "missing " + InstallerPath);
            var session = new FakeSession();
            var registry = RegistryWithDependencies(session);

            installer.Install(registry);

            Assert.That(registry.Resolve<IWaveProgress>().CurrentWave, Is.EqualTo(1));
            Assert.That(registry.Resolve<IWaveCheat>(), Is.SameAs(registry.Resolve<IWaveProgress>()), "dev builds publish the cheat");
            Assert.That(session.Joined.Count, Is.EqualTo(1));
        }

        [Test]
        public void MissingEnemyField_FailsAtOnce_NamingIt()
        {
            var installer = AssetDatabase.LoadAssetAtPath<InstallerAsset>(InstallerPath);
            var registry = new ServiceRegistry();
            var log = new List<string>();
            registry.Register<ISessionScene>(new FakeSession());
            registry.Register<IRunFlow>(new FakeRun(log));
            registry.Register<IWalletIncome>(new FakeIncome(log));

            var error = Assert.Throws<InvalidOperationException>(() => installer.Install(registry));

            Assert.That(error.Message, Does.Contain(nameof(IEnemyField)));
        }
    }
}
