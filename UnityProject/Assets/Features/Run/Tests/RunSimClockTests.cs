using System;
using System.Collections.Generic;
using System.Linq;
using Game.SimClock;
using NUnit.Framework;

namespace Game.Run.Tests
{
    /// <summary>
    /// The run on the shared clock of feature-sim-clock (init-on-run, first-step-on-start, continue-after-revive,
    /// clear-on-dispose, input-next-step, timer-in-steps): when the run calls the clock, what goes through its queue,
    /// and the run's waits counted in clock steps at the run's place in the step order.
    /// </summary>
    public class RunSimClockTests
    {
        private static readonly RunTeam Team = new RunTeam(new[] { "knight", "axe", "archer" });

        private List<string> _log;
        private FakeSimClock _clock;
        private SimClockRunDelay _waits;
        private RunScore _score;

        [SetUp]
        public void SetUp()
        {
            _log = new List<string>();
            _clock = new FakeSimClock(_log);
            _waits = new SimClockRunDelay(_clock);
            // As the scene session does when it runs.
            _waits.Listen();
            _score = new RunScore(new FakeStore());
        }

        [TearDown]
        public void TearDown() => _waits.Dispose();

        private RunFlow Flow(bool skipMenus = false, bool reviveOnce = false)
        {
            var flow = new RunFlow(_score, _waits, 1f, 3, skipMenus, skipMenus ? Team : default, reviveOnce,
                pauseEnabled: true, clock: new SimRunClock(_clock, _clock));
            flow.StepChanged += s => _log.Add("step:" + s);
            flow.RunStarted += _ => _log.Add("RunStarted");
            flow.BattleStarted += () => _log.Add("BattleStarted");
            flow.BattleStopped += () => _log.Add("BattleStopped");
            flow.BattleCleared += () => _log.Add("BattleCleared");
            return flow;
        }

        private RunFlow InBattle(bool reviveOnce = false)
        {
            var flow = Flow(reviveOnce: reviveOnce);
            flow.Play();
            flow.EnterBattle(Team);
            _log.Clear();
            return flow;
        }

        [Test]
        public void EnteringBattleSetsUpTheClockBeforeAnyoneHearsOfTheRunAndStartsItOnceTheBattleIsBuilt()
        {
            var flow = Flow();
            flow.Play();
            _log.Clear();

            flow.EnterBattle(Team);

            CollectionAssert.AreEqual(
                new[] { "clock:SetUpRun", "clock:Gameplay:True", "step:Playing", "RunStarted", "BattleStarted", "clock:StartRun" },
                _log);
        }

        [Test]
        public void ThereIsNoClockRunOnTheStartScreenOrInTeamSelect()
        {
            var flow = Flow();
            flow.Play();

            CollectionAssert.IsEmpty(_log.Where(l => l.StartsWith("clock:")));
        }

        [Test]
        public void PauseGoesThroughTheClockQueueAndAppliesAtTheStartOfTheNextStep()
        {
            var flow = InBattle();

            flow.Pause();
            Assert.AreEqual(RunStep.Playing, flow.Step, "The press only queues the pause.");
            Assert.AreEqual(1, _clock.QueuedCount);
            Assert.AreEqual("Run", _clock.QueuedOwners.Single());

            _clock.Step(1);

            Assert.AreEqual(RunStep.Paused, flow.Step);
            Assert.IsTrue(flow.IsPaused);
            Assert.IsFalse(_clock.Gameplay, "The battle stands still on the steps after the pause.");
        }

        [Test]
        public void ContinueGoesThroughTheClockQueueAndTurnsTheBattleBackOn()
        {
            var flow = InBattle();
            flow.Pause();
            _clock.Step(1);

            flow.Resume();
            Assert.AreEqual(RunStep.Paused, flow.Step, "The press only queues the continue.");
            _clock.Step(1);

            Assert.AreEqual(RunStep.Playing, flow.Step);
            Assert.IsTrue(_clock.Gameplay);
        }

        [Test]
        public void TwoPausesQueuedInOneFramePauseOnce()
        {
            var flow = InBattle();
            flow.Pause();
            flow.Pause();

            _clock.Step(1);

            Assert.AreEqual(RunStep.Paused, flow.Step);
            Assert.AreEqual(1, _log.Count(l => l == "step:Paused"));
        }

