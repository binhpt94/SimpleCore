using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Game.Core;
using NUnit.Framework;
using Game.Services.Popup;
using UnityEditor;
using SessionState = Game.Core.SessionState;

namespace Game.Run.Tests
{
    public class RunInstallerTests
    {
        private const string InstallerPath = "Assets/Features/Run/FeatureAssets/Public/RunInstaller.asset";
        private const string ConfigPath = "Assets/Features/Run/FeatureAssets/Private/RunConfig.asset";

        [Test]
        public void InstallerAssetPublishesTheRunAndJoinsTheSession()
        {
            var installer = AssetDatabase.LoadAssetAtPath<RunInstaller>(InstallerPath);
            Assert.IsNotNull(installer, $"Missing {InstallerPath}");

            var registry = new ServiceRegistry();
            var session = new FakeSession();
            registry.Register<ISessionScene>(session);
            registry.Register<IPopupStack>(new FakePopupStack());

            installer.Install(registry);

            Assert.IsTrue(registry.TryResolve<IRunFlow>(out var flow));
            Assert.IsTrue(registry.TryResolve<IRunScore>(out _));
            Assert.IsTrue(registry.TryResolve<IRunRevive>(out var revive));
            Assert.AreSame(flow, revive, "IRunRevive must be the same object as IRunFlow.");
            Assert.AreEqual(RunStep.Home, flow.Step);
            Assert.AreEqual(2, session.Joined.Count, "The run and its leftover popup check join the session.");
        }

        [Test]
        public void ConfigAssetHoldsTheIdeaRunValues()
        {
            var config = AssetDatabase.LoadAssetAtPath<RunConfig>(ConfigPath);
            Assert.IsNotNull(config, $"Missing {ConfigPath}");

            Assert.AreEqual(1f, config.LoseDelaySeconds);
            Assert.AreEqual(3, config.TeamSize);
            Assert.IsTrue(config.SkipMenus);
            CollectionAssert.AreEqual(new[] { "knight", "axeman", "archer" }, config.DefaultTeam.HeroCodes);
        }

        [Test]
        public void ConfigAssetKeepsTheIdeaBattleRunningWhenLeavingTheGameOrPressingBack()
        {
            // pause-rules "Thông số theo giai đoạn": IDEA has no auto pause and no back key pause.
            var config = AssetDatabase.LoadAssetAtPath<RunConfig>(ConfigPath);
            Assert.IsNotNull(config, $"Missing {ConfigPath}");

            Assert.IsFalse(config.PauseOnLeavingApp);
            Assert.IsFalse(config.BackKeyPauses);
        }

        [Test]
        public void IdeaRunHasNoPauseSoEveryPauseRequestIsIgnored()
        {
            // pause-rules "Thông số theo giai đoạn": IDEA has no pause button, no auto pause, no back key pause.
            var config = AssetDatabase.LoadAssetAtPath<RunConfig>(ConfigPath);
            Assert.IsNotNull(config, $"Missing {ConfigPath}");
            Assert.IsFalse(config.PauseEnabled);

            var installer = AssetDatabase.LoadAssetAtPath<RunInstaller>(InstallerPath);
            var registry = new ServiceRegistry();
            registry.Register<ISessionScene>(new FakeSession());
            registry.Register<IPopupStack>(new FakePopupStack());
            installer.Install(registry);
            var flow = (RunFlow)registry.Resolve<IRunFlow>();
            Assert.IsFalse(flow.PauseEnabled);

            flow.Begin();
            flow.Pause();
            Assert.AreEqual(RunStep.Playing, flow.Step);
            Assert.IsFalse(flow.IsPaused);
            flow.Shutdown();
        }

        [Test]
        public void IdeaRunHasNoReviveSoTheFirstCastleFallLoses()
        {
            // flow-rules "Thông số theo giai đoạn": IDEA has no revive step.
            var config = AssetDatabase.LoadAssetAtPath<RunConfig>(ConfigPath);
            Assert.IsNotNull(config, $"Missing {ConfigPath}");
            Assert.IsFalse(config.ReviveOnce);

            var installer = AssetDatabase.LoadAssetAtPath<RunInstaller>(InstallerPath);
            var registry = new ServiceRegistry();
            registry.Register<ISessionScene>(new FakeSession());
            registry.Register<IPopupStack>(new FakePopupStack());
            installer.Install(registry);
            var flow = (RunFlow)registry.Resolve<IRunFlow>();
            Assert.IsFalse(flow.ReviveOnce);

            flow.Begin();
            flow.Lose();
            Assert.AreEqual(RunStep.Ending, flow.Step);
            flow.Shutdown();
        }

        [Test]
        public void ConfigAssetHasTheDevicePrefabThatHearsLeavingAndBackKey()
        {
            var config = AssetDatabase.LoadAssetAtPath<RunConfig>(ConfigPath);
            Assert.IsNotNull(config, $"Missing {ConfigPath}");
            Assert.IsNotNull(config.AppSignalsPrefab, "RunConfig has no app signals prefab.");
            Assert.IsNotNull(config.AppSignalsPrefab.GetComponent<IAppSignals>(),
                "The app signals prefab has no RunAppSignalsView.");
        }

        [Test]
        public void InstalledRunUsesTheConfigAndGoesStraightIntoBattleWithTheDefaultTeam()
        {
            var installer = AssetDatabase.LoadAssetAtPath<RunInstaller>(InstallerPath);
            Assert.IsNotNull(installer, $"Missing {InstallerPath}");

            var registry = new ServiceRegistry();
            registry.Register<ISessionScene>(new FakeSession());
            registry.Register<IPopupStack>(new FakePopupStack());
            installer.Install(registry);

            var flow = registry.Resolve<IRunFlow>() as RunFlow;
            Assert.IsNotNull(flow, "IRunFlow is not the run's RunFlow.");
            Assert.AreEqual(1f, flow.LoseDelaySeconds);
            Assert.AreEqual(3, flow.TeamSize);
            Assert.IsTrue(flow.SkipMenus);

            flow.Begin();
            Assert.AreEqual(RunStep.Playing, flow.Step);
            CollectionAssert.AreEqual(new[] { "knight", "axeman", "archer" }, flow.Team.HeroCodes);
            flow.Shutdown();
        }

        [Test]
        public void InstallWithoutSessionSceneFailsNamingIt()
        {
            var installer = AssetDatabase.LoadAssetAtPath<RunInstaller>(InstallerPath);
            Assert.IsNotNull(installer, $"Missing {InstallerPath}");

            var error = Assert.Throws<InvalidOperationException>(() => installer.Install(new ServiceRegistry()));
            StringAssert.Contains(nameof(ISessionScene), error.Message);
        }

        [Test]
        public void InstallWithoutPopupStackFailsNamingIt()
        {
            var installer = AssetDatabase.LoadAssetAtPath<RunInstaller>(InstallerPath);
            Assert.IsNotNull(installer, $"Missing {InstallerPath}");

            var registry = new ServiceRegistry();
            registry.Register<ISessionScene>(new FakeSession());
            var error = Assert.Throws<InvalidOperationException>(() => installer.Install(registry));
            StringAssert.Contains(nameof(IPopupStack), error.Message);
        }

        private sealed class FakePopupStack : IPopupStack
        {
            public IReadOnlyList<PopupId> OpenIds => Array.Empty<PopupId>();
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
