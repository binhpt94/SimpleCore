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

        private sealed class FakeParticipant : SessionParticipantBase
        {
            public readonly List<string> Log = new List<string>();
            public CancellationToken LoadCancellation { get; private set; }

            public void Register(string name) => WhenDisposed(() => Log.Add(name));

            public void RegisterNull() => WhenDisposed(null);

            protected override UniTask OnLoadAsync(CancellationToken cancellation)
            {
                LoadCancellation = cancellation;
                return UniTask.CompletedTask;
            }

            protected override void OnRun() => Log.Add("run");

            protected override UniTask OnDisposeAsync()
            {
                Log.Add("dispose");
                return UniTask.CompletedTask;
            }
        }
    }
}
