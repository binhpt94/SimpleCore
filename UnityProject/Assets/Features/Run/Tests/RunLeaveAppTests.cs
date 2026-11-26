using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace Game.Run.Tests
{
    /// <summary>
    /// Leaving and coming back to the game (feature-run/run-flow/flow-rules, edge cases): no step changes, and time
    /// away does not count toward the 1 second before the lose popup.
    /// </summary>
    public class RunLeaveAppTests
    {
        private static readonly RunTeam Team = new RunTeam(new[] { "knight", "axe", "archer" });

        private FakeDelay _delay;
        private RunFlow _flow;
        private FakeSignals _signals;
        private RunInterrupts _interrupts;
        private List<string> _log;

        [SetUp]
        public void SetUp()
        {
            _delay = new FakeDelay();
            _flow = new RunFlow(new RunScore(new FakeStore()), _delay, 1f, 3);
            _signals = new FakeSignals();
            _interrupts = new RunInterrupts(_signals, _delay);
            _log = new List<string>();
            _flow.StepChanged += s => _log.Add("step:" + s);
        }

        [TearDown]
        public void TearDown() => _interrupts.Dispose();

        private void EnterBattle()
        {
            _flow.Play();
            _flow.EnterBattle(Team);
            _log.Clear();
        }

        [Test]
        public void LeavingARunningBattleLeavesItRunning()
        {
            EnterBattle();
            _signals.Leave();
            _signals.Return();
            Assert.AreEqual(RunStep.Playing, _flow.Step);
            CollectionAssert.IsEmpty(_log);
        }

        [Test]
        public void LeavingAfterTheCastleFellChangesNothingAndTheLosePopupShowsOnComingBack()
        {
            EnterBattle();
            _flow.Lose();
            _log.Clear();

            _signals.Leave();
            Assert.AreEqual(RunStep.Ending, _flow.Step);
            CollectionAssert.IsEmpty(_log);

            _signals.Return();
            _delay.Fire();
            Assert.AreEqual(RunStep.Lost, _flow.Step);

            _signals.Leave();
            Assert.AreEqual(RunStep.Lost, _flow.Step);
            CollectionAssert.AreEqual(new[] { "step:Lost" }, _log);
        }

        [Test]
        public void TimeAwayDuringTheWaitAfterTheCastleFellDoesNotCountTowardTheLosePopup()
        {
            EnterBattle();
            _flow.Lose();

            _signals.Leave();
            _delay.Fire();
            Assert.AreEqual(RunStep.Ending, _flow.Step, "The wait stands still while the game is off the screen.");

            _signals.Return();
            Assert.AreEqual(RunStep.Ending, _flow.Step, "Coming back does not end the wait at once.");
            _delay.Fire();
            Assert.AreEqual(RunStep.Lost, _flow.Step);
        }

        [Test]
        public void LeavingOnTheStartScreenOrTeamSelectDoesNothing()
        {
            _signals.Leave();
            Assert.AreEqual(RunStep.Home, _flow.Step);

            _flow.Play();
            _log.Clear();
            _signals.Leave();
            _signals.Return();
            Assert.AreEqual(RunStep.TeamSelect, _flow.Step);
            CollectionAssert.IsEmpty(_log);
        }

        [Test]
        public void EndingTheSessionWhileAwayReleasesTheWaits()
        {
            EnterBattle();
            _flow.Lose();
            _signals.Leave();

            _interrupts.Dispose();
            _signals.Return();
            _delay.Fire();
            Assert.AreEqual(RunStep.Lost, _flow.Step, "Nothing stays held after the session ends.");
        }

        private sealed class FakeSignals : IAppSignals
        {
            public event Action LeftApp;
            public event Action CameBack;

            public void Leave() => LeftApp?.Invoke();

            public void Return() => CameBack?.Invoke();

            public void Dispose() { }
        }

        /// <summary>Fire() is the wait's time passing; while held (game off the screen) that time does not count.</summary>
        private sealed class FakeDelay : IRunDelay, IRunWaitHold
        {
            private Action _then;
            private Action _nextFrame;
            private bool _held;

            public void Hold() => _held = true;

            public void Release() => _held = false;

            public IDisposable Schedule(float seconds, Action then)
            {
                _then = then;
                return new Cancel(() => _then = null);
            }

            public IDisposable NextFrame(Action then)
            {
                _nextFrame = then;
                return new Cancel(() => _nextFrame = null);
            }

            public void Fire()
            {
                if (_held) return;
                var then = _then;
                _then = null;
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
