using System;
using System.Collections.Generic;
using NUnit.Framework;
using static Game.SimClock.Tests.SimClockTestKit;

namespace Game.SimClock.Tests
{
    /// <summary>live: feature-sim-clock/clock-step/clock-values (ideas and edge cases).</summary>
    public class ClockStepTests
    {
        private SimClockConfig _config;

        [SetUp]
        public void SetUp() => _config = NewConfig();

        [TearDown]
        public void TearDown() => Destroy(_config);

        [Test]
        public void Step_Lasts0025Seconds_And40StepsMakeOneSecond()
        {
            var clock = StartedClock(_config);
            var lengths = new List<float>();
            clock.OnStep("Wave", s => lengths.Add(s.Seconds));
            for (var i = 0; i < 8; i++) clock.Advance(0.125);
            Assert.That(clock.StepCount, Is.EqualTo(40));
            Assert.That(clock.GameplaySeconds, Is.EqualTo(1.0).Within(1e-9));
            Assert.That(lengths, Has.All.EqualTo(0.025f));
        }

        [Test]
        public void DefaultMode_IsFixedTick()
        {
            var clock = StartedClock(_config);
            Assert.That(clock.Mode, Is.EqualTo(SimMode.FixedTick));
        }

        [Test]
        public void FrameMode_IsChosenByConfigOnly()
        {
            _config.mode = SimMode.Frame;
            var clock = StartedClock(_config);
            Assert.That(clock.Mode, Is.EqualTo(SimMode.Frame));
            Assert.That(clock.Advance(0.016), Is.EqualTo(1));
        }

        [Test]
        public void LongFrame_RunsAtMost5Steps_AndDropsTheRest()
        {
            var clock = StartedClock(_config);
            Assert.That(clock.Advance(0.5), Is.EqualTo(5));
            Assert.That(clock.Leftover, Is.LessThan(0.025));
            Assert.That(clock.Advance(0.0), Is.EqualTo(0), "the dropped time never comes back as a jump");
        }

        [Test]
        public void Frame100ms_StillKeepsUpWith4Steps()
        {
            var clock = StartedClock(_config);
            Assert.That(clock.Advance(0.1), Is.EqualTo(4));
        }

        [Test]
        public void FrameMode_OneStepAsLongAsTheFrame_ClampedAt0125()
        {
            _config.mode = SimMode.Frame;
            var clock = StartedClock(_config);
            var lengths = new List<float>();
            clock.OnStep("Wave", s => lengths.Add(s.Seconds));
            clock.Advance(0.04);
            clock.Advance(2.0);
            Assert.That(lengths, Is.EqualTo(new[] { 0.04f, 0.125f }));
            Assert.That(clock.GameplaySeconds, Is.EqualTo(0.165).Within(1e-6));
        }

        [Test]
        public void StepCounts_AreWholeNumbers_GameplayStepsStopAfterRunEnded()
        {
            var clock = StartedClock(_config);
            clock.Advance(0.1);
            clock.SetGameplay(false);
            clock.Advance(0.1);
            Assert.That(clock.StepCount, Is.EqualTo(8));
            Assert.That(clock.GameplayStepCount, Is.EqualTo(4));
            Assert.That(clock.GameplaySeconds, Is.EqualTo(4 * 0.025).Within(1e-12));
        }

        [Test]
        public void BattleTime_DoesNotDriftAfterManySteps()
        {
            var clock = StartedClock(_config);
            for (var i = 0; i < 144000; i++) clock.Advance(0.025);
            Assert.That(clock.GameplayStepCount, Is.EqualTo(144000));
            Assert.That(clock.GameplaySeconds, Is.EqualTo(3600.0).Within(1e-9));
        }

        [TestCase(20.0, 800)]
        [TestCase(0.7, 28)]
        public void Wait_EndsOnFirstStepReachingTheTime(double seconds, int steps)
        {
            var clock = StartedClock(_config);
            var start = clock.GameplaySeconds;
            long endedAt = -1;
            clock.OnStep("Castle", s =>
            {
                if (endedAt < 0 && clock.GameplaySeconds - start >= seconds) endedAt = s.GameplayNumber;
            });
            for (var i = 0; i < steps + 10; i++) clock.Advance(0.025);
            Assert.That(endedAt, Is.EqualTo(steps));
        }

        [Test]
        public void PlayerAction_IsAppliedAtStartOfNextStep_InOrderQueued()
        {
            var clock = StartedClock(_config);
            var log = new List<string>();
            clock.OnStep("Wave", s => log.Add("wave " + s.Number));
            clock.Enqueue("Upgrade", () => log.Add("buy"));
            clock.Enqueue("Castle", () => log.Add("skill"));
            Assert.That(log, Is.Empty, "nothing changes when pressed");
            clock.Advance(0.025);
            Assert.That(log, Is.EqualTo(new[] { "buy", "skill", "wave 1" }));
            Assert.That(clock.Applied[0].Step, Is.EqualTo(1));
            Assert.That(clock.Applied[1].Owner, Is.EqualTo("Castle"));
        }

