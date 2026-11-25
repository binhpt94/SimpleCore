using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Game.Core;
using NUnit.Framework;
using UnityEditor;
using SessionState = Game.Core.SessionState;

namespace Game.Services.SessionScene.Tests
{
    public class SessionSceneLogicTests
    {
        private static readonly TimeSpan Limit = TimeSpan.FromSeconds(10);

        private List<string> log;
        private List<Exception> errors;
        private FakeTimer timer;
        private SessionSceneLogic session;

        [SetUp]
        public void SetUp()
        {
            log = new List<string>();
            errors = new List<Exception>();
            timer = new FakeTimer();
            session = new SessionSceneLogic(errors.Add, timer, Limit);
        }

        [Test]
        public void NewSession_IsInInit()
        {
            Assert.That(session.State, Is.EqualTo(SessionState.Init));
        }

        [Test]
        public void WhenEveryoneLoaded_ParticipantsRunInJoinOrder_AndSessionIsRunning()
        {
            Join("a");
            Join("b");

            session.BeginAsync().Forget();

            Assert.That(session.State, Is.EqualTo(SessionState.Running));
            Assert.That(log, Is.EqualTo(new[] { "load a", "load b", "run a", "run b" }));
            Assert.That(errors, Is.Empty);
        }

        [Test]
        public void ParticipantsLoadOneAtATime_TheNextStartsOnlyAfterThePreviousFinished()
        {
            var first = Join("first");
            first.Load = new UniTaskCompletionSource();
            Join("second");

            session.BeginAsync().Forget();

            Assert.That(log, Is.EqualTo(new[] { "load first" }));

            first.Load.TrySetResult();

            Assert.That(log, Is.EqualTo(new[] { "load first", "load second", "run first", "run second" }));
        }

        [Test]
        public void SessionStaysLoading_AndNobodyRuns_UntilTheLastParticipantLoaded()
        {
            Join("fast");
            var slow = Join("slow");
            slow.Load = new UniTaskCompletionSource();

            session.BeginAsync().Forget();

            Assert.That(session.State, Is.EqualTo(SessionState.Loading));
            Assert.That(log, Has.None.StartsWith("run"));

            slow.Load.TrySetResult();

            Assert.That(session.State, Is.EqualTo(SessionState.Running));
            Assert.That(log, Has.Member("run fast").And.Member("run slow"));
        }

        [Test]
        public void JoiningAfterTheSessionBegan_IsRefused()
        {
            session.BeginAsync().Forget();

            Assert.Throws<InvalidOperationException>(() => session.Join(new FakeParticipant("late", log)));
        }

        [Test]
        public void JoiningTwice_IsRefused()
        {
            var once = Join("once");

            Assert.Throws<InvalidOperationException>(() => session.Join(once));
        }

        [Test]
        public void WhenAParticipantFailsToLoad_LaterOnesNeverLoad_ThoseReachedCleanUpInReverse_SessionFails()
        {
            Join("ok");
            Join("broken").LoadError = new InvalidOperationException("save file is corrupt");
            Join("after");

            session.BeginAsync().Forget();

            Assert.That(session.State, Is.EqualTo(SessionState.Failed));
            Assert.That(log, Is.EqualTo(new[] { "load ok", "load broken", "dispose broken", "dispose ok" }));
            Assert.That(errors, Has.Count.EqualTo(1));
            Assert.That(errors[0].Message, Does.Contain("FakeParticipant").And.Contain("save file is corrupt"));
        }

        [Test]
        public void WhenAParticipantFailsToLoad_ItsFeatureNameIsReportedForTheLoadingScreen()
        {
            Join("broken").LoadError = new InvalidOperationException("no save");

            session.BeginAsync().Forget();

            // The fake lives in Game.Services.SessionScene.Tests, so its feature is SessionScene.
            Assert.That(session.FailedParticipant, Is.EqualTo("SessionScene"));
            Assert.That(errors[0].Message, Does.Contain("SessionScene"));
        }

        [Test]
        public void WhenAParticipantGivesUpLoadingOnItsOwn_SessionFails()
        {
            var quitter = Join("quitter");
            quitter.LoadError = new OperationCanceledException();

            session.BeginAsync().Forget();

            Assert.That(session.State, Is.EqualTo(SessionState.Failed));
            Assert.That(log, Has.None.StartsWith("run"));
        }

