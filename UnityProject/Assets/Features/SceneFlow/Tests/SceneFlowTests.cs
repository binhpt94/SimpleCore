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
        private static readonly TimeSpan MinTime = TimeSpan.FromSeconds(1);
        private static readonly TimeSpan SlowTime = TimeSpan.FromSeconds(5);

        private List<string> log;
        private FakeLog flowLog;
        private FakeSession session;
        private FakeScenes scenes;
        private FakeScreens screens;
        private FakeProgress progress;
        private FakeTimer timer;
        private SceneSwitcher switcher;

        [SetUp]
        public void SetUp()
        {
            log = new List<string>();
            flowLog = new FakeLog();
            session = new FakeSession(log);
            progress = new FakeProgress(4);
            scenes = new FakeScenes(log) { Gate = new SceneGate(progress) };
            screens = new FakeScreens(log);
            timer = new FakeTimer();
            switcher = new SceneSwitcher("Loading", scenes, screens, timer, Settings(), flowLog);
        }

        [TearDown]
        public void TearDown()
        {
            screens.Screen?.DestroyRoot();
        }

        private static SceneFlowSettings Settings()
        {
            return new SceneFlowSettings("Loading", "Game", FailedMessage, "Lỗi tải: {0}", MinTime, SlowTime);
        }

        private bool RequestGame()
        {
            return switcher.Request("Game", session.EndAsync);
        }

        // --- One switch: steps and cover (switch-rules) ---

        [Test]
        public void ASwitch_ShowsTheCover_EndsTheOldScene_LoadsTheNewOne_ThenRemovesTheCoverOnceItRuns()
        {
            Assert.That(RequestGame(), Is.True);
            Assert.That(log, Is.EqualTo(new[] { "screen shown", "end session", "load Game" }));

            scenes.Gate.Open();
            timer.Elapse(MinTime);

            Assert.That(log, Is.EqualTo(new[] { "screen shown", "end session", "load Game", "screen closed" }));
            Assert.That(switcher.CurrentScene, Is.EqualTo("Game"));
            Assert.That(switcher.IsSwitching, Is.False);
        }

        [Test]
        public void TheNewScene_StartsLoadingOnlyOnceTheOldSceneHasEnded()
        {
            session.EndPending = true;

            RequestGame();

            Assert.That(log, Is.EqualTo(new[] { "screen shown", "end session" }), "no load while the old scene is ending");

            session.FinishEnd();

            Assert.That(log, Is.EqualTo(new[] { "screen shown", "end session", "session ended", "load Game" }));
        }

        [Test]
        public void ARequestWhileTheOldSceneIsStillEnding_IsIgnored_WithAWarning()
        {
            session.EndPending = true;
            RequestGame();
            log.Clear();

            var second = switcher.Request("Loading", session.EndAsync);

            Assert.That(second, Is.False);
            Assert.That(flowLog.Warnings, Has.Count.EqualTo(1));
            Assert.That(log, Is.Empty, "no second cover, no second session end");

            session.FinishEnd();
            Assert.That(log, Is.EqualTo(new[] { "session ended", "load Game" }), "only the first switch goes on");
        }

        [Test]
        public void TheCover_ShowsAtOnce_AndStaysUntilTheNewSceneHasRun()
        {
            RequestGame();

            Assert.That(screens.Screen, Is.Not.Null, "the cover must show in the same frame as the request");
            Assert.That(screens.Screen.Progress, Is.EqualTo(0f));

            timer.Elapse(MinTime);
            Assert.That(screens.Screen.Closed, Is.False, "the new scene has not run yet");

            scenes.Gate.Open();
            Assert.That(screens.Screen.Closed, Is.True);
        }

        [Test]
        public void WhenEndingTheOldSceneThrows_TheErrorIsLogged_AndTheNewSceneStillLoads()
        {
            switcher.Request("Game", () => throw new InvalidOperationException("Wallet could not let go"));

            Assert.That(flowLog.Exceptions, Has.Count.EqualTo(1));
            Assert.That(log, Has.Member("load Game"));
        }

        // --- Refused requests (switch-rules) ---

        [Test]
        public void ARequestForAnUnknownScene_IsRefused_WithAnErrorNamingIt_AndTheCurrentSceneKeepsRunning()
        {
            var accepted = switcher.Request("Shop", session.EndAsync);

            Assert.That(accepted, Is.False);
            Assert.That(flowLog.Errors, Has.Count.EqualTo(1));
            Assert.That(flowLog.Errors[0], Does.Contain("'Shop'"));
            Assert.That(log, Is.Empty, "no cover, no session end, no load");
            Assert.That(switcher.CurrentScene, Is.EqualTo("Loading"));
        }

        [Test]
        public void ARequestWhileASwitchIsGoing_IsIgnored_WithAWarning_AndNotQueued()
        {
            RequestGame();
            log.Clear();

            var second = switcher.Request("Loading", session.EndAsync);

            Assert.That(second, Is.False);
            Assert.That(flowLog.Warnings, Has.Count.EqualTo(1));
            Assert.That(log, Is.Empty);

            scenes.Gate.Open();
            timer.Elapse(MinTime);
            Assert.That(log, Is.EqualTo(new[] { "screen closed" }), "the ignored request must not run afterwards");
        }

        [Test]
        public void TwoRequestsInTheSameFrame_OnlyTheFirstIsTaken()
        {
            var first = RequestGame();
            var second = RequestGame();

            Assert.That(first, Is.True);
            Assert.That(second, Is.False);
            Assert.That(log.FindAll(line => line == "load Game"), Has.Count.EqualTo(1));
        }

        [Test]
        public void ARequestForTheSceneAlreadyRunning_IsIgnored_WithoutReloading()
        {
            var accepted = switcher.Request("Loading", session.EndAsync);

            Assert.That(accepted, Is.False);
            Assert.That(log, Is.Empty);
            Assert.That(flowLog.Errors, Is.Empty);
        }

        [Test]
        public void OnceASwitchHasEnded_ANewRequestIsTaken()
        {
            RequestGame();
            scenes.Gate.Open();
            timer.Elapse(MinTime);

            Assert.That(switcher.Request("Loading", session.EndAsync), Is.True);
        }

        // --- Failure ---

        [Test]
        public void WhenOneFeatureOfTheNewSceneFailsToLoad_TheCoverStays_WithAnErrorNamingThatFeature()
        {
            RequestGame();

            progress.Fail("Wallet");
            scenes.Gate.Fail();
            timer.Elapse(MinTime);

            Assert.That(screens.Screen.Error, Is.EqualTo("Lỗi tải: Wallet"));
            Assert.That(screens.Screen.Closed, Is.False);
            Assert.That(switcher.IsSwitching, Is.True, "no step after the failure, no new switch");
            Assert.That(log, Has.None.EqualTo("load Loading"), "there is no way back to the old scene");
        }

        [Test]
        public void WhenTheFailedFeatureIsNamedJustAfterTheFailure_TheErrorStillNamesIt()
        {
            RequestGame();

            scenes.Gate.Fail();
            progress.Fail("Wallet");

            Assert.That(screens.Screen.Error, Is.EqualTo("Lỗi tải: Wallet"));
        }

        [Test]
        public void WhenTheNewSceneFailsWithoutNamingAFeature_ThePlayerSeesTheGeneralError()
        {
            RequestGame();

            scenes.Gate.Fail();

            Assert.That(screens.Screen.Error, Is.EqualTo(FailedMessage));
        }

        [Test]
        public void WhenTheNewSceneHasNoGate_ThePlayerSeesTheError_AndTheCauseIsLogged()
        {
            scenes.Gate = null;

            RequestGame();

            Assert.That(screens.Screen.Error, Is.EqualTo(FailedMessage));
            Assert.That(flowLog.Exceptions, Has.Count.EqualTo(1));
            Assert.That(flowLog.Exceptions[0].Message, Does.Contain("'Game'").And.Contain("gate"));
            Assert.That(screens.Screen.Closed, Is.False);
        }

        [Test]
        public void WhenTheNewSceneCannotBeLoaded_ThePlayerSeesTheError_AndTheCauseIsLogged()
        {
            scenes.LoadError = new InvalidOperationException("not in Build Settings");

            RequestGame();

            Assert.That(screens.Screen.Error, Is.EqualTo(FailedMessage));
            Assert.That(flowLog.Exceptions, Has.Count.EqualTo(1));
            Assert.That(flowLog.Exceptions[0].Message, Does.Contain("not in Build Settings"));
        }

        // --- Progress bar ---

        [Test]
        public void TheBar_StartsEmpty_AndFillsAsTheNewScenesFeaturesFinishLoading()
        {
            RequestGame();
            Assert.That(screens.Screen.Progress, Is.EqualTo(0f));

            progress.Load(1);
            Assert.That(screens.Screen.Progress, Is.EqualTo(0.25f));

            progress.Load(3);
            Assert.That(screens.Screen.Progress, Is.EqualTo(1f));
        }

        [Test]
        public void TheBar_StopsWhereItWas_WhenTheNewSceneFails()
        {
            RequestGame();
            progress.Load(2);

            scenes.Gate.Fail();
            progress.Load(1);

            Assert.That(screens.Screen.Progress, Is.EqualTo(0.5f));
        }

        // --- Timings (boot-loading) ---

        [Test]
        public void TheLoadingScreenStaysForItsMinimumTime_EvenWhenTheGameSceneIsReadySooner()
        {
            RequestGame();

            scenes.Gate.Open();

            Assert.That(timer.Requested, Has.Member(MinTime));
            Assert.That(screens.Screen.Closed, Is.False);

            timer.Elapse(MinTime);

            Assert.That(screens.Screen.Closed, Is.True);
        }

        [Test]
        public void TheMinimumTime_CountsFromWhenTheLoadingScreenShows()
        {
            RequestGame();
            timer.Elapse(MinTime);

            scenes.Gate.Open();

            Assert.That(screens.Screen.Closed, Is.True, "loading took longer than the minimum: no extra wait");
        }

        [Test]
        public void WhenTheGameSceneWasReadyBeforeAnyoneAsked_TheSwitchStillEnds()
        {
            scenes.Gate.Open();

            RequestGame();
            timer.Elapse(MinTime);

            Assert.That(screens.Screen.Closed, Is.True);
        }

        [Test]
        public void ASwitchStillGoingAfterFiveSeconds_LogsAPerformanceWarning_AndKeepsGoing()
        {
            RequestGame();
            timer.Elapse(MinTime);

            timer.Elapse(SlowTime);

            Assert.That(flowLog.Warnings, Has.Count.EqualTo(1));
            Assert.That(flowLog.Warnings[0], Does.Contain("'Game'"));
            Assert.That(screens.Screen.Error, Is.Null, "slow is not an error");

            scenes.Gate.Open();
            Assert.That(screens.Screen.Closed, Is.True);
        }

        [Test]
        public void ASwitchDoneWithinFiveSeconds_LogsNoWarning()
        {
            RequestGame();
            scenes.Gate.Open();
            timer.Elapse(MinTime);

            timer.Elapse(SlowTime);

            Assert.That(flowLog.Warnings, Is.Empty);
        }

        // --- Loading scene participant ---

        [Test]
        public void WhenTheLoadingSceneRuns_ItAsksToSwitchToTheGameScene()
        {
            var flow = new LoadingToGameFlow(session, switcher, "Game");

            flow.Run();

            Assert.That(log, Is.EqualTo(new[] { "screen shown", "end session", "load Game" }));
        }

        [Test]
        public void WhenTheLoadingSessionEnds_TheLoadingScreenStays_BecauseItIsTheSwitchCover()
        {
            var flow = new LoadingToGameFlow(session, switcher, "Game");
            flow.Run();

            flow.DisposeAsync().Forget();

            Assert.That(screens.Screen.Closed, Is.False);
        }

        // --- Game scene side ---

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
        public void GameScene_StopsListening_WhenItsSessionEnds()
        {
            var gate = new SceneGate(progress);
            var signal = new GameReadySignal(session, gate, scenes, "Game");

            signal.DisposeAsync().Forget();
            session.Raise(SessionState.Failed);

            Assert.That(gate.WaitAsync().Status, Is.EqualTo(UniTaskStatus.Pending));
        }

        // --- IGameSceneReady (screen routing, PROTOTYPE) ---

        [Test]
        public void GameScene_IsNotReady_WhileItsSessionIsStillLoading()
        {
            var signal = new GameReadySignal(session, new SceneGate(progress), scenes, "Game");
            var raised = 0;
            signal.Ready += () => raised++;

            signal.LoadAsync(CancellationToken.None).Forget();

            Assert.That(signal.IsReady, Is.False);
            Assert.That(raised, Is.EqualTo(0));
        }

        [Test]
        public void GameScene_RaisesReadyOnce_WhenItsSessionRunsAndTheGateOpens()
        {
            var gate = new SceneGate(progress);
            var signal = new GameReadySignal(session, gate, scenes, "Game");
            var raised = 0;
            var gateOpenWhenRaised = false;
            signal.Ready += () =>
            {
                raised++;
                gateOpenWhenRaised = gate.WaitAsync().Status == UniTaskStatus.Succeeded;
            };

            signal.Run();
            signal.Run();

            Assert.That(raised, Is.EqualTo(1));
            Assert.That(signal.IsReady, Is.True);
            Assert.That(gateOpenWhenRaised, Is.True, "Ready comes with the gate opening, not before it");
        }

        [Test]
        public void GameScene_NeverRaisesReady_WhenItsSessionFails()
        {
            var signal = new GameReadySignal(session, new SceneGate(progress), scenes, "Game");
            var raised = 0;
            signal.Ready += () => raised++;

            session.Raise(SessionState.Failed);

            Assert.That(signal.IsReady, Is.False);
            Assert.That(raised, Is.EqualTo(0));
        }

        [Test]
        public void GameScene_DropsItsReadyListeners_WhenItsSessionEnds()
        {
            var signal = new GameReadySignal(session, new SceneGate(progress), scenes, "Game");
            var raised = 0;
            signal.Ready += () => raised++;

            signal.DisposeAsync().Forget();
            signal.Run();

            Assert.That(raised, Is.EqualTo(0));
        }

        // --- Cheat (feature-scene-flow/cheat) ---

        [Test]
        public void CheatLoadError_ShowsTheLoadingScreenStoppedAt40Percent_WithTheErrorNamingWallet()
        {
            var cheat = new LoadErrorCheat(screens, Settings());

            cheat.Run();

            Assert.That(screens.Screen.Progress, Is.EqualTo(0.4f));
            Assert.That(screens.Screen.Error, Is.EqualTo("Lỗi tải: Wallet"));
            Assert.That(log, Is.EqualTo(new[] { "screen shown" }), "the cheat must not load the Game scene");
        }

        [Test]
        public void CheatLoadError_ClosesItsScreen_WhenTheLoadingSessionEnds()
        {
            var cheat = new LoadErrorCheat(screens, Settings());
            cheat.Run();

            cheat.DisposeAsync().Forget();

            Assert.That(screens.Screen.Closed, Is.True);
        }

        // --- Session clean-up count (proposal 2026-11-25-session-usage-tracking) ---

        [Test]
        public void CheatLoadError_ReportsItsScreenAsAHeldView_AndNothingOnceTheSessionEnds()
        {
            var cheat = new LoadErrorCheat(screens, Settings());
            var usage = (ISessionParticipantUsage)cheat;

            cheat.Run();

            Assert.That(usage.OpensViewsOnRun, Is.True);
            Assert.That(usage.HeldViews, Is.EqualTo(1));
            Assert.That(usage.RunningJobs, Is.EqualTo(0));

            cheat.DisposeAsync().Forget();

            Assert.That(usage.HeldViews, Is.EqualTo(0));
            Assert.That(usage.RunningJobs, Is.EqualTo(0));
        }

        [Test]
        public void LoadingScene_DoesNotCountTheSwitchCover_AsAHeldView()
        {
            // The cover outlives the Loading session on purpose; counting it would mark the clean-up as failed.
            var flow = new LoadingToGameFlow(session, switcher, "Game");
            var usage = (ISessionParticipantUsage)flow;

            flow.Run();

            Assert.That(screens.Screen, Is.Not.Null, "the switch cover is shown");
            Assert.That(usage.HeldViews, Is.EqualTo(0));
            Assert.That(usage.RunningJobs, Is.EqualTo(0));

            flow.DisposeAsync().Forget();

            Assert.That(usage.HeldViews, Is.EqualTo(0));
            Assert.That(usage.RunningJobs, Is.EqualTo(0));
        }

        [Test]
        public void GameScene_HoldsNoViewAndNoJob_BeforeOrAfterItsSessionEnds()
        {
            var signal = new GameReadySignal(session, new SceneGate(progress), scenes, "Game");
            var usage = (ISessionParticipantUsage)signal;

            signal.LoadAsync(CancellationToken.None).Forget();
            signal.Run();

            Assert.That(usage.HeldViews, Is.EqualTo(0));
            Assert.That(usage.RunningJobs, Is.EqualTo(0));

            signal.DisposeAsync().Forget();

            Assert.That(usage.HeldViews, Is.EqualTo(0));
            Assert.That(usage.RunningJobs, Is.EqualTo(0));
        }

        [Test]
        public void LoadingInstallerAsset_RunsTheRealFlow_NotACheatState()
        {
            // The shared asset must not ship with a cheat switched on: Play would stop on the error screen.
            var installer = new SerializedObject(LoadInstaller("SceneFlowLoadingInstaller"));

            Assert.That(installer.FindProperty("cheat").intValue, Is.EqualTo((int)SceneFlowCheatState.None));
        }

        // --- Assets ---

        [Test]
        public void ConfigAsset_HoldsTheDesignTimings_AndBothScenes()
        {
            var config = new SerializedObject(AssetDatabase.LoadAssetAtPath<SceneFlowConfig>(
                "Assets/Features/SceneFlow/FeatureAssets/Private/SceneFlowConfig.asset"));

            Assert.That(config.FindProperty("loadingScene").stringValue, Is.EqualTo("Loading"));
            Assert.That(config.FindProperty("gameScene").stringValue, Is.EqualTo("Game"));
            Assert.That(config.FindProperty("minLoadingScreenSeconds").floatValue, Is.EqualTo(1f));
            Assert.That(config.FindProperty("slowSwitchWarningSeconds").floatValue, Is.EqualTo(5f));
            Assert.That(config.FindProperty("featureFailedMessage").stringValue, Does.Contain("{0}"));
        }

        [Test]
        public void LoadingScreenPrefab_ShowsOnlyTheGameNameAndTheBar_WithNoButton()
        {
            var instance = InstantiateLoadingScreen();
            try
            {
                Assert.That(instance.transform.Find("Title"), Is.Not.Null, "LoadingScreen.prefab has no child named Title");
                Assert.That(Serialized(instance.transform.Find("Title"), "Text").FindProperty("m_Text").stringValue,
                    Is.Not.Empty, "the game name");
                Assert.That(instance.transform.Find("ProgressBar"), Is.Not.Null);
                Assert.That(instance.transform.Find("Error").gameObject.activeSelf, Is.False,
                    "no error should show before one happens");

                foreach (var component in instance.GetComponentsInChildren<UnityEngine.Component>(true))
                {
                    if (component == null) continue; // missing script: not a button either
                    Assert.That(component.GetType().Name, Is.Not.EqualTo("Button"), $"{component.name} is a button");
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(instance);
            }
        }

        [Test]
        public void LoadingScreenPrefab_CoversTheWholeScreen_AndCatchesEveryTap()
        {
            var instance = InstantiateLoadingScreen();
            try
            {
                var background = instance.transform.Find("Background");
                Assert.That(background, Is.Not.Null, "LoadingScreen.prefab has no Background");
                Assert.That(instance.GetComponent("GraphicRaycaster"), Is.Not.Null, "taps must reach the cover");

                var rect = new SerializedObject(background.GetComponent<UnityEngine.RectTransform>());
                Assert.That(rect.FindProperty("m_AnchorMin").vector2Value, Is.EqualTo(UnityEngine.Vector2.zero));
                Assert.That(rect.FindProperty("m_AnchorMax").vector2Value, Is.EqualTo(UnityEngine.Vector2.one));

                var image = Serialized(background, "Image");
                Assert.That(image.FindProperty("m_Color").colorValue.a, Is.EqualTo(1f), "the cover is not see-through");
                Assert.That(image.FindProperty("m_RaycastTarget").boolValue, Is.True, "taps must stop on the cover");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(instance);
            }
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
        public void GameInstallerAsset_RegistersTheGameSceneReadySignal_ThatJoinsTheSession()
        {
            var installer = LoadInstaller("SceneFlowGameInstaller");
            var registry = new ServiceRegistry();
            registry.Register<ISessionScene>(session);
            registry.Register<ISessionLoadProgress>(progress);

            installer.Install(registry);

            Assert.That(registry.TryResolve<IGameSceneReady>(out var ready), Is.True);
            Assert.That(session.Joined, Has.Member(ready), "the ready signal is the participant whose Run opens the gate");
            Assert.That(ready.IsReady, Is.False);
        }

        [Test]
        public void LoadingInstallerAsset_DoesNotRegisterTheGameSceneReadySignal()
        {
            var installer = LoadInstaller("SceneFlowLoadingInstaller");
            var registry = new ServiceRegistry();
            registry.Register<ISessionScene>(session);
            registry.Register<ISessionLoadProgress>(progress);

            installer.Install(registry);

            Assert.That(registry.TryResolve<IGameSceneReady>(out _), Is.False);
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

            public SessionState State { get; private set; } = SessionState.Init;

            public event Action<SessionState> StateChanged;

            public void Join(ISessionParticipant participant)
            {
                Joined.Add(participant);
            }

            /// <summary>When set, EndAsync stays pending until the test finishes it with FinishEnd.</summary>
            public bool EndPending;

            private readonly UniTaskCompletionSource ended = new UniTaskCompletionSource();

            public UniTask EndAsync()
            {
                log.Add("end session");
                return EndPending ? ended.Task : UniTask.CompletedTask;
            }

            public void FinishEnd()
            {
                log.Add("session ended");
                ended.TrySetResult();
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

            public UniTask LoadReplacingAsync(string sceneName)
            {
                log.Add("load " + sceneName);
                if (LoadError != null) throw LoadError;
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

        private sealed class FakeLog : ISceneFlowLog
        {
            public readonly List<string> Warnings = new List<string>();
            public readonly List<string> Errors = new List<string>();
            public readonly List<Exception> Exceptions = new List<Exception>();

            public void Warning(string message)
            {
                Warnings.Add(message);
            }

            public void Error(string message)
            {
                Errors.Add(message);
            }

            public void Exception(Exception exception)
            {
                Exceptions.Add(exception);
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
                Screen = new FakeScreen(log);
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

        /// <summary>Every delay waits until the test elapses that length by hand.</summary>
        private sealed class FakeTimer : ITimer
        {
            private readonly Dictionary<TimeSpan, UniTaskCompletionSource> delays =
                new Dictionary<TimeSpan, UniTaskCompletionSource>();

            public readonly List<TimeSpan> Requested = new List<TimeSpan>();

            public UniTask DelayAsync(TimeSpan delay)
            {
                Requested.Add(delay);
                return Source(delay).Task;
            }

            public void Elapse(TimeSpan delay)
            {
                Source(delay).TrySetResult();
            }

            private UniTaskCompletionSource Source(TimeSpan delay)
            {
                if (!delays.TryGetValue(delay, out var source))
                {
                    source = new UniTaskCompletionSource();
                    delays.Add(delay, source);
                }

                return source;
            }
        }

        private sealed class FakeScreen : ILoadingScreen
        {
            private readonly List<string> log;
            private UnityEngine.GameObject root;

            public FakeScreen(List<string> log)
            {
                this.log = log;
            }

            /// <summary>Made only when asked for (the view count asks), hidden, and destroyed by <see cref="DestroyRoot"/>.</summary>
            public UnityEngine.Object Root
            {
                get
                {
                    if (root == null)
                    {
                        root = new UnityEngine.GameObject("fake loading screen")
                        {
                            hideFlags = UnityEngine.HideFlags.HideAndDontSave
                        };
                    }

                    return root;
                }
            }

            public void DestroyRoot()
            {
                if (root != null) UnityEngine.Object.DestroyImmediate(root);
                root = null;
            }

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
                log.Add("screen closed");
            }
        }
    }
}