        [Test]
        public void PlayerAction_WaitsWhenTheFrameRunsNoStep()
        {
            var clock = StartedClock(_config);
            var applied = 0;
            clock.Enqueue("Upgrade", () => applied++);
            clock.Advance(0.01);
            Assert.That(applied, Is.EqualTo(0));
            clock.Advance(0.02);
            Assert.That(applied, Is.EqualTo(1));
            Assert.That(clock.Applied[0].Step, Is.EqualTo(1));
        }

        [Test]
        public void PlayerAction_IsAppliedOnceEvenWhenTheFrameRunsSeveralSteps()
        {
            var clock = StartedClock(_config);
            var applied = new List<long>();
            clock.Enqueue("Upgrade", () => applied.Add(clock.StepCount));
            clock.Advance(0.1);
            Assert.That(applied, Is.EqualTo(new[] { 1L }));
        }

        [Test]
        public void Owners_RunInConfigOrder_NotInSubscribeOrder()
        {
            var clock = StartedClock(_config);
            var log = new List<string>();
            foreach (var owner in new[] { "Run", "Combat", "Hero", "Wave", "Castle", "Enemy" })
                clock.OnStep(owner, _ => log.Add(owner));
            clock.Advance(0.025);
            Assert.That(log, Is.EqualTo(new[] { "Wave", "Enemy", "Hero", "Castle", "Combat", "Run" }));
        }

        [Test]
        public void Owner_MissingFromStepOrder_IsAnError()
        {
            var clock = StartedClock(_config);
            Assert.Throws<ArgumentException>(() => clock.OnStep("Nobody", _ => { }));
        }

