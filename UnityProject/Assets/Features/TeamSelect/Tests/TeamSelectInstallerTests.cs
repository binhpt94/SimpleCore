using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Game.Core;
using Game.Hero;
using Game.Run;
using NUnit.Framework;

namespace Game.TeamSelect.Tests
{
    public class TeamSelectInstallerTests
    {
        private const string InstallerPath = "Assets/Features/TeamSelect/FeatureAssets/Public/TeamSelectInstaller.asset";

        private sealed class FakeSession : ISessionScene
        {
            public readonly List<ISessionParticipant> Joined = new List<ISessionParticipant>();
            public Game.Core.SessionState State => Game.Core.SessionState.Init;
            public event Action<Game.Core.SessionState> StateChanged { add { } remove { } }
            public void Join(ISessionParticipant participant) => Joined.Add(participant);
            public UniTask EndAsync() => UniTask.CompletedTask;
        }

        private static TeamSelectInstaller LoadInstaller()
        {
            var installer = UnityEditor.AssetDatabase.LoadAssetAtPath<TeamSelectInstaller>(InstallerPath);
            Assert.IsNotNull(installer, "missing " + InstallerPath);
            return installer;
        }

        [Test]
        public void InstallerAssetJoinsTheGameSession()
        {
            var registry = new ServiceRegistry();
            var session = new FakeSession();
            registry.Register<IRunFlow>(new FakeRun());
            registry.Register<IHeroRoster>(new FakeRoster());
            registry.Register<ISessionScene>(session);

            LoadInstaller().Install(registry);

            Assert.AreEqual(1, session.Joined.Count);
            Assert.IsInstanceOf<TeamSelectSession>(session.Joined[0]);
        }

        [Test]
        public void InstallerFailsAtOnceNamingMissingRun()
        {
            var registry = new ServiceRegistry();
            registry.Register<IHeroRoster>(new FakeRoster());
            registry.Register<ISessionScene>(new FakeSession());
            var error = Assert.Throws<InvalidOperationException>(() => LoadInstaller().Install(registry));
            StringAssert.Contains(nameof(IRunFlow), error.Message);
        }
    }
}
