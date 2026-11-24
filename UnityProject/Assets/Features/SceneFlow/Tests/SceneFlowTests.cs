using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Game.Core;
using Game.Services.SessionScene;
using NUnit.Framework;
using UnityEditor;
// UnityEditor has its own SessionState class; the one meant here is Core's session enum.
using SessionState = Game.Core.SessionState;

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
        private FakeProgress progress;
        private FakeTimer timer;
        private LoadingToGameFlow flow;

        [SetUp]
        public void SetUp()
        {
            log = new List<string>();
            errors = new List<Exception>();
            session = new FakeSession(log);
            progress = new FakeProgress(4);
            scenes = new FakeScenes(log) { Gate = new SceneGate(progress) };
            screens = new FakeScreens(log);
            timer = new FakeTimer();
            flow = new LoadingToGameFlow(session, scenes, screens, timer,
                new SceneFlowSettings("Loading", "Game", FailedMessage, "Lỗi tải: {0}", TimeSpan.FromSeconds(1)),
                errors.Add);
        }

        [Test]
        public void WhenOneGameSceneFeatureFailsToLoad_TheErrorNamesThatFeature()
        {
            flow.Run();

            progress.Fail("Wallet");
            scenes.Gate.Fail();

            Assert.That(screens.Screen.Error, Is.EqualTo("Lỗi tải: Wallet"));
        }

        [Test]
        public void WhenTheFailedFeatureIsNamedJustAfterTheFailure_TheErrorStillNamesIt()
        {
            flow.Run();

            scenes.Gate.Fail();
            progress.Fail("Wallet");

            Assert.That(screens.Screen.Error, Is.EqualTo("Lỗi tải: Wallet"));
        }

        [Test]
        public void TheLoadingBar_StartsEmpty_AndFillsAsGameSceneFeaturesFinishLoading()
        {
            flow.Run();
            Assert.That(screens.Screen.Progress, Is.EqualTo(0f));

            progress.Load(1);
            Assert.That(screens.Screen.Progress, Is.EqualTo(0.25f));

            progress.Load(3);
            Assert.That(screens.Screen.Progress, Is.EqualTo(1f));
        }

        [Test]
        public void TheLoadingBar_StopsWhereItWas_WhenTheGameSceneFails()
        {
            flow.Run();
            progress.Load(2);

            scenes.Gate.Fail();
            progress.Load(1);

            Assert.That(screens.Screen.Progress, Is.EqualTo(0.5f));
        }

        [Test]
        public void TheLoadingScreenStaysForItsMinimumTime_EvenWhenTheGameSceneIsPlayableSooner()
        {
            timer.Pending = true;
            flow.Run();

            scenes.Gate.Open();

            Assert.That(timer.Requested, Is.EqualTo(TimeSpan.FromSeconds(1)));
            Assert.That(session.Ended, Is.False);
            Assert.That(log, Has.None.EqualTo("unload Loading"));

            timer.Elapse();

            Assert.That(log, Has.Member("unload Loading"));
        }

        [Test]
        public void TheMinimumTime_CountsFromWhenTheLoadingScreenShows()
        {
            timer.Pending = true;
            flow.Run();
            timer.Elapse();

            scenes.Gate.Open();

            Assert.That(log, Has.Member("unload Loading"));
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
            var signal = new GameReadySignal(session, new SceneGate(progress), scenes, "Game");

            signal.LoadAsync(CancellationToken.None).Forget();

            Assert.That(log, Is.EqualTo(new[] { "activate Game" }));
        }

        [Test]
        public void GameScene_OpensTheGate_WhenItsSessionRuns()
        {
            var gate = new SceneGate(progress);
            var signal = new GameReadySignal(session, gate, scenes, "Game");

            signal.Run();

            Assert.That(Answer(gate), Is.True);
        }

        [Test]
        public void GameScene_MarksTheGateFailed_WhenItsSessionFails()
        {
            var gate = new SceneGate(progress);
            var signal = new GameReadySignal(session, gate, scenes, "Game");

            session.Raise(SessionState.Failed);

            Assert.That(Answer(gate), Is.False);
            Assert.That(signal, Is.Not.Null);
        }

        [Test]
        public void GameScene_GateStaysUnanswered_WhileItsSessionIsStillLoading()
        {
            var gate = new SceneGate(progress);
            var signal = new GameReadySignal(session, gate, scenes, "Game");

            session.Raise(SessionState.Loading);

            Assert.That(gate.WaitAsync().Status, Is.EqualTo(UniTaskStatus.Pending));
            Assert.That(signal, Is.Not.Null);
        }

        [Test]
        public void CheatLoadError_ShowsTheLoadingScreenStoppedAt40Percent_WithTheErrorNamingWallet()
        {
            var cheat = new LoadErrorCheat(screens,
                new SceneFlowSettings("Loading", "Game", FailedMessage, "Lỗi tải: {0}", TimeSpan.FromSeconds(1)));

            cheat.Run();

            Assert.That(screens.Screen.Progress, Is.EqualTo(0.4f));
            Assert.That(screens.Screen.Error, Is.EqualTo("Lỗi tải: Wallet"));
            Assert.That(log, Is.EqualTo(new[] { "screen shown" }), "the cheat must not load the Game scene");
        }

        [Test]
        public void CheatLoadError_ClosesItsScreen_WhenTheLoadingSessionEnds()
        {
            var cheat = new LoadErrorCheat(screens,
                new SceneFlowSettings("Loading", "Game", FailedMessage, "Lỗi tải: {0}", TimeSpan.FromSeconds(1)));
            cheat.Run();

            cheat.DisposeAsync().Forget();

            Assert.That(screens.Screen.Closed, Is.True);
        }

        [Test]
        public void LoadingInstallerAsset_RunsTheRealFlow_NotACheatState()
        {
            // The shared asset must not ship with a cheat switched on: Play would stop on the error screen.
            var installer = new SerializedObject(LoadInstaller("SceneFlowLoadingInstaller"));

            Assert.That(installer.FindProperty("cheat").intValue, Is.EqualTo((int)SceneFlowCheatState.None));
        }

        [Test]
        public void LoadingScreenPrefab_WhenAnErrorIsShown_KeepsTheGameNameAndBar_AndShowsTheError()
        {
            var instance = InstantiateLoadingScreen();
            try
            {
                var title = instance.transform.Find("Title");
                var bar = instance.transform.Find("ProgressBar");
                var error = instance.transform.Find("Error");
                Assert.That(title, Is.Not.Null, "LoadingScreen.prefab has no child named Title");
                Assert.That(bar, Is.Not.Null, "LoadingScreen.prefab has no child named ProgressBar");
                Assert.That(error, Is.Not.Null, "LoadingScreen.prefab has no child named Error");
                Assert.That(error.gameObject.activeSelf, Is.False, "no error should show before one happens");

                instance.GetComponentInChildren<ILoadingScreen>(true).ShowError(FailedMessage);

                Assert.That(title.gameObject.activeSelf, Is.True, "the game name should stay when an error shows");
                Assert.That(bar.gameObject.activeSelf, Is.True, "the bar should stay where it stopped");
                Assert.That(error.gameObject.activeSelf, Is.True);
                Assert.That(Serialized(error, "Text").FindProperty("m_Text").stringValue, Is.EqualTo(FailedMessage));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(instance);
            }
        }

        [Test]
        public void LoadingScreenPrefab_TheBarFillsWithTheProgressItIsGiven()
        {
            var instance = InstantiateLoadingScreen();
            try
            {
                var fill = instance.transform.Find("ProgressBar/Fill");
                Assert.That(fill, Is.Not.Null, "LoadingScreen.prefab has no ProgressBar/Fill");
                // Read through the serialized fields: this test assembly does not reference UnityEngine.UI.
                const int filled = 3;
                var image = Serialized(fill, "Image");
                Assert.That(image.FindProperty("m_Type").intValue, Is.EqualTo(filled), "the fill Image should be of type Filled");
                Assert.That(image.FindProperty("m_Sprite").objectReferenceValue, Is.Not.Null,
                    "an Image without a sprite ignores its fill amount");
                Assert.That(image.FindProperty("m_FillAmount").floatValue, Is.EqualTo(0f), "the bar should start empty");

                instance.GetComponentInChildren<ILoadingScreen>(true).ShowProgress(0.4f);

                image.Update();
                Assert.That(image.FindProperty("m_FillAmount").floatValue, Is.EqualTo(0.4f).Within(0.0001f));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(instance);
            }
        }

        private static SerializedObject Serialized(UnityEngine.Transform owner, string componentType)
        {
            var component = owner.GetComponent(componentType);
            Assert.That(component, Is.Not.Null, $"{owner.name} has no {componentType}");
            return new SerializedObject(component);
        }

        private static UnityEngine.GameObject InstantiateLoadingScreen()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(
                "Assets/Features/SceneFlow/FeatureAssets/Private/LoadingScreen.prefab");
            Assert.That(prefab, Is.Not.Null, "LoadingScreen.prefab missing");
            return UnityEngine.Object.Instantiate(prefab);
        }

        [TestCase("SceneFlowLoadingInstaller")]
        [TestCase("SceneFlowGameInstaller")]
        public void InstallerAsset_JoinsTheSceneSession(string assetName)
        {
            var installer = LoadInstaller(assetName);
            var registry = new ServiceRegistry();
            registry.Register<ISessionScene>(session);
            registry.Register<ISessionLoadProgress>(progress);

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

        private sealed class FakeProgress : ISessionLoadProgress
        {
            public FakeProgress(int participants)
            {
                ParticipantCount = participants;
            }

            public int ParticipantCount { get; }

            public int LoadedCount { get; private set; }

            public string FailedParticipant { get; private set; }

            public event Action LoadProgressChanged;

            public void Load(int more)
            {
                LoadedCount += more;
                LoadProgressChanged?.Invoke();
            }

            public void Fail(string participant)
            {
                FailedParticipant = participant;
                LoadProgressChanged?.Invoke();
            }
        }

        private sealed class FakeTimer : ITimer
        {
            private readonly UniTaskCompletionSource elapsed = new UniTaskCompletionSource();

            /// <summary>False: every delay is already over. True: delays wait for Elapse.</summary>
            public bool Pending;

            public TimeSpan? Requested { get; private set; }

            public UniTask DelayAsync(TimeSpan delay)
            {
                Requested = delay;
                return Pending ? elapsed.Task : UniTask.CompletedTask;
            }

            public void Elapse()
            {
                elapsed.TrySetResult();
            }
        }

        private sealed class FakeScreen : ILoadingScreen
        {
            public string Error { get; private set; }

            public bool Closed { get; private set; }

            public float? Progress { get; private set; }

            public void ShowProgress(float fraction)
            {
                Progress = fraction;
            }

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
