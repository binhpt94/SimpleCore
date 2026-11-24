using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Game.Core;
using NUnit.Framework;
using UnityEditor;
using SessionState = Game.Core.SessionState;

namespace Game.Run.Tests
{
    public class RunInstallerTests
    {
        private const string InstallerPath = "Assets/Features/Run/FeatureAssets/Public/RunInstaller.asset";

        [Test]
        public void InstallerAssetPublishesTheRunAndJoinsTheSession()
        {
            var installer = AssetDatabase.LoadAssetAtPath<RunInstaller>(InstallerPath);
            Assert.IsNotNull(installer, $"Missing {InstallerPath}");

            var registry = new ServiceRegistry();
            var session = new FakeSession();
            registry.Register<ISessionScene>(session);

            installer.Install(registry);

            Assert.IsTrue(registry.TryResolve<IRunFlow>(out var flow));
            Assert.IsTrue(registry.TryResolve<IRunScore>(out _));
            Assert.AreEqual(RunStep.Home, flow.Step);
            Assert.AreEqual(1, session.Joined.Count);
        }

        [Test]
        public void InstallWithoutSessionSceneFailsNamingIt()
        {
            var installer = AssetDatabase.LoadAssetAtPath<RunInstaller>(InstallerPath);
            Assert.IsNotNull(installer, $"Missing {InstallerPath}");

            var error = Assert.Throws<InvalidOperationException>(() => installer.Install(new ServiceRegistry()));
            StringAssert.Contains(nameof(ISessionScene), error.Message);
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
