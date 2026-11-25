using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using NUnit.Framework;

namespace Game.Run.Tests
{
    /// <summary>
    /// Entering and leaving the pause (feature-run/run-pause/pause-rules) and the device signals around it:
    /// leaving the game and the back key (feature-run/run-flow/flow-rules, edge cases).
    /// </summary>
    public class RunPauseTests
    {
        private static readonly RunTeam Team = new RunTeam(new[] { "knight", "axe", "archer" });

        private FakeDelay _delay;
        private RunScore _score;
        private RunFlow _flow;
        private FakeSignals _signals;
        private RunInterrupts _interrupts;
        private List<string> _log;

        [SetUp]
        public void SetUp()
        {
            _delay = new FakeDelay();
            _score = new RunScore(new FakeStore());
            _flow = new RunFlow(_score, _delay, 1f, 3);
            _signals = new FakeSignals();
            _interrupts = new RunInterrupts(_flow, _signals, pauseOnLeave: true, backKeyPauses: true);
            _log = new List<string>();
            _flow.StepChanged += s => _log.Add("step:" + s);
            _flow.RunStarted += _ => _log.Add("RunStarted");
            _flow.BattleStarted += () => _log.Add("BattleStarted");
            _flow.BattleStopped += () => _log.Add("BattleStopped");
            _flow.BattleCleared += () => _log.Add("BattleCleared");
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
        public void PauseButtonPausesARunningBattle()
        {
            EnterBattle();
            _flow.Pause();
            CollectionAssert.AreEqual(new[] { "step:Paused" }, _log);
            Assert.IsTrue(_flow.IsPaused);
        }

        [Test]
        public void LeavingTheGamePausesARunningBattle()
        {
            EnterBattle();
            _signals.Leave();
            CollectionAssert.AreEqual(new[] { "step:Paused" }, _log);
            Assert.IsTrue(_flow.IsPaused);
        }

        [Test]
        public void BackKeyPausesARunningBattle()
        {
            EnterBattle();
            _signals.Back();
            CollectionAssert.AreEqual(new[] { "step:Paused" }, _log);
            Assert.IsTrue(_flow.IsPaused);
        }

        [Test]
        public void ContinueRunsTheBattleAgainAtOnceWithoutCountdown()
        {
            EnterBattle();
            _flow.Pause();
            _log.Clear();

            _flow.Resume();

            CollectionAssert.AreEqual(new[] { "step:Playing" }, _log);
            Assert.IsFalse(_flow.IsPaused);
            Assert.AreEqual(0, _delay.Scheduled, "Continue must not wait for anything.");
        }

        [Test]
        public void ComingBackAfterLeavingStaysPausedUntilContinue()
        {
            EnterBattle();
            _signals.Leave();
            _delay.Fire();
            _delay.NextFrameArrives();

            Assert.AreEqual(RunStep.Paused, _flow.Step);
            Assert.AreEqual(0, _delay.Scheduled, "Nothing may resume the battle on its own.");

            _flow.Resume();
            Assert.AreEqual(RunStep.Playing, _flow.Step);
        }

        [Test]
        public void LeavingAgainWhilePausedOpensNothingMore()
        {
            EnterBattle();
            _signals.Leave();
            _log.Clear();

            _signals.Leave();
            _flow.Pause();

            CollectionAssert.IsEmpty(_log);
            Assert.AreEqual(RunStep.Paused, _flow.Step);
        }

        [Test]
        public void LeavingAfterTheCastleFellDoesNotPauseAndTheLosePopupStillShows()
        {
            EnterBattle();
            _flow.Lose();
            _log.Clear();

            _signals.Leave();
            _signals.Back();
            Assert.AreEqual(RunStep.Ending, _flow.Step);
            CollectionAssert.IsEmpty(_log);

            _delay.Fire();
            Assert.AreEqual(RunStep.Lost, _flow.Step);

            _signals.Leave();
            _signals.Back();
            Assert.AreEqual(RunStep.Lost, _flow.Step);
            CollectionAssert.AreEqual(new[] { "step:Lost" }, _log);
        }

        [Test]
        public void LeavingOrBackKeyOnTheStartScreenOrTeamSelectDoesNothing()
        {
            _signals.Leave();
            _signals.Back();
            Assert.AreEqual(RunStep.Home, _flow.Step);

            _flow.Play();
            _log.Clear();
            _signals.Leave();
            _signals.Back();
            Assert.AreEqual(RunStep.TeamSelect, _flow.Step);
            CollectionAssert.IsEmpty(_log);
        }

        [Test]
        public void BackKeyOnThePausePopupIsLeftToThePopup()
        {
            EnterBattle();
            _flow.Pause();
            _log.Clear();

            _signals.Back();

            Assert.AreEqual(RunStep.Paused, _flow.Step);
            CollectionAssert.IsEmpty(_log);
        }

        [Test]
        public void CastleFallingWhilePausedStillEndsTheRun()
        {
            EnterBattle();
            _flow.Pause();
            _log.Clear();

            _flow.Lose();
            Assert.AreEqual(RunStep.Ending, _flow.Step);
            Assert.IsFalse(_flow.IsPaused);
            _delay.Fire();
            Assert.AreEqual(RunStep.Lost, _flow.Step);
        }

        [Test]
        public void WithTheDeviceSignalsOffLeavingAndBackKeyDoNothing()
        {
            _interrupts.Dispose();
            using (new RunInterrupts(_flow, _signals, pauseOnLeave: false, backKeyPauses: false))
            {
                EnterBattle();
                _signals.Leave();
                _signals.Back();
                Assert.AreEqual(RunStep.Playing, _flow.Step);
            }
        }

        [Test]
        public void GivingUpFromThePauseClearsTheStillBattleAndTheNextRunStartsClean()
        {
            EnterBattle();
            _flow.CountClearedWave(1);
            _delay.NextFrameArrives();
            _flow.Pause();
            _log.Clear();

            _flow.Abandon();
            CollectionAssert.AreEqual(new[] { "BattleStopped", "BattleCleared", "step:Home" }, _log);
            Assert.IsFalse(_flow.IsPaused);

            _log.Clear();
            _flow.Play();
            _flow.EnterBattle(Team);
            CollectionAssert.AreEqual(new[] { "step:TeamSelect", "step:Playing", "RunStarted", "BattleStarted" }, _log);
            Assert.AreEqual(0, _score.Score);
            Assert.IsFalse(_flow.IsPaused);
        }

        [Test]
        public void SessionListensToTheDeviceWhileItRunsAndStopsAtTheEnd()
        {
            _interrupts.Dispose();
            var made = 0;
            var signals = new FakeSignals();
            var session = new RunSession(_flow, _score, _delay, () => { made++; return signals; },
                pauseOnLeave: true, backKeyPauses: true);
            session.LoadAsync(CancellationToken.None).Forget();
            session.Run();
            Assert.AreEqual(1, made);

            EnterBattle();
            signals.Leave();
            Assert.AreEqual(RunStep.Paused, _flow.Step);
            _flow.Resume();

            session.DisposeAsync().Forget();
            Assert.IsTrue(signals.Disposed);
            signals.Leave();
            signals.Back();
            Assert.AreEqual(RunStep.Playing, _flow.Step);
        }

        [Test]
        public void SessionWithTheDeviceSignalsOffMakesNoUnityObject()
        {
            _interrupts.Dispose();
            var made = 0;
            var session = new RunSession(_flow, _score, _delay, () => { made++; return new FakeSignals(); });
            session.Run();
            Assert.AreEqual(0, made);
            session.DisposeAsync().Forget();
        }

        private sealed class FakeSignals : IAppSignals
        {
            public bool Disposed { get; private set; }

            public event Action LeftApp;
            public event Action BackPressed;

            public void Leave() => LeftApp?.Invoke();

            public void Back() => BackPressed?.Invoke();

            public void Dispose() => Disposed = true;
        }

        private sealed class FakeDelay : IRunDelay
        {
            private Action _then;
            private Action _nextFrame;

            public int Scheduled { get; private set; }

            public IDisposable Schedule(float seconds, Action then)
            {
                Scheduled++;
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