        [Test]
        public void PauseQueuedJustBeforeTheCastleFallsIsIgnoredWhenItsStepComes()
        {
            var flow = InBattle();
            flow.Pause();
            flow.Lose();

            _clock.Step(1);

            Assert.AreEqual(RunStep.Ending, flow.Step);
        }

        [Test]
        public void LosingStopsTheBattleOnTheClockAndTheLosePopupWaitsOneSecondOfSteps()
        {
            var flow = InBattle();

            flow.Lose();
            Assert.IsFalse(_clock.Gameplay, "Nothing of the battle moves while waiting for the lose popup.");

            _clock.Step(39);
            Assert.AreEqual(RunStep.Ending, flow.Step, "39 steps of 0.025 s are 0.975 s.");
            _clock.Step(1);
            Assert.AreEqual(RunStep.Lost, flow.Step, "The 40th step is 1 s after the castle fell.");
        }

        [Test]
        public void TheRunWaitsAtItsOwnPlaceInTheStepOrder()
        {
            var flow = InBattle();
            flow.Lose();

            CollectionAssert.AreEqual(new[] { "Run" }, _clock.Owners);
        }

        [Test]
        public void LosePopupWaitStandsStillWhenTheClockRunsNoStep()
        {
            var flow = InBattle();
            flow.Lose();

            // No step for a long time (the clock is frozen, or frames are skipped): the wait does not end on its own.
            _clock.Step(0);

            Assert.AreEqual(RunStep.Ending, flow.Step);
        }

        [Test]
        public void WaveClearedEarlierInAStepCountsAtTheRunsPlaceInThatStep()
        {
            var flow = InBattle();
            _clock.OnStep("Wave", step => { if (step.Number == 1) flow.CountClearedWave(1); });

            _clock.Step(1);

            Assert.AreEqual(1, _score.Score);
        }

        [Test]
        public void CastleFallingLaterInTheSameStepAsAWaveClearDropsThatWave()
        {
            var flow = InBattle();
            _clock.OnStep("Wave", step => { if (step.Number == 1) flow.CountClearedWave(1); });
            _clock.OnStep("Combat", step => { if (step.Number == 1) flow.Lose(); });

            _clock.Step(1);

            Assert.AreEqual(0, _score.Score);
            Assert.AreEqual(RunStep.Ending, flow.Step);
        }

        [Test]
        public void WaitingForTheReviveStopsTheBattleAndTheReviveGoesOnWithoutResettingTheClock()
        {
            var flow = InBattle(reviveOnce: true);

            flow.Lose();
            Assert.AreEqual(RunStep.ReviveWait, flow.Step);
            Assert.IsFalse(_clock.Gameplay);
            _clock.Step(40);
            Assert.AreEqual(RunStep.ReviveOffer, flow.Step, "The revive popup shows after 1 s of clock steps.");

            flow.Revive();
            flow.ReviveShown();

            Assert.AreEqual(RunStep.Playing, flow.Step);
            Assert.IsTrue(_clock.Gameplay, "The battle plays on from where it stopped.");
            CollectionAssert.DoesNotContain(_log, "clock:SetUpRun", "A revive keeps the run's steps and streams.");
            CollectionAssert.DoesNotContain(_log, "clock:ClearRun");
        }

        [Test]
        public void PlayAgainClearsTheClockBeforeTheBattleIsReportedClearedAndSetsUpTheNextRun()
        {
            var flow = Flow(skipMenus: true);
            flow.Begin();
            flow.Lose();
            _clock.Step(40);
            _log.Clear();

            flow.Replay();

            var clear = _log.IndexOf("clock:ClearRun");
            var cleared = _log.IndexOf("BattleCleared");
            var setUp = _log.IndexOf("clock:SetUpRun");
            Assert.That(clear, Is.GreaterThanOrEqualTo(0));
            Assert.That(clear, Is.LessThan(cleared), "The clock is cleared before the battle is reported cleared.");
            Assert.That(setUp, Is.GreaterThan(cleared), "The next run is set up after the old one is cleared.");
            Assert.AreEqual("clock:StartRun", _log.Last());
        }

        [Test]
        public void BackToStartClearsTheClockBeforeTheBattleIsReportedCleared()
        {
            var flow = InBattle();
            flow.Lose();
            _clock.Step(40);
            _log.Clear();

            flow.GoHome();

            CollectionAssert.AreEqual(new[] { "clock:ClearRun", "BattleCleared", "step:Home" }, _log);
        }

