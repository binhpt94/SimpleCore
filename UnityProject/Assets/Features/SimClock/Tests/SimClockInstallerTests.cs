using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Game.Core;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using static Game.SimClock.Tests.SimClockTestKit;
using SessionState = Game.Core.SessionState;

namespace Game.SimClock.Tests
{
    /// <summary>The real installer asset, installed into a registry with a fake session.</summary>
    public class SimClockInstallerTests
    {
        private sealed class FakeSession : ISessionScene
        {
            internal readonly List<ISessionParticipant> Joined = new List<ISessionParticipant>();

            public SessionState State => SessionState.Init;

            public event Action<SessionState> StateChanged { add { } remove { } }

            public void Join(ISessionParticipant participant) => Joined.Add(participant);

            public UniTask EndAsync() => UniTask.CompletedTask;
        }

        [Test]
        public void Install_PublishesClockRunControlAndRandom_AndJoinsTheSession()
        {
            var installer = AssetDatabase.LoadAssetAtPath<SimClockInstaller>(InstallerPath);
            Assert.That(installer, Is.Not.Null, InstallerPath);
            var registry = new ServiceRegistry();
            var session = new FakeSession();
            registry.Register<ISessionScene>(session);

            installer.Install(registry);

            var clock = registry.Resolve<ISimClock>();
            Assert.That(registry.Resolve<ISimRunControl>(), Is.SameAs(clock));
            Assert.That(registry.Resolve<ISimRandom>(), Is.SameAs(clock));
            Assert.That(clock.Mode, Is.EqualTo(SimMode.FixedTick));
            Assert.That(session.Joined, Has.Count.EqualTo(1));
            Assert.That(registry.Resolve<ISimRandom>().Stream("Wave"), Is.Not.Null);
        }

        [Test]
        public void Install_WithoutSessionScene_FailsNamingIt()
        {
            var installer = AssetDatabase.LoadAssetAtPath<SimClockInstaller>(InstallerPath);
            var ex = Assert.Throws<InvalidOperationException>(() => installer.Install(new ServiceRegistry()));
            Assert.That(ex.Message, Does.Contain(nameof(ISessionScene)));
        }

        [Test]
        public void Install_WithoutConfig_FailsNamingIt()
        {
            var installer = ScriptableObject.CreateInstance<SimClockInstaller>();
            try
            {
                var registry = new ServiceRegistry();
                registry.Register<ISessionScene>(new FakeSession());
                var ex = Assert.Throws<InvalidOperationException>(() => installer.Install(registry));
                Assert.That(ex.Message, Does.Contain("SimClockConfig"));
            }
            finally
            {
                Destroy(installer);
            }
        }
    }
}
