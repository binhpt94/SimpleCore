using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Game.Core;
using Game.Run;
using Game.SimClock;
using NUnit.Framework;
using UnityEditor;
using SessionState = Game.Core.SessionState;

namespace Game.Combat.Tests
{
    public class CombatInstallerTests
    {
        private const string InstallerPath = "Assets/Features/Combat/FeatureAssets/Public/CombatInstaller.asset";

        private FakeRegistry _registry;
        private FakeSession _session;
        private FakeSimClock _clock;
        private FakeRunFlow _run;

        [SetUp]
        public void SetUp()
        {
            _registry = new FakeRegistry();
            _session = new FakeSession();
            _clock = new FakeSimClock();
            _run = new FakeRunFlow();
            _registry.Register<ISessionScene>(_session);
            _registry.Register<ISimClock>(_clock);
            _registry.Register<IRunFlow>(_run);
        }

        private static CombatInstaller LoadInstaller()
        {
            var installer = AssetDatabase.LoadAssetAtPath<CombatInstaller>(InstallerPath);
            Assert.IsNotNull(installer, $"missing {InstallerPath}");
            return installer;
        }

        [Test]
        public void InstallingCombatPublishesItForTheFeaturesThatDealHits()
        {
            LoadInstaller().Install(_registry);

            Assert.IsTrue(_registry.TryResolve<ICombat>(out var combat));
            Assert.IsInstanceOf<CombatTick>(combat);
        }

        [Test]
        public void CombatJoinsTheSceneSessionWithoutAView()
        {
            LoadInstaller().Install(_registry);

            Assert.AreEqual(1, _session.Joined.Count);
            var usage = (ISessionParticipantUsage)_session.Joined[0];
            Assert.IsFalse(usage.OpensViewsOnRun);
        }

        [TestCase(typeof(ISessionScene), "SessionScene")]
        [TestCase(typeof(ISimClock), "SimClock")]
        [TestCase(typeof(IRunFlow), "Run")]
        public void MissingDependencyFailsTheInstallNamingIt(Type missing, string feature)
        {
            _registry.Remove(missing);

            var error = Assert.Throws<InvalidOperationException>(() => LoadInstaller().Install(_registry));

            StringAssert.Contains(missing.Name, error.Message);
            StringAssert.Contains(feature, error.Message);
            Assert.IsFalse(_registry.TryResolve<ICombat>(out _), "nothing is published when an install fails");
        }

        [Test]
        public void CombatTicksOnTheSharedClockOnlyOnceTheSessionRuns()
        {
            LoadInstaller().Install(_registry);
            var participant = (ISessionParticipant)_session.Joined[0];
            var combat = _registry.Resolve<ICombat>();
            var done = 0;
            combat.TickDone += () => done++;
            _run.StartRun();
            _run.StartBattle();

            _clock.Step();
            Assert.AreEqual(0, done, "no tick before the session runs");

            participant.Run();
            _run.StartRun();
            _run.StartBattle();
            _clock.Step(2);

            Assert.AreEqual(2, done);
            Assert.AreEqual(1, ((ISessionParticipantUsage)participant).RunningJobs, "the loop on the clock is a tracked job");
        }

        [Test]
        public void EndingTheSessionStopsTheLoopAndReleasesItsJob()
        {
            LoadInstaller().Install(_registry);
            var participant = (ISessionParticipant)_session.Joined[0];
            var combat = _registry.Resolve<ICombat>();
            var done = 0;
            combat.TickDone += () => done++;
            participant.Run();
            _run.StartRun();
            _run.StartBattle();

            participant.DisposeAsync().GetAwaiter().GetResult();
            _clock.Step(3);

            Assert.AreEqual(0, done);
            Assert.AreEqual(0, _clock.Subscribers);
            Assert.AreEqual(0, ((ISessionParticipantUsage)participant).RunningJobs);
        }

        [Test]
        public void HitsWaitingWhenTheSessionEndsAreDropped()
        {
            LoadInstaller().Install(_registry);
            var participant = (ISessionParticipant)_session.Joined[0];
            var combat = _registry.Resolve<ICombat>();
            var enemies = new FakeOwner("Enemy");
            enemies.Add(1, 50f);
            var id = combat.AddReceiver(enemies);
            participant.Run();
            combat.Hit(new CombatHit(id, 1, 10f, CombatSource.Hero));

            participant.DisposeAsync().GetAwaiter().GetResult();
            ((CombatTick)combat).Run(0.025f);

            Assert.AreEqual(50f, enemies.Health(1));
        }

        private sealed class FakeRegistry : IServiceRegistry
        {
            private readonly Dictionary<Type, object> _services = new Dictionary<Type, object>();

            public void Register<T>(T service) where T : class => _services[typeof(T)] = service;

            public T Resolve<T>() where T : class => (T)_services[typeof(T)];

            public bool TryResolve<T>(out T service) where T : class
            {
                var found = _services.TryGetValue(typeof(T), out var value);
                service = found ? (T)value : null;
                return found;
            }

            public void Remove(Type type) => _services.Remove(type);
        }

        private sealed class FakeSession : ISessionScene
        {
            public readonly List<ISessionParticipant> Joined = new List<ISessionParticipant>();

            public SessionState State => SessionState.Init;

            public event Action<SessionState> StateChanged { add { } remove { } }

            public void Join(ISessionParticipant participant) => Joined.Add(participant);

            public UniTask EndAsync() => UniTask.CompletedTask;
        }
    }
}
