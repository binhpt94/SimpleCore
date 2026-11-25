using System;
using System.Collections.Generic;
using System.Linq;
using Game.SimClock;
using NUnit.Framework;

namespace Game.Run.Tests
{
    /// <summary>
    /// The run's part of feature-popup-revive/revive-order/order-values (PROTOTYPE revive flow), on the shared clock
    /// of feature-sim-clock: each test names the requirement id it checks.
    /// </summary>
    public class RunReviveOrderTests
    {
        private static readonly RunTeam Team = new RunTeam(new[] { "knight", "axe", "archer" });

        private List<string> _log;
        private FakeSimClock _clock;
        private SimClockRunDelay _waits;
        private FakeStore _store;
        private RunScore _score;
        private RunFlow _flow;

        [SetUp]
        public void SetUp()
        {
            _log = new List<string>();
            _clock = new FakeSimClock();
            _waits = new SimClockRunDelay(_clock);
            _waits.Listen();
            _store = new FakeStore();
            _score = new RunScore(_store);
            _flow = new RunFlow(_score, _waits, 1f, 3, reviveOnce: true, pauseEnabled: true,
                clock: new SimRunClock(_clock, _clock));
            _flow.StepChanged += s => _log.Add("step:" + s);
            _flow.BattleStopped += () => _log.Add("BattleStopped");
            _flow.BattleCleared += () => _log.Add("BattleCleared");
            _flow.ReviveStarted += () => _log.Add("ReviveStarted");
            _flow.Play();
            _flow.EnterBattle(Team);
            _log.Clear();
        }

        [TearDown]
        public void TearDown() => _waits.Dispose();

        /// <summary>The castle falls for the first time and the 1 s wait runs out: the revive popup is offered.</summary>
        private void FallAndOffer()
        {
            _flow.Lose();
            _clock.Step(40);
            Assert.AreEqual(RunStep.ReviveOffer, _flow.Step);
        }

        private void ReviveAndShow()
        {
            _flow.Revive();
            _flow.ReviveShown();
        }

        [Test]
        public void PopupAfter1s_RevivePopupOpensAfterOneSecondOfClockStepsWhileTheBattleStandsStill()
        {
            // #popup-after-1s: the wait counts every step of the clock (StepCount), since gameplay stands still.
            _flow.Lose();
            Assert.AreEqual(RunStep.ReviveWait, _flow.Step);
            Assert.IsFalse(_clock.Gameplay, "Nothing of the battle moves during the wait.");

            _clock.Step(39);
            Assert.AreEqual(RunStep.ReviveWait, _flow.Step, "0.975 s: not yet.");
            Assert.AreEqual(0, _clock.GameplayStepCount, "No gameplay step ran during the wait.");
            _clock.Step(1);
            Assert.AreEqual(RunStep.ReviveOffer, _flow.Step, "1 s after the battle stopped.");
        }

        [Test]
        public void EdgeLeavingTheGameDuringTheOneSecondDelay_PauseIsDroppedAndTimeOutsideDoesNotCount()
        {
            // #edge-roi-game-trong-1-giay-tre: leaving asks for a pause, which is dropped; the clock runs no step
            // while the game is away (frame time is clamped by the clock), so the wait goes on when it comes back.
            _flow.Lose();
            _clock.Step(10);

            _flow.Pause();
            Assert.AreEqual(0, _clock.QueuedCount, "The pause request is dropped, not even queued.");
            _clock.Step(0);
            Assert.AreEqual(RunStep.ReviveWait, _flow.Step, "Time away from the game ran no step.");

            _clock.Step(30);
            Assert.AreEqual(RunStep.ReviveOffer, _flow.Step, "Back in the game, the revive popup opens.");
            Assert.IsFalse(_flow.IsPaused && _flow.Step == RunStep.Paused);
        }

