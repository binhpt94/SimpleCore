using Game.Run;
using NUnit.Framework;

namespace Game.Combat.Tests
{
    public class CombatLoopTests
    {
        private static readonly CombatSettings Settings = new CombatSettings(0.02f, 5);

        private FakeRunFlow _run;
        private FakeTime _time;
        private FakeActor _actor;
        private CombatTick _tick;
        private CombatLoop _loop;

        [SetUp]
        public void SetUp()
        {
            _run = new FakeRunFlow();
            _time = new FakeTime();
            _tick = new CombatTick();
            _actor = new FakeActor("Actor");
            _tick.AddActor(_actor);
            _loop = new CombatLoop(_tick, new FixedStepClock(Settings), _run, _time);
            _run.StartRun();
            _run.StartBattle();
        }

        [TearDown]
        public void TearDown() => _loop.Dispose();

        [Test]
        public void BattleRunsFiftyTicksEachSecondOfTwoHundredthsEach()
        {
            _time.DeltaTime = 0.02f;
            var ticks = 0;
            for (var frame = 0; frame < 50; frame++) ticks += _loop.Frame();

            Assert.AreEqual(50, ticks);
            Assert.That(_actor.Steps, Is.All.EqualTo(0.02f));
        }

        [Test]
        public void FastFramesWaitUntilAWholeStepHasPassed()
        {
            _time.DeltaTime = 0.01f;

            Assert.AreEqual(0, _loop.Frame());
            Assert.AreEqual(1, _loop.Frame());
        }

        [Test]
        public void SlowFrameRunsSeveralTicks()
        {
            _time.DeltaTime = 0.1f;

            Assert.AreEqual(5, _loop.Frame());
        }

        [Test]
        public void VerySlowFrameRunsAtMostFiveTicksAndTheBattleSlowsDown()
        {
            _time.DeltaTime = 0.3f;
            Assert.AreEqual(5, _loop.Frame());

            _time.DeltaTime = 0.02f;
            Assert.AreEqual(1, _loop.Frame(), "the time beyond five steps is dropped, not run later");
        }

        [Test]
        public void SameFrameTimesGiveTheSameTicksOnFastAndSlowDevices()
        {
            var fast = new FixedStepClock(Settings);
            var slow = new FixedStepClock(Settings);
            var fastSteps = 0;
            var slowSteps = 0;
            for (var i = 0; i < 60; i++) fastSteps += fast.Advance(1f / 60f);
            for (var i = 0; i < 30; i++) slowSteps += slow.Advance(1f / 30f);

            Assert.AreEqual(50, fastSteps);
            Assert.AreEqual(50, slowSteps);
        }

        [Test]
        public void NoTickRunsWhilePaused()
        {
            _time.DeltaTime = 0.1f;
            _run.Pause();

            Assert.AreEqual(0, _loop.Frame());
            Assert.IsEmpty(_actor.Steps);

            _run.Resume();
            Assert.AreEqual(5, _loop.Frame());
        }

        [TestCase(RunStep.ReviveWait)]
        [TestCase(RunStep.ReviveOffer)]
        [TestCase(RunStep.Reviving)]
        public void NoTickRunsWhileWaitingForARevive(RunStep step)
        {
            _time.DeltaTime = 0.1f;
            _run.SetStep(step);

            Assert.AreEqual(0, _loop.Frame());
        }

        [Test]
        public void NoTickRunsAfterTheRunEnded()
        {
            _time.DeltaTime = 0.1f;
            _run.StopBattle();

            Assert.AreEqual(0, _loop.Frame());

            _run.ClearBattle();
            Assert.AreEqual(0, _loop.Frame());
        }

        [Test]
        public void NoTickRunsBeforeTheBattleStarts()
        {
            _run.StartRun();
            _time.DeltaTime = 0.1f;

            Assert.AreEqual(0, _loop.Frame());
        }

        [Test]
        public void CastleFallingInATickStopsTheRestOfTheFramesTicks()
        {
            var castle = new FakeOwner("Castle");
            castle.Add(0, 1f);
            var id = _tick.AddReceiver(castle);
            castle.OnFall = (_, __) => _run.Lose();
            _tick.Hit(new CombatHit(id, 0, 5f, CombatSource.Enemy));
            _time.DeltaTime = 0.1f;

            Assert.AreEqual(1, _loop.Frame());
        }

        [Test]
        public void HitsWaitingWhenTheBattleIsClearedDoNotReachTheNextRun()
        {
            var enemies = new FakeOwner("Enemy");
            enemies.Add(1, 50f);
            var id = _tick.AddReceiver(enemies);
            _tick.Hit(new CombatHit(id, 1, 10f, CombatSource.Castle));
            _run.ClearBattle();
            _run.StartRun();
            _run.StartBattle();
            _time.DeltaTime = 0.02f;

            _loop.Frame();

            Assert.AreEqual(50f, enemies.Health(1));
        }
    }
}