        [Test]
        public void EachParticipantGetsTheConfiguredTimeLimit()
        {
            Join("a");
            Join("b");

            session.BeginAsync().Forget();

            Assert.That(timer.Requested, Is.EqualTo(new[] { Limit, Limit }));
        }

        [Test]
        public void WhenAParticipantLoadsLongerThanTheLimit_ItIsTold_ToStop_LaterOnesNeverLoad_SessionFails()
        {
            Join("ok");
            var slow = Join("slow");
            slow.Load = new UniTaskCompletionSource();
            Join("after");
            session.BeginAsync().Forget();

            timer.Expire();

            Assert.That(session.State, Is.EqualTo(SessionState.Failed));
            Assert.That(slow.LoadWasCancelled, Is.True);
            Assert.That(session.FailedParticipant, Is.EqualTo("SessionScene"));
            Assert.That(log, Is.EqualTo(new[] { "load ok", "load slow", "dispose slow", "dispose ok" }));
            Assert.That(errors, Has.Count.EqualTo(1));
            Assert.That(errors[0].Message, Does.Contain("10 seconds"));
        }

        [Test]
        public void WhenAParticipantFailsToRun_SessionFails_AndEveryoneCleansUp()
        {
            Join("a").RunError = new InvalidOperationException("prefab missing");
            Join("b");

            session.BeginAsync().Forget();

            Assert.That(session.State, Is.EqualTo(SessionState.Failed));
            Assert.That(log, Has.None.EqualTo("run b"));
            Assert.That(log, Has.Member("dispose a").And.Member("dispose b"));
            Assert.That(errors, Has.Count.EqualTo(1));
            Assert.That(session.FailedParticipant, Is.Null);
        }

        [Test]
        public void EndingARunningSession_CleansUpInReverseJoinOrder_AndSessionIsDisposed()
        {
            Join("a");
            Join("b");
            session.BeginAsync().Forget();
            log.Clear();

            var end = session.EndAsync();

            Assert.That(session.State, Is.EqualTo(SessionState.Disposed));
            Assert.That(log, Is.EqualTo(new[] { "dispose b", "dispose a" }));
            Assert.That(end.Status, Is.EqualTo(UniTaskStatus.Succeeded));
        }

        [Test]
        public void EndingTwice_CleansUpOnlyOnce()
        {
            Join("a");
            session.BeginAsync().Forget();

            session.EndAsync().Forget();
            session.EndAsync().Forget();

            Assert.That(log.FindAll(entry => entry == "dispose a"), Has.Count.EqualTo(1));
        }

        [Test]
        public void EndingWhileLoading_CancelsTheLoad_LaterOnesNeverLoad_OnlyThoseReachedCleanUp()
        {
            var slow = Join("slow");
            slow.Load = new UniTaskCompletionSource();
            Join("after");
            session.BeginAsync().Forget();

            session.EndAsync().Forget();

            Assert.That(slow.LoadWasCancelled, Is.True);
            Assert.That(session.State, Is.EqualTo(SessionState.Disposed));
            Assert.That(log, Is.EqualTo(new[] { "load slow", "dispose slow" }));
            Assert.That(errors, Is.Empty);
            Assert.That(session.FailedParticipant, Is.Null);
        }

        [Test]
        public void EndingWhileLoading_WaitsForAParticipantThatIgnoresTheCancel_AtMostUntilTheLimit()
        {
            var stubborn = Join("stubborn");
            stubborn.Load = new UniTaskCompletionSource();
            stubborn.IgnoresCancellation = true;
            session.BeginAsync().Forget();

            var end = session.EndAsync();

            Assert.That(session.State, Is.EqualTo(SessionState.Loading));
            Assert.That(end.Status, Is.EqualTo(UniTaskStatus.Pending));

            timer.Expire();

            Assert.That(session.State, Is.EqualTo(SessionState.Disposed));
            Assert.That(log, Is.EqualTo(new[] { "load stubborn", "dispose stubborn" }));
            Assert.That(errors, Is.Empty);
        }