        [Test]
        public void DisposedSubscription_StopsBeingCalled()
        {
            var clock = StartedClock(_config);
            var calls = 0;
            var handle = clock.OnStep("Hero", _ => calls++);
            clock.Advance(0.025);
            handle.Dispose();
            clock.Advance(0.025);
            Assert.That(calls, Is.EqualTo(1));
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        [TestCase(1, 5)]
        [TestCase(2, 10)]
        [TestCase(4, 20)]
        [TestCase(8, 40)]
        [TestCase(16, 80)]
        public void DevSpeed_MultipliesTheStepCapOfAFrame(int speed, int steps)
        {
            var clock = StartedClock(_config);
            clock.SetSpeed(speed);
            Assert.That(clock.Advance(0.125), Is.EqualTo(steps));
        }

        [Test]
        public void DevSpeed_OutsideTheList_IsRefused()
        {
            var clock = StartedClock(_config);
            Assert.Throws<ArgumentOutOfRangeException>(() => clock.SetSpeed(3));
        }

        [Test]
        public void DevRunSteps_RunsNStepsAtOnce_LikeRealTime()
        {
            var fast = StartedClock(_config);
            var slow = StartedClock(_config);
            var fastSeen = new List<long>();
            var slowSeen = new List<long>();
            fast.OnStep("Wave", s => fastSeen.Add(s.GameplayNumber));
            slow.OnStep("Wave", s => slowSeen.Add(s.GameplayNumber));
            fast.Enqueue("Castle", () => { });
            slow.Enqueue("Castle", () => { });
            fast.RunSteps(120);
            for (var i = 0; i < 120; i++) slow.Advance(0.025);
            Assert.That(fastSeen, Is.EqualTo(slowSeen));
            Assert.That(fast.GameplaySeconds, Is.EqualTo(slow.GameplaySeconds));
            Assert.That(fast.Applied[0].Step, Is.EqualTo(slow.Applied[0].Step));
        }

        [Test]
        public void DevFreeze_StopsFrames_AndStepOnceRunsOneStep()
        {
            var clock = StartedClock(_config);
            clock.Freeze(true);
            Assert.That(clock.Advance(0.125), Is.EqualTo(0));
            clock.StepOnce();
            Assert.That(clock.StepCount, Is.EqualTo(1));
            clock.Freeze(false);
            Assert.That(clock.Advance(0.025), Is.EqualTo(1));
        }
#endif

        [Test]
        public void View_SeesTheStateAfterTheLastStepOfTheFrame()
        {
            var clock = StartedClock(_config);
            long lastSeen = 0;
            clock.OnStep("Run", s => lastSeen = s.Number);
            clock.Advance(0.1);
            Assert.That(lastSeen, Is.EqualTo(4), "every step of the frame ran before Advance returned");
            Assert.That(clock.StepCount, Is.EqualTo(lastSeen));
        }

        // ---------------------------------------------------------------- edge cases

        [Test]
        public void Edge_FirstFrameAfterLongSceneLoad_IsClampedTo5Steps()
        {
            var clock = StartedClock(_config);
            Assert.That(clock.Advance(4.0), Is.EqualTo(5));
            Assert.That(clock.GameplaySeconds, Is.EqualTo(0.125).Within(1e-9));
        }

        [Test]
        public void Edge_GameLeftAndBack_StepsRunButGameplayStandsStill()
        {
            var clock = StartedClock(_config);
            clock.SetGameplay(false);
            Assert.That(clock.Advance(30.0), Is.EqualTo(5));
            Assert.That(clock.GameplayStepCount, Is.EqualTo(0));
            Assert.That(clock.GameplaySeconds, Is.EqualTo(0));
        }

        [Test]
        public void Edge_FrameShorterThanAStep_KeepsTheTimeForNextFrame()
        {
            var clock = StartedClock(_config);
            Assert.That(clock.Advance(0.015), Is.EqualTo(0));
            Assert.That(clock.Leftover, Is.EqualTo(0.015).Within(1e-9));
            Assert.That(clock.Advance(0.015), Is.EqualTo(1));
            Assert.That(clock.Leftover, Is.EqualTo(0.005).Within(1e-9));
        }

        [Test]
        public void Edge_ActionAfterRunEnded_ReachesTheFeatureWhichRefusesIt()
        {
            var clock = StartedClock(_config);
            var runOver = false;
            var refused = 0;
            var accepted = 0;
            clock.OnStep("Run", _ =>
            {
                runOver = true;
                clock.SetGameplay(false);
            });
            clock.Advance(0.025);
            clock.Enqueue("Castle", () =>
            {
                if (runOver) refused++;
                else accepted++;
            });
            clock.Advance(0.025);
            Assert.That(refused, Is.EqualTo(1));
            Assert.That(accepted, Is.EqualTo(0));
        }

        [Test]
        public void Edge_ModeChangedWhilePlaying_IsReadOnlyAtNextRun()
        {
            var clock = StartedClock(_config);
            _config.mode = SimMode.Frame;
            Assert.That(clock.Mode, Is.EqualTo(SimMode.FixedTick));
            Assert.That(clock.Advance(0.1), Is.EqualTo(4));
            clock.ClearRun();
            clock.SetUpRun();
            Assert.That(clock.Mode, Is.EqualTo(SimMode.Frame));
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        [Test]
        public void Edge_Speed16OnSlowMachine_ClampsFrameAndRuns80Steps_SameResult()
        {
            var fast = StartedClock(_config);
            fast.SetSpeed(16);
            Assert.That(fast.Advance(1.5), Is.EqualTo(80));
            var normal = StartedClock(_config);
            for (var i = 0; i < 80; i++) normal.Advance(0.025);
            Assert.That(fast.GameplaySeconds, Is.EqualTo(normal.GameplaySeconds));
        }

        [Test]
        public void Edge_RunNStepsAfterRunEnded_StepsRunGameplayStill()
        {
            var clock = StartedClock(_config);
            clock.SetGameplay(false);
            var gameplaySteps = 0;
            clock.OnStep("Hero", s => { if (s.Gameplay) gameplaySteps++; });
            clock.RunSteps(50);
            Assert.That(clock.StepCount, Is.EqualTo(50));
            Assert.That(clock.GameplayStepCount, Is.EqualTo(0));
            Assert.That(gameplaySteps, Is.EqualTo(0));
        }
#endif

        [Test]
        public void Edge_FrameMode_StepsFollowFrameLengths()
        {
            _config.mode = SimMode.Frame;
            var clock = StartedClock(_config);
            clock.Advance(0.016);
            clock.Advance(0.033);
            Assert.That(clock.StepCount, Is.EqualTo(2));
            Assert.That(clock.GameplaySeconds, Is.EqualTo(0.049).Within(1e-6));
        }

        [Test]
        public void NoStep_RunsBeforeTheRunStarted()
        {
            var clock = NewClock(_config, true, out _, out _);
            clock.SetUpRun();
            Assert.That(clock.Advance(0.1), Is.EqualTo(0));
            clock.StartRun();
            Assert.That(clock.Advance(0.1), Is.EqualTo(4));
        }

        [Test]
        public void ClearRun_DropsQueuedActionsAndLeftover()
        {
            var clock = StartedClock(_config);
            var applied = 0;
            clock.Advance(0.01);
            clock.Enqueue("Upgrade", () => applied++);
            clock.ClearRun();
            Assert.That(clock.Leftover, Is.EqualTo(0));
            Assert.That(clock.Advance(0.1), Is.EqualTo(0));
            clock.SetUpRun();
            clock.StartRun();
            clock.Advance(0.025);
            Assert.That(applied, Is.EqualTo(0));
            Assert.That(clock.StepCount, Is.EqualTo(1));
        }
    }
}
