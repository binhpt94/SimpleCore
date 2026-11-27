using Game.Run;
using NUnit.Framework;

namespace Game.Combat.Tests
{
    /// <summary>
    /// One test per row of the TickParams table (feature-combat/combat-tick/tick-values), at its IDEA value (same at PROTOTYPE and FULL).
    /// step_seconds and max_steps_per_frame belong to the shared clock: combat only reads the step it is given.
    /// </summary>
    public class TickParamsTests
    {
        private const float Step = 0.025f;

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

        [Test]
        public void CombatEnabled_EverySharedStepRunsOneCombatTickThatSettlesItsHitsTogether()
        {
            // combat_enabled from IDEA on: yes, one combat tick per step of the shared clock.
            var enemies = new FakeOwner("Enemy");
            enemies.Add(1, 100f);
            var id = _tick.AddReceiver(enemies);
            _tick.Hit(new CombatHit(id, 1, 10f, CombatSource.Hero));
            _tick.Hit(new CombatHit(id, 1, 15f, CombatSource.Castle));

            Assert.AreEqual(100f, enemies.Health(1), "a hit waits for the tick, it is not taken when dealt");

            _clock.Step(3);

            Assert.AreEqual(3, _loop.TicksRun);
            CollectionAssert.AreEqual(new[] { (1, 25f) }, enemies.Totals, "both hits settled together in one total");
        }

        [Test]
        public void StepSeconds_TickIsAsLongAsTheSharedStepOfOneFortiethOfASecond()
        {
            // step_seconds from IDEA on: 0.025 s, from the shared clock.
            _clock.Step(2);

            Assert.That(_actor.Steps, Is.All.EqualTo(Step));
        }

        [Test]
        public void MaxStepsPerFrame_CombatHasNoCapOfItsOwn()
        {
            // max_steps_per_frame: kept by the shared clock. Its dev speed x16 runs up to 80 steps in one frame:
            // combat runs every one of them.
            _clock.Step(80);

            Assert.AreEqual(80, _loop.TicksRun);
        }

        [Test]
        public void DamageRounding_None_TotalsAndHealthKeepFractions()
        {
            // damage_rounding: none.
            var enemies = new FakeOwner("Enemy");
            enemies.Add(1, 10f);
            var id = _tick.AddReceiver(enemies);
            _tick.Hit(new CombatHit(id, 1, 0.3f, CombatSource.Hero));
            _tick.Hit(new CombatHit(id, 1, 0.45f, CombatSource.Hero));

            _clock.Step();

            Assert.AreEqual(0.75f, enemies.Totals[0].total, 1e-5f);
            Assert.AreEqual(9.25f, enemies.Health(1), 1e-5f);
        }

        [Test]
        public void LightningHits_OneHitPerEnemyInTheZone()
        {
            // lightning_hits: one hit per enemy in the zone, dealt by the castle.
            var enemies = new FakeOwner("Enemy");
            enemies.Add(1, 200f);
            enemies.Add(2, 200f);
            enemies.Add(3, 200f);
            var id = _tick.AddReceiver(enemies);
            foreach (var key in new[] { 1, 2, 3 })
                _tick.Hit(new CombatHit(id, key, 80f, CombatSource.Castle));

            _clock.Step();

            CollectionAssert.AreEqual(new[] { (1, 80f), (2, 80f), (3, 80f) }, enemies.Totals);

            _clock.Step();
            Assert.AreEqual(3, enemies.Totals.Count, "one press is one hit per enemy, on one tick only");
        }

        [Test]
        public void KillRewardTimes_EnemyFinishedBySeveralSidesFallsOnceSoItPaysOnce()
        {
            // kill_reward_times: one per enemy, even when several sides finish it together.
            var enemies = new FakeOwner("Enemy");
            enemies.Add(4, 30f);
            var id = _tick.AddReceiver(enemies);
            var paid = 0;
            enemies.OnFall = (_, __) => paid++;
            _tick.Hit(new CombatHit(id, 4, 20f, CombatSource.Hero));
            _tick.Hit(new CombatHit(id, 4, 20f, CombatSource.Castle));
            _tick.Hit(new CombatHit(id, 4, 20f, CombatSource.Hero));

            _clock.Step();
            _clock.Step();

            Assert.AreEqual(1, enemies.Totals.Count);
            CollectionAssert.AreEqual(new[] { 4 }, enemies.Fallen);
            Assert.AreEqual(1, paid);
        }
    }
}