        [Test]
        public void EndingFromInsideRun_StillLetsEveryParticipantRun_ThenDisposes()
        {
            Join("a").OnRun = () => session.EndAsync().Forget();
            Join("b");

            session.BeginAsync().Forget();

            Assert.That(session.State, Is.EqualTo(SessionState.Disposed));
            Assert.That(log, Is.EqualTo(new[] { "load a", "load b", "run a", "run b", "dispose b", "dispose a" }));
        }

        [Test]
        public void EndingBeforeTheSessionBegan_CleansUp_AndASubsequentBeginDoesNothing()
        {
            Join("a");

            session.EndAsync().Forget();
            session.BeginAsync().Forget();

            Assert.That(session.State, Is.EqualTo(SessionState.Disposed));
            Assert.That(log, Is.EqualTo(new[] { "dispose a" }));
        }

        [Test]
        public void WhenOneParticipantFailsToCleanUp_TheOthersStillDo_AndSessionIsDisposed()
        {
            Join("a");
            Join("b").DisposeError = new InvalidOperationException("disk full");
            session.BeginAsync().Forget();

            session.EndAsync().Forget();

            Assert.That(session.State, Is.EqualTo(SessionState.Disposed));
            Assert.That(log, Has.Member("dispose a"));
            Assert.That(errors, Has.Count.EqualTo(1));
            Assert.That(errors[0].Message, Does.Contain("disk full"));
        }

        [Test]
        public void EndAsyncOfAFailedSession_CompletesAndTheStateStaysFailed()
        {
            Join("broken").LoadError = new InvalidOperationException("no save");
            session.BeginAsync().Forget();

            var end = session.EndAsync();

            Assert.That(end.Status, Is.EqualTo(UniTaskStatus.Succeeded));
            Assert.That(session.State, Is.EqualTo(SessionState.Failed));
        }

        [Test]
        public void StateChanged_ReportsEachStateInOrder()
        {
            var states = new List<SessionState>();
            session.StateChanged += states.Add;
            Join("a");

            session.BeginAsync().Forget();
            session.EndAsync().Forget();

            Assert.That(states, Is.EqualTo(new[]
            {
                SessionState.Loading, SessionState.Running, SessionState.Disposing, SessionState.Disposed
            }));
        }

        [Test]
        public void LoadProgress_CountsJoinedParticipants_AndRisesAsEachFinishesLoading()
        {
            Join("fast");
            var slow = Join("slow");
            slow.Load = new UniTaskCompletionSource();
            var reported = new List<int>();
            session.LoadProgressChanged += () => reported.Add(session.LoadedCount);

            Assert.That(session.ParticipantCount, Is.EqualTo(2));
            Assert.That(session.LoadedCount, Is.EqualTo(0));

            session.BeginAsync().Forget();
            Assert.That(session.LoadedCount, Is.EqualTo(1));

            slow.Load.TrySetResult();
            Assert.That(session.LoadedCount, Is.EqualTo(session.ParticipantCount));
            Assert.That(reported, Is.EqualTo(new[] { 1, 2 }));
        }

        [Test]
        public void LoadProgress_WhenAParticipantFails_DoesNotCountIt_AndReportsTheFailure()
        {
            Join("ok");
            Join("broken").LoadError = new InvalidOperationException("no save");
            var reported = new List<string>();
            session.LoadProgressChanged += () => reported.Add(session.LoadedCount + ":" + session.FailedParticipant);

            session.BeginAsync().Forget();

            Assert.That(session.LoadedCount, Is.EqualTo(1));
            Assert.That(reported, Is.EqualTo(new[] { "1:", "1:SessionScene" }));
        }

        // participant-order#edge-yeu-cau-huy-den-khi-scene-dang-tai: two already loaded, the third still
        // loading when the end is asked: the one loading is not skipped, then the others in reverse order.
        [Test]
        public void EndingWhileTheThirdIsLoading_DisposesTheOneLoadingFirst_ThenTheLoadedOnesInReverse_AndLoadsNoMore()
        {
            Join("a");
            Join("b");
            var c = Join("c");
            c.Load = new UniTaskCompletionSource();
            Join("d");
            session.BeginAsync().Forget();

            var end = session.EndAsync();

            Assert.That(end.Status, Is.EqualTo(UniTaskStatus.Succeeded));
            Assert.That(session.State, Is.EqualTo(SessionState.Disposed));
            Assert.That(log, Is.EqualTo(new[] { "load a", "load b", "load c", "dispose c", "dispose b", "dispose a" }));
            Assert.That(errors, Is.Empty);
        }

