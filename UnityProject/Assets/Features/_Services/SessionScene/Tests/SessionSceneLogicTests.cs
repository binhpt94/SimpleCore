using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Game.Core;
using NUnit.Framework;
using UnityEditor;

namespace Game.Services.SessionScene.Tests
{
    public class SessionSceneLogicTests
    {
        private List<string> log;
        private List<Exception> errors;
        private SessionSceneLogic session;

        [SetUp]
        public void SetUp()
        {
            log = new List<string>();
            errors = new List<Exception>();
            session = new SessionSceneLogic(errors.Add);
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
        public void SessionStaysLoading_AndNobodyRuns_UntilTheSlowestParticipantLoaded()
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
        public void WhenOneParticipantFailsToLoad_SessionFails_OthersAreCancelled_NobodyRuns_EveryoneCleansUp()
        {
            var waiting = Join("waiting");
            waiting.Load = new UniTaskCompletionSource();
            var broken = Join("broken");
            broken.LoadError = new InvalidOperationException("save file is corrupt");

            session.BeginAsync().Forget();

            Assert.That(session.State, Is.EqualTo(SessionState.Failed));
            Assert.That(waiting.LoadWasCancelled, Is.True);
            Assert.That(log, Has.None.StartsWith("run"));
            Assert.That(log, Has.Member("dispose waiting").And.Member("dispose broken"));
            Assert.That(errors, Has.Count.EqualTo(1));
            Assert.That(errors[0].Message, Does.Contain("FakeParticipant").And.Contain("save file is corrupt"));
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
        public void WhenAParticipantFailsToRun_SessionFails_AndEveryoneCleansUp()
        {
            Join("a").RunError = new InvalidOperationException("prefab missing");
            Join("b");

            session.BeginAsync().Forget();

            Assert.That(session.State, Is.EqualTo(SessionState.Failed));
            Assert.That(log, Has.None.EqualTo("run b"));
            Assert.That(log, Has.Member("dispose a").And.Member("dispose b"));
            Assert.That(errors, Has.Count.EqualTo(1));
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
        public void EndingWhileLoading_CancelsTheLoads_NobodyRuns_AndSessionIsDisposed()
        {
            var slow = Join("slow");
            slow.Load = new UniTaskCompletionSource();
            session.BeginAsync().Forget();

            session.EndAsync().Forget();

            Assert.That(slow.LoadWasCancelled, Is.True);
            Assert.That(session.State, Is.EqualTo(SessionState.Disposed));
            Assert.That(log, Has.None.StartsWith("run"));
            Assert.That(log, Has.Member("dispose slow"));
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
        public void InstallerAsset_RegistersASessionWaitingInInit()
        {
            var installer = AssetDatabase.LoadAssetAtPath<SessionSceneInstaller>(
                "Assets/Features/_Services/SessionScene/FeatureAssets/Public/SessionSceneInstaller.asset");
            Assert.That(installer, Is.Not.Null, "SessionSceneInstaller.asset missing or its script reference is broken");
            var registry = new ServiceRegistry();

            // Also proves the driver prefab is assigned: Install throws without it.
            installer.Install(registry);

            Assert.That(registry.Resolve<ISessionScene>().State, Is.EqualTo(SessionState.Init));
        }

        private FakeParticipant Join(string name)
        {
            var participant = new FakeParticipant(name, log);
            session.Join(participant);
            return participant;
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

            /// <summary>Null: loading finishes at once. Otherwise loading waits for this source.</summary>
            public UniTaskCompletionSource Load;
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
                    Load.TrySetCanceled(cancellation);
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
