using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using NUnit.Framework;

namespace Game.Core.Tests
{
    public class SessionParticipantBaseTests
    {
        [Test]
        public void Run_StartsTheParticipant()
        {
            var participant = new FakeParticipant();

            participant.Run();

            Assert.That(participant.Log, Is.EqualTo(new[] { "run" }));
        }

        [Test]
        public void Run_AfterSessionEnded_DoesNothing()
        {
            var participant = new FakeParticipant();
            participant.DisposeAsync().GetAwaiter().GetResult();
            participant.Log.Clear();

            participant.Run();

            Assert.That(participant.Log, Is.Empty);
        }

        [Test]
        public void SessionEnd_UndoesInReverseOrderOnce_ThenDisposes()
        {
            var participant = new FakeParticipant();
            participant.Register("undo-first");
            participant.Register("undo-second");

            participant.DisposeAsync().GetAwaiter().GetResult();

            Assert.That(participant.Log, Is.EqualTo(new[] { "undo-second", "undo-first", "dispose" }));
        }

        [Test]
        public void SessionEndTwice_RunsNothingTheSecondTime()
        {
            var participant = new FakeParticipant();
            participant.Register("undo");
            participant.DisposeAsync().GetAwaiter().GetResult();
            participant.Log.Clear();

            participant.DisposeAsync().GetAwaiter().GetResult();

            Assert.That(participant.Log, Is.Empty);
        }

        [Test]
        public void RegisteringNoUndo_Fails()
        {
            var participant = new FakeParticipant();

            Assert.Throws<ArgumentNullException>(() => participant.RegisterNull());
        }

        [Test]
        public void Load_PassesTheSessionCancellationOn()
        {
            var participant = new FakeParticipant();
            using (var source = new CancellationTokenSource())
            {
                participant.LoadAsync(source.Token).GetAwaiter().GetResult();

                Assert.That(participant.LoadCancellation, Is.EqualTo(source.Token));
            }
        }

        [Test]
        public void Load_WithNothingToLoad_FinishesAtOnce()
        {
            var participant = new RunOnlyParticipant();

            var load = participant.LoadAsync(CancellationToken.None);

            Assert.That(load.Status, Is.EqualTo(UniTaskStatus.Succeeded));
        }

        [Test]
        public void Load_Failing_ReachesTheSession()
        {
            var participant = new FakeParticipant { LoadError = new InvalidOperationException("config missing") };

            var error = Assert.Throws<InvalidOperationException>(
                () => participant.LoadAsync(CancellationToken.None).GetAwaiter().GetResult());

            Assert.That(error.Message, Is.EqualTo("config missing"));
        }

        [Test]
        public void SessionEndBeforeRun_StillUndoesWhatLoadHeld_AndNeverRuns()
        {
            var participant = new FakeParticipant { RegisterOnLoad = "undo-load" };
            participant.LoadAsync(CancellationToken.None).GetAwaiter().GetResult();

            participant.DisposeAsync().GetAwaiter().GetResult();
            participant.Run();

            Assert.That(participant.Log, Is.EqualTo(new[] { "load", "undo-load", "dispose" }));
        }

        [Test]
        public void SessionEnd_WithNothingToRelease_FinishesAtOnce()
        {
            var participant = new RunOnlyParticipant();

            var dispose = participant.DisposeAsync();

            Assert.That(dispose.Status, Is.EqualTo(UniTaskStatus.Succeeded));
        }

        [Test]
        public void FullLife_LoadsThenRunsThenUndoesThenDisposes()
        {
            var participant = new FakeParticipant { RegisterOnLoad = "undo-load", RegisterOnRun = "undo-run" };

            participant.LoadAsync(CancellationToken.None).GetAwaiter().GetResult();
            participant.Run();
            participant.DisposeAsync().GetAwaiter().GetResult();

            Assert.That(participant.Log, Is.EqualTo(new[] { "load", "run", "undo-run", "undo-load", "dispose" }));
        }

        private sealed class RunOnlyParticipant : SessionParticipantBase
        {
            protected override void OnRun() { }
        }

        private sealed class FakeParticipant : SessionParticipantBase
        {
            public readonly List<string> Log = new List<string>();
            public CancellationToken LoadCancellation { get; private set; }
            public Exception LoadError { get; set; }
            public string RegisterOnLoad { get; set; }
            public string RegisterOnRun { get; set; }

            public void Register(string name) => WhenDisposed(() => Log.Add(name));

            public void RegisterNull() => WhenDisposed(null);

            protected override UniTask OnLoadAsync(CancellationToken cancellation)
            {
                LoadCancellation = cancellation;
                if (LoadError != null) throw LoadError;
                if (RegisterOnLoad != null)
                {
                    Log.Add("load");
                    Register(RegisterOnLoad);
                }
                return UniTask.CompletedTask;
            }

            protected override void OnRun()
            {
                Log.Add("run");
                if (RegisterOnRun != null) Register(RegisterOnRun);
            }

            protected override UniTask OnDisposeAsync()
            {
                Log.Add("dispose");
                return UniTask.CompletedTask;
            }
        }
    }
}
