using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Game.Core;
using Game.Services.SessionScene;
using NUnit.Framework;
using UnityEditor;
// UnityEditor has its own SessionState class; the one meant here is the session contract's enum.
using SessionState = Game.Services.SessionScene.SessionState;

namespace Game.SceneFlow.Tests
{
    public class SceneFlowTests
    {
        private const string FailedMessage = "cannot load";

        private List<string> log;
        private List<Exception> errors;
        private FakeSession session;
        private FakeScenes scenes;
        private FakeScreens screens;
        private LoadingToGameFlow flow;

        [SetUp]
        public void SetUp()
        {
            log = new List<string>();
            errors = new List<Exception>();
            session = new FakeSession(log);
            scenes = new FakeScenes(log) { Gate = new SceneGate() };
            screens = new FakeScreens(log);
            flow = new LoadingToGameFlow(session, scenes, screens,
                new SceneFlowSettings("Loading", "Game", FailedMessage), errors.Add);
        }

        [Test]
        public void WhenTheLoadingSceneRuns_TheLoadingScreenShows_AndTheGameSceneLoadsOnTop()
        {
            flow.Run();

            Assert.That(log, Is.EqualTo(new[] { "screen shown", "load Game" }));
        }

        [Test]
        public void TheLoadingScreenStays_UntilTheGameSceneIsPlayable()
        {
            flow.Run();

            Assert.That(session.Ended, Is.False);
            Assert.That(log, Has.None.EqualTo("unload Loading"));
            Assert.That(screens.Screen.Closed, Is.False);
        }

        [Test]
        public void WhenTheGameSceneIsPlayable_TheLoadingSessionEnds_ThenTheLoadingSceneIsRemoved()
        {
            flow.Run();
            log.Clear();

            scenes.Gate.Open();

            Assert.That(log, Is.EqualTo(new[] { "end session", "unload Loading" }));
            Assert.That(errors, Is.Empty);
        }

        [Test]
        public void WhenTheGameSceneWasPlayableBeforeAnyoneAsked_TheSwitchStillHappens()
        {
            scenes.Gate.Open();

            flow.Run();

            Assert.That(log, Has.Member("unload Loading"));
        }

        [Test]
        public void WhenTheGameSceneFails_ThePlayerSeesTheError_AndTheLoadingSceneStays()
        {
            flow.Run();

            scenes.Gate.Fail();

            Assert.That(screens.Screen.Error, Is.EqualTo(FailedMessage));
            Assert.That(session.Ended, Is.False);
            Assert.That(log, Has.None.EqualTo("unload Loading"));
        }

        [Test]
        public void WhenTheGameSceneHasNoGate_ThePlayerSeesTheError_AndTheCauseIsReported()
        {
            scenes.Gate = null;

            flow.Run();

            Assert.That(screens.Screen.Error, Is.EqualTo(FailedMessage));
            Assert.That(errors, Has.Count.EqualTo(1));
            Assert.That(errors[0].Message, Does.Contain("'Game'").And.Contain("gate"));
            Assert.That(session.Ended, Is.False);
        }

        [Test]
        public void WhenTheGameSceneCannotBeLoaded_ThePlayerSeesTheError_AndTheCauseIsReported()
        {
            scenes.LoadError = new InvalidOperationException("not in Build Settings");

            flow.Run();

            Assert.That(screens.Screen.Error, Is.EqualTo(FailedMessage));
            Assert.That(errors, Has.Count.EqualTo(1));
            Assert.That(errors[0].Message, Does.Contain("not in Build Settings"));
        }

        [Test]
        public void WhenTheLoadingSessionEnds_TheLoadingScreenCloses()
        {
            flow.Run();

            flow.DisposeAsync().Forget();

            Assert.That(screens.Screen.Closed, Is.True);
        }

        [Test]
        public void WhenTheLoadingSceneGoesAwayFirst_ALateAnswerFromTheGameSceneChangesNothing()
        {
            flow.Run();
            flow.DisposeAsync().Forget();
            log.Clear();

            scenes.Gate.Open();

            Assert.That(log, Is.Empty);
        }

        [Test]
        public void GameScene_BecomesTheActiveScene_BeforeAnyFeatureOpensAView()
        {
            // Regression: views opened in Run were created in the still-active Loading scene and
            // disappeared with it. Loading comes before every Run, so activating there is in time.
            var signal = new GameReadySignal(session, new SceneGate(), scenes, "Game");

            signal.LoadAsync(CancellationToken.None).Forget();

            Assert.That(log, Is.EqualTo(new[] { "activate Game" }));
        }

        [Test]
        public void GameScene_OpensTheGate_WhenItsSessionRuns()
        {
            var gate = new SceneGate();
            var signal = new GameReadySignal(session, gate, scenes, "Game");

            signal.Run();

            Assert.That(Answer(gate), Is.True);
        }

        [Test]
        public void GameScene_MarksTheGateFailed_WhenItsSessionFails()
        {
            var gate = new SceneGate();
            var signal = new GameReadySignal(session, gate, scenes, "Game");

            session.Raise(SessionState.Failed);

            Assert.That(Answer(gate), Is.False);
            Assert.That(signal, Is.Not.Null);
        }

