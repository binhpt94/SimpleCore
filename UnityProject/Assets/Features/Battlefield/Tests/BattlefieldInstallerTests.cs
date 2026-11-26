using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Game.Core;
using Game.Run;
using NUnit.Framework;

namespace Game.Battlefield.Tests
{
    public class BattlefieldInstallerTests
    {
        [Test]
        public void RealInstaller_WithRunAndSession_JoinsTheBackdropToTheSession()
        {
            var registry = new ServiceRegistry();
            var session = new FakeSession();
            registry.Register<ISessionScene>(session);
            registry.Register<IRunFlow>(new NullRunFlow());

            BattlefieldTestAssets.Installer().Install(registry);

            Assert.That(session.Joined.Count, Is.EqualTo(1));
            Assert.That(session.Joined[0], Is.InstanceOf<BattlefieldParticipant>());
        }

        [Test]
        public void RealInstaller_WithoutTheRun_FailsAtOnceNamingIt()
        {
            var registry = new ServiceRegistry();
            registry.Register<ISessionScene>(new FakeSession());

            var error = Assert.Throws<InvalidOperationException>(() => BattlefieldTestAssets.Installer().Install(registry));

            Assert.That(error.Message, Does.Contain("IRunFlow"));
        }

        [Test]
        public void RealInstaller_WithoutTheSession_FailsAtOnceNamingIt()
        {
            var registry = new ServiceRegistry();
            registry.Register<IRunFlow>(new NullRunFlow());

            var error = Assert.Throws<InvalidOperationException>(() => BattlefieldTestAssets.Installer().Install(registry));

            Assert.That(error.Message, Does.Contain("ISessionScene"));
        }

        private sealed class FakeSession : ISessionScene
        {
            public readonly List<ISessionParticipant> Joined = new List<ISessionParticipant>();
            public SessionState State => SessionState.Init;
            public event Action<SessionState> StateChanged { add { } remove { } }
            public void Join(ISessionParticipant participant) => Joined.Add(participant);
            public UniTask EndAsync() => UniTask.CompletedTask;
        }

        private sealed class NullRunFlow : IRunFlow
        {
            public RunStep Step => RunStep.Home;
            public event Action<RunStep> StepChanged { add { } remove { } }
            public RunTeam Team => default;
            public event Action<RunTeam> RunStarted { add { } remove { } }
            public event Action BattleStarted { add { } remove { } }
            public event Action BattleStopped { add { } remove { } }
            public event Action BattleCleared { add { } remove { } }
            public void Play() { }
            public void EnterBattle(RunTeam team) { }
            public void Replay() { }
            public void GoHome() { }
            public void Lose() { }
            public void CountClearedWave(int wave) { }
        }
    }
}