        [Test]
        public void OfferFlag_IsOnFromTheFirstFallAndOffOnlyWhenANewRunStarts()
        {
            // #offer-flag
            Assert.IsFalse(_flow.ReviveOffered);
            FallAndOffer();
            Assert.IsTrue(_flow.ReviveOffered);

            ReviveAndShow();
            Assert.IsTrue(_flow.ReviveOffered, "The revive does not turn the flag off.");

            _flow.Lose();
            _clock.Step(40);
            Assert.AreEqual(RunStep.Lost, _flow.Step);
            Assert.IsTrue(_flow.ReviveOffered);

            _flow.Replay();
            _flow.BuildBattle(Team);
            Assert.IsFalse(_flow.ReviveOffered, "A new run starts with the flag off.");
        }

        [Test]
        public void EdgeSecondFallRightAfterTheRevive_GoesToEndingThenTheLosePopupAfterOneSecond()
        {
            // #edge-thanh-bi-pha-lan-hai-ngay-sau-khi
            FallAndOffer();
            ReviveAndShow();
            _clock.Step(2);
            _log.Clear();

            _flow.Lose();

            Assert.AreEqual(RunStep.Ending, _flow.Step, "No second offer.");
            CollectionAssert.Contains(_log, "BattleStopped");
            _clock.Step(39);
            Assert.AreEqual(RunStep.Ending, _flow.Step);
            _clock.Step(1);
            Assert.AreEqual(RunStep.Lost, _flow.Step);
            Assert.IsFalse(_flow.LostAfterFailedRevive);
        }

        [Test]
        public void FailToLoseNow_EndRunSettlesTheScoreAndShowsTheLosePopupAtOnce()
        {
            // #fail-to-lose-now: End run on the revive popup.
            _flow.CountClearedWave(1);
            _clock.Step(1);
            FallAndOffer();

            _flow.DeclineRevive();

            Assert.AreEqual(RunStep.Lost, _flow.Step, "No further wait.");
            Assert.AreEqual(1, _store.Saved, "The score was settled and the new best saved.");
            Assert.IsFalse(_flow.LostAfterFailedRevive);
        }

        [Test]
        public void FailToLoseNow_AdNotWatchedSettlesTheScoreAndShowsTheLosePopupAtOnce()
        {
            // #fail-to-lose-now: the ad failed.
            _flow.CountClearedWave(1);
            _clock.Step(1);
            FallAndOffer();

            _flow.FailRevive();

            Assert.AreEqual(RunStep.Lost, _flow.Step);
            Assert.AreEqual(1, _store.Saved);
            Assert.IsTrue(_flow.LostAfterFailedRevive, "The lose popup says the ad was not watched.");
        }

        [Test]
        public void ResumeAfterAnim_TheBattleRunsAgainTheMomentTheCastleIsShownWithNoCountdown()
        {
            // #resume-after-anim
            FallAndOffer();
            _flow.Revive();
            Assert.AreEqual(RunStep.Reviving, _flow.Step);
            Assert.IsFalse(_clock.Gameplay, "Still standing still while the castle grows back.");

            _flow.ReviveShown();

            Assert.AreEqual(RunStep.Playing, _flow.Step);
            Assert.IsTrue(_clock.Gameplay, "Gameplay is on at once.");
            var before = _clock.GameplayStepCount;
            _clock.Step(1);
            Assert.AreEqual(before + 1, _clock.GameplayStepCount, "The very next step is a gameplay step.");
        }

        [Test]
        public void HeldClear_WaveClearedInTheStepOfTheFirstFallCountsOnlyAfterTheRevive()
        {
            // #held-clear
            _clock.OnStep("Wave", step => { if (step.Number == 1) _flow.CountClearedWave(1); });
            _clock.OnStep("Combat", step => { if (step.Number == 1) _flow.Lose(); });

            // The castle fell inside step 1, so the 1 s wait counts steps 2 to 41.
            _clock.Step(41);
            Assert.AreEqual(RunStep.ReviveOffer, _flow.Step);
            Assert.AreEqual(0, _score.Score, "Held while the revive is decided.");

            ReviveAndShow();

            Assert.AreEqual(1, _score.Score, "Counted once the run goes on.");
        }