        [Test]
        public void GameScene_GateStaysUnanswered_WhileItsSessionIsStillLoading()
        {
            var gate = new SceneGate();
            var signal = new GameReadySignal(session, gate, scenes, "Game");

            session.Raise(SessionState.Loading);

            Assert.That(gate.WaitAsync().Status, Is.EqualTo(UniTaskStatus.Pending));
            Assert.That(signal, Is.Not.Null);
        }

        [Test]
        public void LoadingScreenPrefab_WhenAnErrorIsShown_HidesTheLoadingTextAndShowsTheError()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(
                "Assets/Features/SceneFlow/FeatureAssets/Private/LoadingScreen.prefab");
            Assert.That(prefab, Is.Not.Null, "LoadingScreen.prefab missing");
            var instance = UnityEngine.Object.Instantiate(prefab);
            try
            {
                var title = instance.transform.Find("Title");
                var error = instance.transform.Find("Error");
                Assert.That(title, Is.Not.Null, "LoadingScreen.prefab has no child named Title");
                Assert.That(error, Is.Not.Null, "LoadingScreen.prefab has no child named Error");
                Assert.That(title.gameObject.activeSelf, Is.True, "the loading text should show before any error");
                Assert.That(error.gameObject.activeSelf, Is.False, "no error should show before one happens");

                instance.GetComponentInChildren<ILoadingScreen>(true).ShowError(FailedMessage);

                Assert.That(title.gameObject.activeSelf, Is.False, "the loading text should hide once an error shows");
                Assert.That(error.gameObject.activeSelf, Is.True);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(instance);
            }
        }

        [TestCase("SceneFlowLoadingInstaller")]
        [TestCase("SceneFlowGameInstaller")]
        public void InstallerAsset_JoinsTheSceneSession(string assetName)
        {
            var installer = LoadInstaller(assetName);
            var registry = new ServiceRegistry();
            registry.Register<ISessionScene>(session);

            // Also proves the asset's config and prefabs are assigned: Install throws without them.
            installer.Install(registry);

            Assert.That(session.Joined, Has.Count.EqualTo(1));
        }

        [Test]
        public void InstallerAsset_WithoutASessionInstalledFirst_FailsAtInstall()
        {
            var installer = LoadInstaller("SceneFlowLoadingInstaller");

            Assert.That(() => installer.Install(new ServiceRegistry()), Throws.Exception);
        }

        private static SceneFlowInstaller LoadInstaller(string assetName)
        {
            var installer = AssetDatabase.LoadAssetAtPath<SceneFlowInstaller>(
                $"Assets/Features/SceneFlow/FeatureAssets/Public/{assetName}.asset");
            Assert.That(installer, Is.Not.Null, $"{assetName}.asset missing or its script reference is broken");
            return installer;
        }

        private static bool Answer(SceneGate gate)
        {
            var waiting = gate.WaitAsync();
            Assert.That(waiting.Status, Is.EqualTo(UniTaskStatus.Succeeded), "the gate has no answer yet");
            return waiting.GetAwaiter().GetResult();
        }

        private sealed class FakeSession : ISessionScene
        {
            private readonly List<string> log;

            public FakeSession(List<string> log)
            {
                this.log = log;
            }

            public readonly List<ISessionParticipant> Joined = new List<ISessionParticipant>();

            public bool Ended { get; private set; }

            public SessionState State { get; private set; } = SessionState.Init;

            public event Action<SessionState> StateChanged;

            public void Join(ISessionParticipant participant)
            {
                Joined.Add(participant);
            }

            public UniTask EndAsync()
            {
                Ended = true;
                log.Add("end session");
                return UniTask.CompletedTask;
            }

            public void Raise(SessionState state)
            {
                State = state;
                StateChanged?.Invoke(state);
            }
        }

        private sealed class FakeScenes : ISceneLoader
        {
            private readonly List<string> log;

            public FakeScenes(List<string> log)
            {
                this.log = log;
            }

            public SceneGate Gate;
            public Exception LoadError;

            public UniTask LoadAdditiveAsync(string sceneName)
            {
                log.Add("load " + sceneName);
                if (LoadError != null) throw LoadError;
                return UniTask.CompletedTask;
            }

            public UniTask UnloadAsync(string sceneName)
            {
                log.Add("unload " + sceneName);
                return UniTask.CompletedTask;
            }

            public void SetActive(string sceneName)
            {
                log.Add("activate " + sceneName);
            }

            public SceneGate FindGate()
            {
                return Gate;
            }
        }

        private sealed class FakeScreens : ILoadingScreenSource
        {
            private readonly List<string> log;

            public FakeScreens(List<string> log)
            {
                this.log = log;
            }

            public FakeScreen Screen { get; private set; }

            public ILoadingScreen Create()
            {
                log.Add("screen shown");
                Screen = new FakeScreen();
                return Screen;
            }
        }

        private sealed class FakeScreen : ILoadingScreen
        {
            public string Error { get; private set; }

            public bool Closed { get; private set; }

            public void ShowError(string message)
            {
                Error = message;
            }

            public void Close()
            {
                Closed = true;
            }
        }
    }
}
