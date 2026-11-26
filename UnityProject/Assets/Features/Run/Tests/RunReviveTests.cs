using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace Game.Run.Tests
{
    /// <summary>
    /// One revive per run (feature-run/run-flow/flow-rules#revive-step, feature-popup-revive/revive-order/order-values,
    /// feature-run/run-score/score-rules, feature-run/run-start/start-values, feature-run/run-pause/pause-rules).
    /// </summary>
    public class RunReviveTests
    {
        private static readonly RunTeam Team = new RunTeam(new[] { "knight", "axe", "archer" });

        private FakeDelay _delay;
        private FakeStore _store;
        private RunScore _score;
        private RunFlow _flow;
        private IRunRevive _revive;
        private List<string> _log;

        [SetUp]
        public void SetUp()
        {
            _delay = new FakeDelay();
            _store = new FakeStore();
            _score = new RunScore(_store);
            _score.LoadBest();
            _flow = new RunFlow(_score, _delay, 1f, 3, reviveOnce: true);
            _revive = _flow;
            _log = new List<string>();
            _flow.StepChanged += s => _log.Add("step:" + s);
            _flow.RunStarted += _ => _log.Add("RunStarted");
            _flow.BattleStarted += () => _log.Add("BattleStarted");
            _flow.BattleStopped += () => _log.Add("BattleStopped");
            _flow.BattleCleared += () => _log.Add("BattleCleared");
            _revive.ReviveStarted += () => _log.Add("ReviveStarted");
        }

        private void EnterBattle()
        {
            _flow.Play();
            _flow.EnterBattle(Team);
            _log.Clear();
        }

        private void ClearWave(int wave)
        {
            _flow.CountClearedWave(wave);
            _delay.NextFrameArrives();
        }

        /// <summary>The castle falls for the first time and the 1 second wait ends: the revive popup is up.</summary>
        private void FallAndOffer()
        {
            _flow.Lose();
            _delay.Fire();
            _log.Clear();
        }

        private void ReviveAndStand()
        {
            _revive.Revive();
            _revive.ReviveShown();
            _log.Clear();
        }

        [Test]
        public void FirstCastleFallFreezesTheBattleAndOffersTheReviveAfterTheDelay()
        {
            EnterBattle();
            _flow.Lose();

            CollectionAssert.AreEqual(new[] { "step:ReviveWait" }, _log, "The battle is not stopped, only stands still.");
            Assert.IsTrue(_flow.IsPaused);
            Assert.AreEqual(1f, _delay.LastSeconds);

            _delay.Fire();
            Assert.AreEqual(RunStep.ReviveOffer, _flow.Step);
            Assert.IsTrue(_flow.IsPaused);
        }

        [Test]
        public void NothingCanBePressedDuringTheWaitBeforeTheRevivePopup()
        {
            EnterBattle();
            _flow.Lose();
            _log.Clear();

            _flow.Pause();
            _flow.Abandon();
            _flow.Replay();
            _revive.Revive();
            _revive.DeclineRevive();
            _flow.Lose();

            CollectionAssert.IsEmpty(_log);
            Assert.AreEqual(RunStep.ReviveWait, _flow.Step);
        }

        [Test]
        public void WatchedAdStandsTheCastleUpAndTheBattleGoesOnFromWhereItStopped()
        {
            EnterBattle();
            ClearWave(1);
            FallAndOffer();

            _revive.Revive();
            CollectionAssert.AreEqual(new[] { "step:Reviving", "ReviveStarted" }, _log);
            Assert.IsTrue(_flow.IsPaused, "The battle stands still while the castle stands up.");

            _log.Clear();
            _revive.ReviveShown();
            CollectionAssert.AreEqual(new[] { "step:Playing" }, _log, "No new run, no reset, no countdown.");
            Assert.IsFalse(_flow.IsPaused);
            Assert.AreEqual(1, _score.Score);
            Assert.AreEqual(0, _delay.Pending, "Nothing waits before the battle runs again.");
        }

        [Test]
        public void CastleCanReportItIsShownFromInsideReviveStarted()
        {
            EnterBattle();
            FallAndOffer();
            _revive.ReviveStarted += _revive.ReviveShown;

            _revive.Revive();
            Assert.AreEqual(RunStep.Playing, _flow.Step);
        }

        [Test]
        public void DecliningTheReviveLosesAtOnceWithoutAnotherWait()
        {
            EnterBattle();
            ClearWave(1);
            FallAndOffer();

            _revive.DeclineRevive();

            CollectionAssert.AreEqual(new[] { "BattleStopped", "step:Lost" }, _log);
            Assert.AreEqual(0, _delay.Pending);
            Assert.AreEqual(1, _score.Score);
            Assert.AreEqual(1, _store.Saved, "The best is saved before the lose popup.");
            Assert.IsFalse(_revive.LostAfterFailedRevive);
        }

        [Test]
        public void AdNotWatchedLosesAtOnceAndTellsTheLosePopup()
        {
            EnterBattle();
            FallAndOffer();

            _revive.FailRevive();

            CollectionAssert.AreEqual(new[] { "BattleStopped", "step:Lost" }, _log);
            Assert.IsTrue(_revive.LostAfterFailedRevive);
        }

        [Test]
        public void OnlyTheFirstChoiceOnTheRevivePopupCounts()
        {
            EnterBattle();
            FallAndOffer();
            _flow.BattleStopped += () => _revive.Revive();

            _revive.DeclineRevive();
            _revive.Revive();
            _revive.FailRevive();

            Assert.AreEqual(RunStep.Lost, _flow.Step);
            Assert.IsFalse(_revive.LostAfterFailedRevive);
        }

        [Test]
        public void SecondCastleFallInTheRunGoesStraightToTheLosePopup()
        {
            EnterBattle();
            FallAndOffer();
            ReviveAndStand();

            _flow.Lose();
            CollectionAssert.AreEqual(new[] { "step:Ending", "BattleStopped" }, _log);
            _delay.Fire();
            Assert.AreEqual(RunStep.Lost, _flow.Step);
        }

        [Test]
        public void CastleFallingWhileItStandsUpIsIgnored()
        {
            EnterBattle();
            FallAndOffer();
            _revive.Revive();
            _log.Clear();

            _flow.Lose();
            CollectionAssert.IsEmpty(_log);
            Assert.AreEqual(RunStep.Reviving, _flow.Step);
        }

        [Test]
        public void EveryRunHasItsOneReviveAgainEvenAfterTheLastRunUsedIt()
        {
            EnterBattle();
            FallAndOffer();
            ReviveAndStand();
            _flow.Lose();
            _delay.Fire();

            _flow.Replay();
            _flow.BuildBattle(Team);
            _log.Clear();

            _flow.Lose();
            Assert.AreEqual(RunStep.ReviveWait, _flow.Step);
        }

        [Test]
        public void ARunThatDeclinedStartsTheNextRunWithItsReviveAndNoFailFlag()
        {
            EnterBattle();
            FallAndOffer();
            _revive.FailRevive();
            _flow.Replay();
            Assert.IsFalse(_revive.LostAfterFailedRevive);
            _flow.BuildBattle(Team);

            _flow.Lose();
            Assert.AreEqual(RunStep.ReviveWait, _flow.Step);
        }

        [Test]
        public void ReviveDoesNotSettleOrResetTheScoreAndCountingGoesOn()
        {
            EnterBattle();
            ClearWave(1);
            ClearWave(2);
            FallAndOffer();
            Assert.AreEqual(0, _store.Saved, "No settling at the first fall.");
            Assert.IsFalse(_score.IsNewBest);

            ReviveAndStand();
            Assert.AreEqual(2, _score.Score);
            ClearWave(3);
            Assert.AreEqual(3, _score.Score);

            _flow.Lose();
            Assert.AreEqual(3, _store.Saved, "Settled once, when the run really ends.");
        }

        [Test]
        public void WaveClearedInTheFrameOfTheFirstFallCountsWhenTheCastleIsRevived()
        {
            EnterBattle();
            ClearWave(1);
            _flow.CountClearedWave(2);
            _flow.Lose();
            _flow.CountClearedWave(3);
            _delay.NextFrameArrives();
            _delay.Fire();
            Assert.AreEqual(1, _score.Score, "Held while the revive is decided.");

            _revive.Revive();
            Assert.AreEqual(1, _score.Score, "Still held while the castle stands up.");
            _revive.ReviveShown();
            Assert.AreEqual(3, _score.Score);
        }

        [Test]
        public void WaveClearedInTheFrameOfTheFirstFallDoesNotCountWhenTheRunIsLost()
        {
            EnterBattle();
            ClearWave(1);
            _flow.CountClearedWave(2);
            _flow.Lose();
            _delay.NextFrameArrives();
            _delay.Fire();

            _revive.DeclineRevive();
            Assert.AreEqual(1, _score.Score);
            Assert.AreEqual(1, _store.Saved);
        }

        [Test]
        public void PauseIsIgnoredInEveryReviveStepAndWorksAgainAfterTheRevive()
        {
            EnterBattle();
            _flow.Lose();
            _flow.Pause();
            Assert.AreEqual(RunStep.ReviveWait, _flow.Step);
            _delay.Fire();
            _flow.Pause();
            Assert.AreEqual(RunStep.ReviveOffer, _flow.Step);
            _revive.Revive();
            _flow.Pause();
            Assert.AreEqual(RunStep.Reviving, _flow.Step);

            _revive.ReviveShown();
            _flow.Pause();
            Assert.AreEqual(RunStep.Paused, _flow.Step);
            _flow.Resume();
            Assert.AreEqual(RunStep.Playing, _flow.Step);
        }

        [Test]
        public void LeavingTheGameOrBackKeyDuringTheReviveOpensNoPause()
        {
            var signals = new FakeSignals();
            using (new RunInterrupts(_flow, signals, pauseOnLeave: true, backKeyPauses: true, waits: _delay))
            {
                EnterBattle();
                _flow.Lose();
                signals.Leave();
                signals.Back();
                Assert.AreEqual(RunStep.ReviveWait, _flow.Step);

                signals.Return();
                _delay.Fire();
                Assert.AreEqual(RunStep.ReviveOffer, _flow.Step, "Coming back shows the revive popup, not the pause.");

                signals.Leave();
                signals.Back();
                Assert.AreEqual(RunStep.ReviveOffer, _flow.Step);
            }
        }

        [Test]
        public void TimeAwayDuringTheWaitAfterTheFirstCastleFallDoesNotCountTowardTheRevivePopup()
        {
            var signals = new FakeSignals();
            using (new RunInterrupts(_flow, signals, pauseOnLeave: true, backKeyPauses: true, waits: _delay))
            {
                EnterBattle();
                _flow.Lose();

                signals.Leave();
                _delay.Fire();
                Assert.AreEqual(RunStep.ReviveWait, _flow.Step, "The wait stands still while the game is off the screen.");

                signals.Return();
                Assert.AreEqual(RunStep.ReviveWait, _flow.Step, "Coming back does not end the wait at once.");
                _delay.Fire();
                Assert.AreEqual(RunStep.ReviveOffer, _flow.Step, "First fall of the run: the revive popup shows.");
            }
        }

        [Test]
        public void LeavingDuringTheWaitAfterTheSecondCastleFallShowsTheLosePopupOnComingBack()
        {
            var signals = new FakeSignals();
            using (new RunInterrupts(_flow, signals, pauseOnLeave: true, backKeyPauses: true, waits: _delay))
            {
                EnterBattle();
                _flow.Lose();
                _delay.Fire();
                _revive.Revive();
                _revive.ReviveShown();
                _flow.Lose();
                Assert.AreEqual(RunStep.Ending, _flow.Step);

                signals.Leave();
                signals.Back();
                _delay.Fire();
                Assert.AreEqual(RunStep.Ending, _flow.Step, "No pause, and time away does not count.");

                signals.Return();
                _delay.Fire();
                Assert.AreEqual(RunStep.Lost, _flow.Step, "Not the first fall of the run: the lose popup shows.");
            }
        }

        [Test]
        public void CastleFallingWhilePausedOffersTheReviveAndThePauseEnds()
        {
            EnterBattle();
            _flow.Pause();
            _log.Clear();

            _flow.Lose();
            CollectionAssert.AreEqual(new[] { "step:ReviveWait" }, _log);
        }

        [Test]
        public void SessionEndDropsTheWaitBeforeTheRevivePopup()
        {
            EnterBattle();
            _flow.Lose();
            _flow.Shutdown();
            Assert.IsTrue(_delay.Cancelled);
            Assert.AreEqual(RunStep.ReviveWait, _flow.Step);
        }

        [Test]
        public void ReviveRequestsOutsideTheRevivePopupAreIgnored()
        {
            EnterBattle();
            _revive.Revive();
            _revive.DeclineRevive();
            _revive.FailRevive();
            _revive.ReviveShown();
            CollectionAssert.IsEmpty(_log);
            Assert.AreEqual(RunStep.Playing, _flow.Step);
        }

        private sealed class FakeSignals : IAppSignals
        {
            public event Action LeftApp;
            public event Action CameBack;
            public event Action BackPressed;

            public void Leave() => LeftApp?.Invoke();

            public void Return() => CameBack?.Invoke();

            public void Back() => BackPressed?.Invoke();

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

            public float LastSeconds { get; private set; }
            public bool Cancelled { get; private set; }
            public int Pending => (_then != null ? 1 : 0) + (_nextFrame != null ? 1 : 0);

            public IDisposable Schedule(float seconds, Action then)
            {
                LastSeconds = seconds;
                _then = then;
                return new Cancel(() =>
                {
                    if (_then == then)
                    {
                        _then = null;
                        Cancelled = true;
                    }
                });
            }

            public IDisposable NextFrame(Action then)
            {
                _nextFrame = then;
                return new Cancel(() =>
                {
                    if (_nextFrame == then) _nextFrame = null;
                });
            }

            public void Fire()
            {
                if (_held) return;
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
            public int Saved { get; private set; }

            public int Load() => 0;

            public void Save(int best) => Saved = best;
        }
    }
}