        [Test]
        public void HeldClear_WaveClearedInTheStepOfTheFirstFallIsDroppedWhenTheRunIsLost()
        {
            // #held-clear
            _clock.OnStep("Wave", step => { if (step.Number == 1) _flow.CountClearedWave(1); });
            _clock.OnStep("Combat", step => { if (step.Number == 1) _flow.Lose(); });
            _clock.Step(41);
            Assert.AreEqual(RunStep.ReviveOffer, _flow.Step);

            _flow.DeclineRevive();

            Assert.AreEqual(0, _score.Score);
            Assert.AreEqual(RunStep.Lost, _flow.Step);
        }

        [Test]
        public void EdgeMoneyFromKillsInTheStepOfTheFall_TheFirstFallNeitherStopsNorClearsTheBattle()
        {
            // #edge-tien-ha-quai-toi-cung-khung-hinh-thanh: kills resolved earlier in the step keep their money; the
            // run only stands the battle still from the next step and tells no one to stop or clear it.
            var gameplayStepsBefore = _clock.GameplayStepCount;
            _clock.OnStep("Combat", step => { if (step.Number == 1) _flow.Lose(); });

            _clock.Step(1);

            Assert.AreEqual(gameplayStepsBefore + 1, _clock.GameplayStepCount, "The step of the fall was a full gameplay step.");
            CollectionAssert.DoesNotContain(_log, "BattleStopped");
            CollectionAssert.DoesNotContain(_log, "BattleCleared");
            Assert.IsFalse(_clock.Gameplay, "From the next step nothing hits any more.");
        }

        /// <summary>
        /// The shared clock of feature-sim-clock, faked: owners in the step order of its config, queued actions at
        /// the start of each step, every step 0.025 s, step count up before the callbacks.
        /// </summary>
        private sealed class FakeSimClock : ISimClock, ISimRunControl
        {
            private static readonly string[] Order = { "Wave", "Enemy", "Hero", "Castle", "Combat", "Run" };

            private readonly List<(string owner, Action<SimStep> onStep)> _onStep = new List<(string, Action<SimStep>)>();
            private readonly List<Action> _queue = new List<Action>();

            public bool Gameplay { get; private set; }
            public int QueuedCount => _queue.Count;

            public SimMode Mode => SimMode.FixedTick;
            public float StepSeconds => 0.025f;
            public long StepCount { get; private set; }
            public long GameplayStepCount { get; private set; }
            public double GameplaySeconds => GameplayStepCount * 0.025;
            public int Seed => 5;

            public IDisposable OnStep(string owner, Action<SimStep> onStep)
            {
                if (Array.IndexOf(Order, owner) < 0) throw new ArgumentException($"'{owner}' is not in the step order.");
                var entry = (owner, onStep);
                _onStep.Add(entry);
                return new Cancel(() => _onStep.Remove(entry));
            }

            public void Enqueue(string owner, Action apply) => _queue.Add(apply);

            public void Step(int count)
            {
                for (var i = 0; i < count; i++)
                {
                    StepCount++;
                    var queued = _queue.ToArray();
                    _queue.Clear();
                    foreach (var apply in queued) apply();
                    if (Gameplay) GameplayStepCount++;
                    var step = new SimStep(StepCount, GameplayStepCount, 0.025f, Gameplay);
                    foreach (var owner in Order)
                    foreach (var s in _onStep.Where(s => s.owner == owner).ToArray())
                        s.onStep(step);
                }
            }

            public void SetUpRun() { StepCount = 0; GameplayStepCount = 0; _queue.Clear(); }
            public void StartRun() { }
            public void SetGameplay(bool playing) => Gameplay = playing;
            public void ClearRun() => _queue.Clear();

            private sealed class Cancel : IDisposable
            {
                private Action _undo;
                public Cancel(Action undo) => _undo = undo;
                public void Dispose() { _undo?.Invoke(); _undo = null; }
            }
        }

        private sealed class FakeStore : IBestScoreStore
        {
            public int? Saved;
            public int Load() => 0;
            public void Save(int best) => Saved = best;
        }
    }
}