        // participant-order#run-no-wait: the run step loads nothing and is over in the same call that
        // finished the last load, so every view appears in one frame.
        [Test]
        public void RunStep_EveryoneRunsInTheSameCallThatFinishedTheLastLoad_WithoutAnyNewLoadTimer()
        {
            Join("a");
            Join("b");
            var last = Join("last");
            last.Load = new UniTaskCompletionSource();
            session.BeginAsync().Forget();
            var timersBeforeRun = timer.Requested.Count;

            last.Load.TrySetResult();

            Assert.That(session.State, Is.EqualTo(SessionState.Running));
            Assert.That(log, Is.EqualTo(new[] { "load a", "load b", "load last", "run a", "run b", "run last" }));
            Assert.That(timer.Requested.Count, Is.EqualTo(timersBeforeRun), "the run step started a new wait");
            Assert.That(timer.StillWaiting, Is.EqualTo(0));
        }

        // participant-order#edge-feature-thu-k-chay-loi: k fails to run, later ones never run, every
        // participant (all of them loaded) is cleaned up from the last back to the first, the error names k.
        [Test]
        public void WhenTheKthParticipantFailsToRun_LaterOnesNeverRun_EveryoneCleansUpFromTheLast_AndTheErrorNamesIt()
        {
            Join("a");
            Join("k").RunError = new InvalidOperationException("prefab missing");
            Join("c");

            session.BeginAsync().Forget();

            Assert.That(session.State, Is.EqualTo(SessionState.Failed));
            Assert.That(log, Is.EqualTo(new[]
            {
                "load a", "load k", "load c", "run a", "run k", "dispose c", "dispose k", "dispose a"
            }));
            Assert.That(errors, Has.Count.EqualTo(1));
            Assert.That(errors[0].Message, Does.Contain("SessionScene").And.Contain("failed to run")
                .And.Contain("prefab missing"));
        }

        // participant-order#dispose-never-stops: a participant failing to clean up is reported by name,
        // and the participants before it are still cleaned up, in reverse order.
        [Test]
        public void WhenAParticipantFailsToCleanUp_TheErrorNamesIt_AndTheOnesBeforeItStillCleanUpInReverse()
        {
            Join("a");
            Join("b").DisposeError = new InvalidOperationException("disk full");
            Join("c");
            session.BeginAsync().Forget();
            log.Clear();

            var end = session.EndAsync();

            Assert.That(end.Status, Is.EqualTo(UniTaskStatus.Succeeded));
            Assert.That(session.State, Is.EqualTo(SessionState.Disposed));
            Assert.That(log, Is.EqualTo(new[] { "dispose c", "dispose b", "dispose a" }));
            Assert.That(errors, Has.Count.EqualTo(1));
            Assert.That(errors[0].Message, Does.Contain("SessionScene").And.Contain("failed to dispose")
                .And.Contain("disk full"));
        }

        // SessionState transitions of every way a session can go.
        [Test]
        public void StateChanged_WhenAParticipantFailsToLoad_GoesLoadingThenFailed_AndEndingLeavesItFailed()
        {
            var states = new List<SessionState>();
            session.StateChanged += states.Add;
            Join("broken").LoadError = new InvalidOperationException("no save");

            session.BeginAsync().Forget();
            session.EndAsync().Forget();

            Assert.That(states, Is.EqualTo(new[] { SessionState.Loading, SessionState.Failed }));
            Assert.That(session.State, Is.EqualTo(SessionState.Failed));
        }

        [Test]
        public void StateChanged_WhenAParticipantFailsToRun_GoesLoadingThenRunningThenFailed()
        {
            var states = new List<SessionState>();
            session.StateChanged += states.Add;
            Join("broken").RunError = new InvalidOperationException("prefab missing");

            session.BeginAsync().Forget();

            Assert.That(states, Is.EqualTo(new[] { SessionState.Loading, SessionState.Running, SessionState.Failed }));
        }

