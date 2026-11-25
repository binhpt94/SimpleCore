using System.Linq;
using Game.Run;
using NUnit.Framework;

namespace Game.Combat.Tests
{
    public class CombatLoopTests
    {
        private FakeRunFlow _run;
        private FakeSimClock _clock;
        private FakeActor _actor;
        private CombatTick _tick;
        private CombatLoop _loop;

        [SetUp]
        public void SetUp()
        {
            _run = new FakeRunFlow();
            _clock = new FakeSimClock();
            _tick = new CombatTick();
            _actor = new FakeActor("Actor");
            _tick.AddActor(_actor);
            _loop = new CombatLoop(_tick, _clock, _run);
            _run.StartRun();
            _run.StartBattle();
        }

        [TearDown]
        public void TearDown() => _loop.Dispose();

        // fixed-step: one combat tick per shared step, no clock of its own.

        [Test]
        public void EachSharedStepRunsExactlyOneCombatTick()
        {
            _clock.Step(7);

            Assert.AreEqual(7, _loop.TicksRun);
            Assert.AreEqual(7, _actor.Steps.Count);
        }

        [Test]
        public void CombatStepsAtItsOwnPlaceInTheSharedStepOrder()
        {
            Assert.AreEqual(new[] { "Combat" }, _clock.Owners.ToArray());
            Assert.AreEqual("Combat", CombatLoop.Owner);
        }

        [Test]
        public void TickTakesTheStepLengthOfTheSharedClock()
        {
            _clock.Step(3);

            Assert.That(_actor.Steps, Is.All.EqualTo(0.025f));
        }

        [Test]
        public void CombatKeepsNoTimeOfItsOwnAndFollowsTheSharedStepLength()
        {
            // No frame time reaches combat: a tick runs only when the shared clock steps, as long as that step.
            Assert.AreEqual(0, _loop.TicksRun);

            _clock.StepSeconds = 0.05f;
            _clock.Step();
            _clock.StepSeconds = 0.025f;
            _clock.Step();

            Assert.AreEqual(new[] { 0.05f, 0.025f }, _actor.Steps.ToArray());
        }

        [Test]
        public void StepThatIsNotGameplayRunsNoTick()
        {
            _clock.Step(4, gameplay: false);

            Assert.AreEqual(0, _loop.TicksRun);
            Assert.IsEmpty(_actor.Steps);
        }

        [Test]
        public void DisposedLoopNoLongerTicks()
        {
            _loop.Dispose();
            _clock.Step(3);

            Assert.AreEqual(0, _clock.Subscribers);
            Assert.IsEmpty(_actor.Steps);
        }

        // params: step length and the cap of steps per frame belong to the shared clock.

        [Test]
        public void BattleRunsFortyTicksEachSecondOfTheSharedClock()
        {
            _clock.Step(40);

            Assert.AreEqual(40, _loop.TicksRun);
            Assert.AreEqual(1.0, _actor.Steps.Sum(s => (double)s), 1e-5);
        }

        // pause-no-tick

        [Test]
        public void NoTickRunsWhilePaused()
        {
            _run.Pause();
            _clock.Step(5, gameplay: false);

            Assert.AreEqual(0, _loop.TicksRun);
            Assert.IsEmpty(_actor.Steps);

            _run.Resume();
            _clock.Step(5);
            Assert.AreEqual(5, _loop.TicksRun);
        }

        [TestCase(RunStep.ReviveWait)]
        [TestCase(RunStep.ReviveOffer)]
        [TestCase(RunStep.Reviving)]
        public void NoTickRunsWhileWaitingForARevive(RunStep step)
        {
            _run.SetStep(step);
            _clock.Step(5, gameplay: false);
            Assert.AreEqual(0, _loop.TicksRun);

            // Even if a step still came marked as gameplay, the run's pause holds combat.
            _clock.Step(1);
            Assert.AreEqual(0, _loop.TicksRun);
        }

        [Test]
        public void NoTickRunsAfterTheRunEnded()
        {
            _run.StopBattle();
            _clock.Step(5);
            Assert.AreEqual(0, _loop.TicksRun);

            _run.ClearBattle();
            _clock.Step(5);
            Assert.AreEqual(0, _loop.TicksRun);
        }

        [Test]
        public void NoTickRunsBeforeTheBattleStarts()
        {
            _run.StartRun();
            _clock.Step(5);

            Assert.AreEqual(0, _loop.TicksRun);
        }

        [Test]
        public void CastleFallingInATickStopsTheRestOfTheFramesTicks()
        {
            var castle = new FakeOwner("Castle");
            castle.Add(0, 1f);
            var id = _tick.AddReceiver(castle);
            castle.OnFall = (_, __) => _run.Lose();
            _tick.Hit(new CombatHit(id, 0, 5f, CombatSource.Enemy));

            // Five steps in one frame: the castle falls in the first, the run waits for a revive.
            _clock.Step(5);

            Assert.AreEqual(1, _loop.TicksRun);
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

            _clock.Step();

            Assert.AreEqual(50f, enemies.Health(1));
        }
    }
}
