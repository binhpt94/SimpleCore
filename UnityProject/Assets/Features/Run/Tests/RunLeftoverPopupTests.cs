using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using NUnit.Framework;

namespace Game.Run.Tests
{
    /// <summary>feature-run/run-start/replay-checks: a new run starts with no popup open; a leftover popup is logged, not closed.</summary>
    public class RunLeftoverPopupTests
    {
        private static readonly RunTeam Team = new RunTeam(new[] { "knight", "axe", "archer" });

        private FakeDelay _delay;
        private RunFlow _flow;
        private List<string> _open;
        private FakeLog _log;
        private LeftoverPopupCheck _check;

        [SetUp]
        public void SetUp()
        {
            _delay = new FakeDelay();
            _flow = new RunFlow(new RunScore(new FakeStore()), _delay, 1f, 3);
            _open = new List<string>();
            _log = new FakeLog();
            _check = new LeftoverPopupCheck(_flow, () => _open, _delay, _log);
            _check.Run();
        }

        private void EnterBattle()
        {
            _flow.Play();
            _flow.EnterBattle(Team);
        }

        [Test]
        public void NewRunWithNoPopupOpenLogsNothing()
        {
            EnterBattle();
            _delay.NextFrameArrives();
            CollectionAssert.IsEmpty(_log.Errors);
        }

        [Test]
        public void NewRunWithAPopupStillOpenLogsAnErrorNamingIt()
        {
            _open.Add("PopupLose/Main");
            EnterBattle();
            _delay.NextFrameArrives();

            Assert.AreEqual(1, _log.Errors.Count);
            StringAssert.Contains("PopupLose/Main", _log.Errors[0]);
            Assert.AreEqual(1, _open.Count, "The run does not close popups itself.");
        }

        [Test]
        public void PopupClosedByThePressThatStartedTheRunIsNotAnError()
        {
            // The lose popup closes after the run already started the new battle, in the same frame.
            _open.Add("PopupLose/Main");
            _flow.BattleStarted += () => _open.Clear();
            EnterBattle();
            _delay.NextFrameArrives();

            CollectionAssert.IsEmpty(_log.Errors);
        }

        [Test]
        public void EveryNewRunIsChecked()
        {
            EnterBattle();
            _delay.NextFrameArrives();
            _flow.Lose();
            _delay.Fire();
            _open.Add("PopupLose/Main");
            _flow.Replay();
            _flow.BuildBattle(Team);
            _delay.NextFrameArrives();

            Assert.AreEqual(1, _log.Errors.Count);
        }

        [Test]
        public void SessionEndBeforeTheCheckLogsNothing()
        {
            _open.Add("PopupLose/Main");
            EnterBattle();
            _check.DisposeAsync().Forget();
            _delay.NextFrameArrives();

            CollectionAssert.IsEmpty(_log.Errors);
        }

        private sealed class FakeLog : IRunLog
        {
            public readonly List<string> Errors = new List<string>();

            public void Error(string message) => Errors.Add(message);
        }

        private sealed class FakeDelay : IRunDelay
        {
            private Action _then;
            private Action _nextFrame;

            public IDisposable Schedule(float seconds, Action then)
            {
                _then = then;
                return new Cancel(() => _then = null);
            }

            public IDisposable NextFrame(Action then)
            {
                _nextFrame = then;
                return new Cancel(() => { if (_nextFrame == then) _nextFrame = null; });
            }

            public void Fire()
            {
                var then = _then;
                _then = null;
                then?.Invoke();
            }

            public void NextFrameArrives()
            {
                var then = _nextFrame;
                _nextFrame = null;
                then?.Invoke();
            }

            private sealed class Cancel : IDisposable
            {
                private readonly Action _cancel;
                public Cancel(Action cancel) => _cancel = cancel;
                public void Dispose() => _cancel();
            }
        }

        private sealed class FakeStore : IBestScoreStore
        {
            public int Load() => 0;

            public void Save(int best) { }
        }
    }
}