        [Test]
        public void StateChanged_WhenEndedWhileLoading_GoesLoadingThenDisposingThenDisposed_NeverRunning()
        {
            var states = new List<SessionState>();
            session.StateChanged += states.Add;
            Join("slow").Load = new UniTaskCompletionSource();
            session.BeginAsync().Forget();

            session.EndAsync().Forget();

            Assert.That(states, Is.EqualTo(new[] { SessionState.Loading, SessionState.Disposing, SessionState.Disposed }));
        }

        [Test]
        public void StateChanged_WhenEndedBeforeItBegan_GoesStraightToDisposingThenDisposed()
        {
            var states = new List<SessionState>();
            session.StateChanged += states.Add;
            Join("a");

            session.EndAsync().Forget();
            session.BeginAsync().Forget();

            Assert.That(states, Is.EqualTo(new[] { SessionState.Disposing, SessionState.Disposed }));
        }

        [Test]
        public void BeginningTwice_IsRefused()
        {
            Join("a");
            session.BeginAsync().Forget();

            Assert.Throws<InvalidOperationException>(() => session.BeginAsync().GetAwaiter().GetResult());
            Assert.That(session.State, Is.EqualTo(SessionState.Running));
        }

        // clean-checks#leak-names-owner: what a participant still holds after the dispose step is
        // reported with its feature name and the counts; nothing is cleaned up on its behalf.
        [Test]
        public void AfterTheDisposeStep_AParticipantThatStillHoldsViewsOrJobs_IsReportedByNameWithWhatItLeft()
        {
            var usage = CountWith();
            Join("clean");
            var leaky = Join("leaky");
            usage.Left[leaky] = new ParticipantUsage(2, 1);
            session.BeginAsync().Forget();

            session.EndAsync().Forget();

            Assert.That(session.State, Is.EqualTo(SessionState.Disposed));
            Assert.That(errors, Has.Count.EqualTo(1));
            Assert.That(errors[0].Message, Does.Contain("SessionScene").And.Contain("2 view(s)")
                .And.Contain("1 running job(s)"));
        }

        // clean-checks#edge-feature-khong-co-view-hay-viec-nao: counts of zero report nothing.
        [Test]
        public void AfterTheDisposeStep_ParticipantsThatHoldNothing_AreNotReported()
        {
            CountWith();
            Join("a");
            Join("b");
            session.BeginAsync().Forget();

            session.EndAsync().Forget();

            Assert.That(errors, Is.Empty);
        }

        // clean-checks#after-dispose-zero: everyone reached is counted, and only once all of them disposed.
        [Test]
        public void EveryParticipantReached_IsCountedOnlyAfterEveryoneDisposed()
        {
            CountWith();
            Join("a");
            Join("b");
            session.BeginAsync().Forget();
            log.Clear();

            session.EndAsync().Forget();

            Assert.That(log, Is.EqualTo(new[] { "dispose b", "dispose a", "count b", "count a" }));
        }

        [Test]
        public void WhenLoadingFails_OnlyTheParticipantsReachedAreCounted()
        {
            CountWith();
            Join("ok");
            Join("broken").LoadError = new InvalidOperationException("no save");
            Join("never");

            session.BeginAsync().Forget();

            Assert.That(log, Has.Member("count ok").And.Member("count broken"));
            Assert.That(log, Has.None.EqualTo("count never"));
        }

        // clean-checks#edge-feature-loi-o-buoc-huy: a participant that failed to clean up is still counted,
        // and what it left is reported besides its dispose error.
        [Test]
        public void AParticipantThatFailsToCleanUp_IsStillCounted_AndWhatItLeftIsReported()
        {
            var usage = CountWith();
            var broken = Join("broken");
            broken.DisposeError = new InvalidOperationException("disk full");
            usage.Left[broken] = new ParticipantUsage(1, 0);
            session.BeginAsync().Forget();

            session.EndAsync().Forget();

            Assert.That(errors, Has.Count.EqualTo(2));
            Assert.That(errors[0].Message, Does.Contain("failed to dispose").And.Contain("disk full"));
            Assert.That(errors[1].Message, Does.Contain("SessionScene").And.Contain("1 view(s)"));
        }

