using System;
using System.Collections.Generic;
using Game.Core;
using Game.Enemy;
using Game.Run;
using Game.SimClock;
using Game.Wallet;
using NUnit.Framework;
using UnityEditor;

namespace Game.Wave.Tests
{
    public class WaveInstallerTests
    {
        private const string InstallerPath = "Assets/Features/Wave/FeatureAssets/Public/WaveInstaller.asset";

        private static ServiceRegistry RegistryWithDependencies(FakeSession session, FakeSimClock clock = null, FakeSimRandom random = null)
        {
            var log = new List<string>();
            var registry = new ServiceRegistry();
            registry.Register<ISessionScene>(session);
            registry.Register<IRunFlow>(new FakeRun(log));
            registry.Register<IEnemyField>(new FakeEnemies());
            registry.Register<IWalletIncome>(new FakeIncome(log));
            registry.Register<ISimClock>(clock ?? new FakeSimClock());
            registry.Register<ISimRandom>(random ?? new FakeSimRandom(12345));
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
            registry.Register<ISimClock>(new FakeSimClock());
            registry.Register<ISimRandom>(new FakeSimRandom(1));

            var error = Assert.Throws<InvalidOperationException>(() => installer.Install(registry));

            Assert.That(error.Message, Does.Contain(nameof(IEnemyField)));
        }

        [Test]
        public void MissingSharedClock_FailsAtOnce_NamingIt()
        {
            var installer = AssetDatabase.LoadAssetAtPath<InstallerAsset>(InstallerPath);
            var registry = new ServiceRegistry();
            var log = new List<string>();
            registry.Register<ISessionScene>(new FakeSession());
            registry.Register<IRunFlow>(new FakeRun(log));
            registry.Register<IEnemyField>(new FakeEnemies());
            registry.Register<IWalletIncome>(new FakeIncome(log));

            var error = Assert.Throws<InvalidOperationException>(() => installer.Install(registry));

            Assert.That(error.Message, Does.Contain(nameof(ISimClock)));
        }

        [Test]
        public void RealInstaller_StepsWaveOnTheSharedClockAsWave_AndDrawsFromTheStreamNamedWave()
        {
            var installer = AssetDatabase.LoadAssetAtPath<InstallerAsset>(InstallerPath);
            var session = new FakeSession();
            var clock = new FakeSimClock();
            var random = new FakeSimRandom(12345);

            installer.Install(RegistryWithDependencies(session, clock, random));
            Assert.That(random.Asked, Is.EqualTo(new[] { "Wave" }), "Wave asks for its own stream only");

            session.Joined[0].Run();
            Assert.That(clock.Owners, Is.EqualTo(new[] { "Wave" }), "one step subscription under the owner Wave");

            session.Joined[0].DisposeAsync().GetAwaiter().GetResult();
            Assert.That(clock.Subscribers, Is.EqualTo(0), "the session end stops the steps");
        }
    }
}