        [Test]
        public void GivingUpClearsTheClockBeforeTheBattleIsReportedCleared()
        {
            var flow = InBattle();
            flow.Pause();
            _clock.Step(1);
            _log.Clear();

            flow.Abandon();

            CollectionAssert.AreEqual(new[] { "BattleStopped", "clock:ClearRun", "BattleCleared", "step:Home" }, _log);
        }

        [Test]
        public void EndingTheSessionClearsTheClockRunAndStopsWaitingOnTheClock()
        {
            var flow = InBattle();
            flow.Lose();

            flow.Shutdown();
            _waits.Dispose();
            _clock.Step(40);

            CollectionAssert.Contains(_log, "clock:ClearRun");
            Assert.AreEqual(0, _clock.Listening, "The run no longer listens to the clock.");
            Assert.AreEqual(RunStep.Ending, flow.Step, "The dropped lose popup does not show.");
        }

        [Test]
        public void EndingTheSessionOnTheStartScreenDoesNotClearAClockRunThatNeverStarted()
        {
            var flow = Flow();

            flow.Shutdown();

            CollectionAssert.DoesNotContain(_log, "clock:ClearRun");
        }

        /// <summary>
        /// The shared clock of feature-sim-clock, faked: owners called in the step order of its config, queued
        /// actions applied at the start of each step, every step 0.025 s; the step count goes up before the callbacks.
        /// </summary>
        private sealed class FakeSimClock : ISimClock, ISimRunControl
        {
            private static readonly string[] Order = { "Wave", "Enemy", "Hero", "Castle", "Combat", "Run" };

            private readonly List<string> _log;
            private readonly List<(string owner, Action<SimStep> onStep)> _onStep = new List<(string, Action<SimStep>)>();
            private readonly List<(string owner, Action apply)> _queue = new List<(string, Action)>();

            public FakeSimClock(List<string> log) => _log = log;

            public bool Gameplay { get; private set; }
            public int QueuedCount => _queue.Count;
            public IEnumerable<string> QueuedOwners => _queue.Select(q => q.owner);
            public int Listening => _onStep.Count(s => s.owner == "Run");
            public List<string> Owners { get; } = new List<string>();

            public SimMode Mode => SimMode.FixedTick;
            public float StepSeconds => 0.025f;
            public long StepCount { get; private set; }
            public long GameplayStepCount { get; private set; }
            public double GameplaySeconds => GameplayStepCount * 0.025;
            public int Seed => 7;

            public IDisposable OnStep(string owner, Action<SimStep> onStep)
            {
                if (Array.IndexOf(Order, owner) < 0) throw new ArgumentException($"'{owner}' is not in the step order.");
                if (owner == "Run") Owners.Add(owner);
                var entry = (owner, onStep);
                _onStep.Add(entry);
                return new Unsubscribe(() => _onStep.Remove(entry));
            }

            public void Enqueue(string owner, Action apply) => _queue.Add((owner, apply));

            public void Step(int count)
            {
                for (var i = 0; i < count; i++)
                {
                    StepCount++;
                    var queued = _queue.ToArray();
                    _queue.Clear();
                    foreach (var q in queued) q.apply();
                    if (Gameplay) GameplayStepCount++;
                    var step = new SimStep(StepCount, GameplayStepCount, 0.025f, Gameplay);
                    foreach (var owner in Order)
                    foreach (var s in _onStep.Where(s => s.owner == owner).ToArray())
                        s.onStep(step);
                }
            }

            public void SetUpRun()
            {
                _log.Add("clock:SetUpRun");
                StepCount = 0;
                GameplayStepCount = 0;
                _queue.Clear();
            }

            public void StartRun() => _log.Add("clock:StartRun");

            public void SetGameplay(bool playing)
            {
                Gameplay = playing;
                _log.Add("clock:Gameplay:" + playing);
            }

            public void ClearRun()
            {
                _log.Add("clock:ClearRun");
                _queue.Clear();
            }

            private sealed class Unsubscribe : IDisposable
            {
                private Action _undo;
                public Unsubscribe(Action undo) => _undo = undo;
                public void Dispose() { _undo?.Invoke(); _undo = null; }
            }
        }

        private sealed class FakeStore : IBestScoreStore
        {
            public int Load() => 0;
            public void Save(int best) { }
        }
    }
}