        // clean-checks: after the dispose step nothing of the session is still waiting, and everyone it
        // reached is cleaned up exactly once, whichever way the session ended.
        [TestCase("ended while running")]
        [TestCase("ended while loading")]
        [TestCase("ended before it began")]
        [TestCase("failed to load")]
        [TestCase("load took too long")]
        [TestCase("failed to run")]
        public void AfterTheSessionEnds_EveryoneReachedIsCleanedUpOnce_AndNoLoadTimerIsLeftWaiting(string ending)
        {
            var first = Join("first");
            var second = Join("second");
            var reached = new[] { "first", "second" };
            switch (ending)
            {
                case "ended while loading":
                    second.Load = new UniTaskCompletionSource();
                    break;
                case "failed to load":
                    second.LoadError = new InvalidOperationException("no save");
                    break;
                case "load took too long":
                    second.Load = new UniTaskCompletionSource();
                    break;
                case "failed to run":
                    first.RunError = new InvalidOperationException("prefab missing");
                    break;
            }

            if (ending != "ended before it began") session.BeginAsync().Forget();
            if (ending == "load took too long") timer.Expire();
            var end = session.EndAsync();

            Assert.That(end.Status, Is.EqualTo(UniTaskStatus.Succeeded));
            Assert.That(session.State, Is.EqualTo(SessionState.Disposed).Or.EqualTo(SessionState.Failed));
            foreach (var name in reached)
            {
                Assert.That(log.FindAll(entry => entry == "dispose " + name), Has.Count.EqualTo(1),
                    $"{name} should be cleaned up exactly once");
            }

            Assert.That(timer.StillWaiting, Is.EqualTo(0), "a load timer of the ended session is still waiting");
        }

        [Test]
        public void ALoadThatFinishesInTime_StopsItsTimer()
        {
            var slow = Join("slow");
            slow.Load = new UniTaskCompletionSource();
            session.BeginAsync().Forget();
            Assert.That(timer.StillWaiting, Is.EqualTo(1));

            slow.Load.TrySetResult();

            Assert.That(timer.StillWaiting, Is.EqualTo(0));
        }

        [Test]
        public void TheNextScenesSession_StartsClean_AndNeverTouchesTheParticipantsOfTheSceneBefore()
        {
            Join("loading-screen");
            session.BeginAsync().Forget();
            session.EndAsync().Forget();
            log.Clear();

            var next = new SessionSceneLogic(errors.Add, timer, Limit);
            next.Join(new FakeParticipant("game", log));

            Assert.That(next.State, Is.EqualTo(SessionState.Init));
            Assert.That(next.ParticipantCount, Is.EqualTo(1));
            Assert.That(next.LoadedCount, Is.EqualTo(0));
            Assert.That(next.FailedParticipant, Is.Null);

            next.BeginAsync().Forget();
            next.EndAsync().Forget();

            Assert.That(log, Is.EqualTo(new[] { "load game", "run game", "dispose game" }));
            Assert.That(errors, Is.Empty);
        }

        [TestCase("Game.Wallet", "WalletParticipant", ExpectedResult = "Wallet")]
        [TestCase("Game.Services.Popup", "PopupParticipant", ExpectedResult = "Popup")]
        [TestCase("Game.Wallet.Internal", "WalletParticipant", ExpectedResult = "Wallet")]
        [TestCase("Other.Library", "Thing", ExpectedResult = "Thing")]
        [TestCase(null, "Thing", ExpectedResult = "Thing")]
        public string FailedFeatureName_IsTheFeaturePartOfTheNamespace(string typeNamespace, string typeName)
        {
            return SessionSceneLogic.FeatureNameOf(typeNamespace, typeName);
        }

        [Test]
        public void InstallerAsset_RegistersASessionWaitingInInit()
        {
            var installer = AssetDatabase.LoadAssetAtPath<SessionSceneInstaller>(
                "Assets/Features/_Services/SessionScene/FeatureAssets/Public/SessionSceneInstaller.asset");
            Assert.That(installer, Is.Not.Null, "SessionSceneInstaller.asset missing or its script reference is broken");
            var registry = new ServiceRegistry();

            // Also proves the driver prefab is assigned and the time limit is valid: Install throws otherwise.
            installer.Install(registry);

            Assert.That(registry.Resolve<ISessionScene>().State, Is.EqualTo(SessionState.Init));
            Assert.That(registry.Resolve<ISessionLoadProgress>().LoadedCount, Is.EqualTo(0));
            Assert.That(registry.Resolve<ISessionLoadProgress>().FailedParticipant, Is.Null);
        }

