using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Game.Core;
using Game.Run;
using Game.Services.Popup;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using SessionState = Game.Core.SessionState;

namespace Game.PopupLose.Tests
{
    public sealed class PopupLoseInstallerTests
    {
        private const string InstallerPath = "Assets/Features/PopupLose/FeatureAssets/Public/PopupLoseInstaller.asset";
        private const string PrefabPath = "Assets/Features/PopupLose/FeatureAssets/Public/LoseMain.prefab";

        private static PopupLoseInstaller LoadInstaller()
        {
            var installer = AssetDatabase.LoadAssetAtPath<PopupLoseInstaller>(InstallerPath);
            Assert.That(installer, Is.Not.Null, $"installer asset missing at {InstallerPath}");
            return installer;
        }

        private static ServiceRegistry FullRegistry(FakeSession session, LosePopupTests.FakeFlow flow)
        {
            var registry = new ServiceRegistry();
            var popups = new LosePopupTests.FakePopups();
            registry.Register<ISessionScene>(session);
            registry.Register<IRunFlow>(flow);
            registry.Register<IRunScore>(new LosePopupTests.FakeScore());
            registry.Register<IPopupService>(popups);
            registry.Register<IPopupViews>(popups);
            return registry;
        }

        [Test]
        public void RealInstallerJoinsTheSessionWithoutCheat()
        {
            var installer = LoadInstaller();
            Assert.That(installer.Cheat, Is.EqualTo(LoseCheatState.None), "the shipped asset keeps the cheat off");
            var session = new FakeSession();

            installer.Install(FullRegistry(session, new LosePopupTests.FakeFlow()));

            Assert.That(session.Joined, Has.Count.EqualTo(1));
            Assert.That(session.Joined[0], Is.InstanceOf<LosePopupParticipant>());
        }

        [Test]
        public void MissingRunIsNamedAtInstall()
        {
            var installer = LoadInstaller();
            var registry = new ServiceRegistry();
            registry.Register<ISessionScene>(new FakeSession());

            var error = Assert.Throws<InvalidOperationException>(() => installer.Install(registry));
            Assert.That(error.Message, Does.Contain(nameof(IRunFlow)));
        }

        [Test]
        public void MissingPopupServiceIsNamedAtInstall()
        {
            var installer = LoadInstaller();
            var registry = new ServiceRegistry();
            registry.Register<ISessionScene>(new FakeSession());
            registry.Register<IRunFlow>(new LosePopupTests.FakeFlow());
            registry.Register<IRunScore>(new LosePopupTests.FakeScore());

            var error = Assert.Throws<InvalidOperationException>(() => installer.Install(registry));
            Assert.That(error.Message, Does.Contain(nameof(IPopupService)));
        }

        [Test]
        public void CheatStateAlsoJoinsTheCheat()
        {
            var installer = ScriptableObject.CreateInstance<PopupLoseInstaller>();
            try
            {
                installer.Cheat = LoseCheatState.NewRecord;
                var session = new FakeSession();

                installer.Install(FullRegistry(session, new LosePopupTests.FakeFlow()));

                Assert.That(session.Joined, Has.Count.EqualTo(2));
                Assert.That(session.Joined[1], Is.InstanceOf<LoseCheatParticipant>());
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(installer);
            }
        }

        [Test]
        public void PopupPrefabIsUiWithLoseViewOnItsRoot()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            Assert.That(prefab, Is.Not.Null, $"popup prefab missing at {PrefabPath}");
            Assert.That(prefab.transform, Is.InstanceOf<RectTransform>());
            Assert.That(prefab.GetComponent<ILoseView>(), Is.Not.Null);
        }

        internal sealed class FakeSession : ISessionScene
        {
            public readonly List<ISessionParticipant> Joined = new List<ISessionParticipant>();
            public SessionState State => SessionState.Init;
            public event Action<SessionState> StateChanged { add { } remove { } }
            public void Join(ISessionParticipant participant) => Joined.Add(participant);
            public UniTask EndAsync() => UniTask.CompletedTask;
        }
    }
}
