using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Game.Core;
using Game.Home;
using Game.Run;
using Game.SceneFlow;
using Game.TeamSelect;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using SessionState = Game.Core.SessionState;

namespace Game.GameFlow.Tests
{
    public class GameFlowInstallerTests
    {
        private const string InstallerPath = "Assets/Features/GameFlow/FeatureAssets/Public/GameFlowInstaller.asset";
        private const string ConfigPath = "Assets/Features/GameFlow/FeatureAssets/Private/GameFlowConfig.asset";

        private readonly List<UnityEngine.Object> _made = new List<UnityEngine.Object>();

        [TearDown]
        public void TearDown()
        {
            foreach (var o in _made) UnityEngine.Object.DestroyImmediate(o);
            _made.Clear();
        }

        [Test]
        public void InstallerAsset_WithItsRealConfig_JoinsTheSession_AndLeavesSceneReadyForLater()
        {
            var installer = AssetDatabase.LoadAssetAtPath<GameFlowInstaller>(InstallerPath);
            Assert.That(installer, Is.Not.Null, InstallerPath + " missing");
            var registry = FullRegistry(out var session);

            Assert.DoesNotThrow(() => installer.Install(registry));

            Assert.That(session.Joined, Has.Count.EqualTo(1));
            Assert.That(session.Joined[0], Is.TypeOf<GameFlowParticipant>());
            Assert.That(registry.Registered, Is.Empty, "GameFlow publishes no contract");
            Assert.That(registry.Resolved, Has.No.Member(typeof(IGameSceneReady)),
                "SceneFlow installs last: its signal is taken when the session runs");
        }

        [Test]
        public void ConfigAsset_HoldsTheDesignedValues_AndRoutesTheScreensAtPrototype()
        {
            var config = AssetDatabase.LoadAssetAtPath<GameFlowConfig>(ConfigPath);
            Assert.That(config, Is.Not.Null, ConfigPath + " missing");

            var settings = config.ToSettings();
            Assert.That(config.RouteScreens, Is.True);
            Assert.That(settings.SwitchSeconds, Is.EqualTo(0f));
            Assert.That(settings.BuildLimitSeconds, Is.EqualTo(3f));
            Assert.That(settings.BuildFailedText, Is.EqualTo("Không vào được trận, thử lại"));
            Assert.That(settings.BackButtonMinSidePx, Is.EqualTo(120));
            Assert.That(settings.BackButtonCorner, Is.EqualTo(ScreenCorner.TopLeft));
        }

        [Test]
        public void Install_WithoutConfig_FailsNamingTheConfig()
        {
            var installer = Make<GameFlowInstaller>();

            var error = Assert.Throws<InvalidOperationException>(() => installer.Install(new FakeRegistry()));
            Assert.That(error.Message, Does.Contain("GameFlowConfig"));
        }

        [Test]
        public void Install_WithAConfigValueThatCannotWork_Fails()
        {
            var config = Make<GameFlowConfig>();
            config.SetBuildLimitForTest(0f);
            var installer = Make<GameFlowInstaller>();
            installer.SetConfigForTest(config);

            Assert.Throws<ArgumentOutOfRangeException>(() => installer.Install(new FakeRegistry()));
        }

        [Test]
        public void Install_WithRouteScreensOff_JoinsNothing()
        {
            var installer = InstallerWith(routeScreens: false);
            var registry = FullRegistry(out var session);

            installer.Install(registry);

            Assert.That(session.Joined, Is.Empty);
            Assert.That(registry.Resolved, Is.Empty);
        }

        [TestCase(typeof(ISessionScene))]
        [TestCase(typeof(IHomeScreen))]
        [TestCase(typeof(ITeamSelectScreen))]
        [TestCase(typeof(IRunBattle))]
        public void Install_WithRouteScreensOn_FailsNamingAMissingDependency(Type missing)
        {
            var installer = InstallerWith(routeScreens: true);
            var registry = FullRegistry(out _);
            registry.Remove(missing);

            var error = Assert.Throws<InvalidOperationException>(() => installer.Install(registry));
            Assert.That(error.Message, Does.Contain(missing.Name));
        }

        private GameFlowInstaller InstallerWith(bool routeScreens)
        {
            var config = Make<GameFlowConfig>();
            config.SetRouteScreensForTest(routeScreens);
            var installer = Make<GameFlowInstaller>();
            installer.SetConfigForTest(config);
            return installer;
        }

        private static FakeRegistry FullRegistry(out FakeSession session)
        {
            session = new FakeSession();
            var registry = new FakeRegistry();
            registry.Add<ISessionScene>(session);
            registry.Add<IHomeScreen>(new GameFlowParticipantTests.FakeHome());
            registry.Add<ITeamSelectScreen>(new GameFlowParticipantTests.FakeTeamSelect());
            registry.Add<IRunBattle>(new GameFlowParticipantTests.FakeBattle());
            registry.Add<IGameSceneReady>(new GameFlowParticipantTests.FakeSceneReady());
            return registry;
        }

        private T Make<T>() where T : ScriptableObject
        {
            var o = ScriptableObject.CreateInstance<T>();
            _made.Add(o);
            return o;
        }

        private sealed class FakeSession : ISessionScene
        {
            public readonly List<ISessionParticipant> Joined = new List<ISessionParticipant>();

            public SessionState State => SessionState.Init;

            public event Action<SessionState> StateChanged { add { } remove { } }

            public void Join(ISessionParticipant participant) => Joined.Add(participant);

            public UniTask EndAsync() => UniTask.CompletedTask;
        }

        private sealed class FakeRegistry : IServiceRegistry
        {
            private readonly Dictionary<Type, object> _services = new Dictionary<Type, object>();
            public readonly List<Type> Registered = new List<Type>();
            public readonly List<Type> Resolved = new List<Type>();

            public void Add<T>(T service) where T : class => _services[typeof(T)] = service;

            public void Remove(Type type) => _services.Remove(type);

            public void Register<T>(T service) where T : class => Registered.Add(typeof(T));

            public T Resolve<T>() where T : class
            {
                if (TryResolve<T>(out var service)) return service;
                throw new InvalidOperationException(typeof(T).Name + " not installed");
            }

            public bool TryResolve<T>(out T service) where T : class
            {
                Resolved.Add(typeof(T));
                service = _services.TryGetValue(typeof(T), out var found) ? (T)found : null;
                return service != null;
            }
        }
    }
}