        // participant-order#load-timeout: the installer asset the scenes use gives each participant 10 seconds.
        [Test]
        public void InstallerAsset_GivesEachParticipantTenSecondsToLoad()
        {
            var installer = AssetDatabase.LoadAssetAtPath<SessionSceneInstaller>(
                "Assets/Features/_Services/SessionScene/FeatureAssets/Public/SessionSceneInstaller.asset");
            Assert.That(installer, Is.Not.Null, "SessionSceneInstaller.asset missing or its script reference is broken");

            var limit = new SerializedObject(installer).FindProperty("loadTimeLimitSeconds");

            Assert.That(limit, Is.Not.Null, "SessionSceneInstaller has no loadTimeLimitSeconds field");
            Assert.That(limit.floatValue, Is.EqualTo(10f));
        }

        private FakeParticipant Join(string name)
        {
            var participant = new FakeParticipant(name, log);
            session.Join(participant);
            return participant;
        }

        /// <summary>Replaces the session with one that counts what participants hold after the dispose step.</summary>
        private FakeUsage CountWith()
        {
            var usage = new FakeUsage(log);
            session = new SessionSceneLogic(errors.Add, timer, Limit, usage);
            return usage;
        }

        private sealed class FakeUsage : IParticipantUsage
        {
            private readonly List<string> log;

            public FakeUsage(List<string> log)
            {
                this.log = log;
            }

            public Dictionary<ISessionParticipant, ParticipantUsage> Left { get; } =
                new Dictionary<ISessionParticipant, ParticipantUsage>();

            public ParticipantUsage CountFor(ISessionParticipant participant)
            {
                log.Add("count " + ((FakeParticipant)participant).Name);
                return Left.TryGetValue(participant, out var left) ? left : new ParticipantUsage(0, 0);
            }
        }

        /// <summary>The time limit passes only when the test calls <see cref="Expire"/>.</summary>
        private sealed class FakeTimer : ILoadTimer
        {
            private readonly List<UniTaskCompletionSource> pending = new List<UniTaskCompletionSource>();
            private readonly List<UniTaskCompletionSource> all = new List<UniTaskCompletionSource>();

            public List<TimeSpan> Requested { get; } = new List<TimeSpan>();

            /// <summary>Waits neither finished nor cancelled: work the session would leave running.</summary>
            public int StillWaiting => all.FindAll(wait => wait.Task.Status == UniTaskStatus.Pending).Count;

            public UniTask WaitAsync(TimeSpan limit, CancellationToken cancellation)
            {
                Requested.Add(limit);
                var wait = new UniTaskCompletionSource();
                cancellation.Register(() => wait.TrySetCanceled(cancellation));
                pending.Add(wait);
                all.Add(wait);
                return wait.Task;
            }

            public void Expire()
            {
                var waits = pending.ToArray();
                pending.Clear();
                foreach (var wait in waits) wait.TrySetResult();
            }
        }

        private sealed class FakeParticipant : ISessionParticipant
        {
            private readonly string name;
            private readonly List<string> log;

            public FakeParticipant(string name, List<string> log)
            {
                this.name = name;
                this.log = log;
            }

            public string Name => name;

            /// <summary>Null: loading finishes at once. Otherwise loading waits for this source.</summary>
            public UniTaskCompletionSource Load;
            public bool IgnoresCancellation;
            public Exception LoadError;
            public Exception RunError;
            public Exception DisposeError;
            public Action OnRun;

            public bool LoadWasCancelled { get; private set; }

            public UniTask LoadAsync(CancellationToken cancellation)
            {
                log.Add("load " + name);
                if (LoadError != null) throw LoadError;
                if (Load == null) return UniTask.CompletedTask;

                cancellation.Register(() =>
                {
                    LoadWasCancelled = true;
                    if (!IgnoresCancellation) Load.TrySetCanceled(cancellation);
                });
                return Load.Task;
            }

            public void Run()
            {
                log.Add("run " + name);
                OnRun?.Invoke();
                if (RunError != null) throw RunError;
            }

            public UniTask DisposeAsync()
            {
                log.Add("dispose " + name);
                if (DisposeError != null) throw DisposeError;
                return UniTask.CompletedTask;
            }
        }
    }
}
