using System.Collections.Generic;
using NUnit.Framework;

namespace Game.Combat.Tests
{
    public class CombatTickTests
    {
        private const float Step = 0.02f;

        [Test]
        public void TickRunsAimStrikeFilterTakeHealthReportFallThenDone()
        {
            var log = new CallLog();
            var tick = new CombatTick();
            var enemies = new FakeOwner("Enemy", log);
            enemies.Add(1, 10f);
            var enemyId = tick.AddReceiver(enemies);
            var hero = new FakeActor("Hero", log);
            hero.OnStrike = () => tick.Hit(new CombatHit(enemyId, 1, 12f, CombatSource.Hero));
            var castle = new FakeActor("Castle", log);
            tick.AddActor(hero);
            tick.AddActor(castle);
            tick.TickDone += () => log.Add("TickDone");

            tick.Run(Step);

            CollectionAssert.AreEqual(new[]
            {
                "Hero.Aim", "Castle.Aim", "Hero.Strike", "Castle.Strike",
                "Enemy.CanBeHit(1)", "Enemy.TakeTotal(1)", "Enemy.ReportFallen", "TickDone",
            }, log.Calls);
        }

        [Test]
        public void EveryActorStrikesOnlyAfterEveryActorAimed()
        {
            var log = new CallLog();
            var tick = new CombatTick();
            tick.AddActor(new FakeActor("A", log));
            tick.AddActor(new FakeActor("B", log));

            tick.Run(Step);

            CollectionAssert.AreEqual(new[] { "A.Aim", "B.Aim", "A.Strike", "B.Strike" }, log.Calls);
        }

        [Test]
        public void TickWithNoHitStillReportsDone()
        {
            var tick = new CombatTick();
            var done = 0;
            tick.TickDone += () => done++;

            tick.Run(Step);

            Assert.AreEqual(1, done);
        }

        [Test]
        public void DamageKeepsFractionsWhenSummedAndTaken()
        {
            var tick = new CombatTick();
            var enemies = new FakeOwner("Enemy");
            enemies.Add(1, 100f);
            var id = tick.AddReceiver(enemies);
            tick.Hit(new CombatHit(id, 1, 12.25f, CombatSource.Hero));
            tick.Hit(new CombatHit(id, 1, 0.5f, CombatSource.Castle));

            tick.Run(Step);

            Assert.AreEqual(1, enemies.Totals.Count);
            Assert.AreEqual(12.75f, enemies.Totals[0].total, 1e-5f);
            Assert.AreEqual(87.25f, enemies.Health(1), 1e-5f);
        }

        [Test]
        public void SidesWithLessThanOneHealthLeftAreNotFallen()
        {
            var tick = new CombatTick();
            var heroes = new FakeOwner("Hero");
            heroes.Add(0, 10f);
            var id = tick.AddReceiver(heroes);
            tick.Hit(new CombatHit(id, 0, 9.6f, CombatSource.Enemy));

            tick.Run(Step);

            Assert.AreEqual(0.4f, heroes.Health(0), 1e-5f);
            Assert.IsEmpty(heroes.Fallen);
        }

        [Test]
        public void HitsOfSeveralSidesOnOneEnemyBecomeOneTotalAndOneFall()
        {
            var tick = new CombatTick();
            var enemies = new FakeOwner("Enemy");
            enemies.Add(7, 30f);
            var id = tick.AddReceiver(enemies);
            tick.Hit(new CombatHit(id, 7, 12f, CombatSource.Hero));
            tick.Hit(new CombatHit(id, 7, 14f, CombatSource.Hero));
            tick.Hit(new CombatHit(id, 7, 10f, CombatSource.Castle));

            tick.Run(Step);

            Assert.AreEqual(1, enemies.Totals.Count, "the owner takes health once, so it pays coins once");
            Assert.AreEqual(36f, enemies.Totals[0].total, 1e-5f);
            CollectionAssert.AreEqual(new[] { 7 }, enemies.Fallen);
        }

        [Test]
        public void ThunderPressedBetweenTicksHitsEveryEnemyInZoneOnTheNextTick()
        {
            var tick = new CombatTick();
            var enemies = new FakeOwner("Enemy");
            enemies.Add(1, 76f);
            enemies.Add(2, 200f);
            enemies.Add(3, 50f);
            var id = tick.AddReceiver(enemies);

            // The castle makes one hit per enemy in the zone at the moment the player pressed (1 and 2; 3 is outside).
            tick.Hit(new CombatHit(id, 1, 80f, CombatSource.Castle));
            tick.Hit(new CombatHit(id, 2, 80f, CombatSource.Castle));
            tick.Run(Step);

            CollectionAssert.AreEqual(new[] { (1, 80f), (2, 80f) }, enemies.Totals);
            CollectionAssert.AreEqual(new[] { 1 }, enemies.Fallen);
            Assert.AreEqual(50f, enemies.Health(3));

            tick.Run(Step);
            Assert.AreEqual(2, enemies.Totals.Count, "a press is one hit per enemy, only on one tick");
        }

        [Test]
        public void HitsOnSidesThatNoLongerTakeHitsAreDropped()
        {
            var tick = new CombatTick();
            var heroes = new FakeOwner("Hero");
            heroes.Add(0, 100f);
            heroes.Add(1, 100f);
            heroes.SetUnhittable(1); // appearing on revive
            heroes.Add(2, 0f); // fallen
            var id = tick.AddReceiver(heroes);
            tick.Hit(new CombatHit(id, 0, 5f, CombatSource.Enemy));
            tick.Hit(new CombatHit(id, 1, 5f, CombatSource.Enemy));
            tick.Hit(new CombatHit(id, 2, 5f, CombatSource.Enemy));

            tick.Run(Step);

            CollectionAssert.AreEqual(new[] { (0, 5f) }, heroes.Totals);
        }

        [Test]
        public void HeroAndEnemyThatFinishEachOtherInOneTickBothFall()
        {
            var tick = new CombatTick();
            var heroes = new FakeOwner("Hero");
            heroes.Add(0, 10f);
            var enemies = new FakeOwner("Enemy");
            enemies.Add(5, 12f);
            var heroId = tick.AddReceiver(heroes);
            var enemyId = tick.AddReceiver(enemies);
            var hero = new FakeActor("Hero");
            hero.OnStrike = () => tick.Hit(new CombatHit(enemyId, 5, 12f, CombatSource.Hero));
            var enemy = new FakeActor("Enemy");
            enemy.OnStrike = () => tick.Hit(new CombatHit(heroId, 0, 15f, CombatSource.Enemy));
            tick.AddActor(hero);
            tick.AddActor(enemy);

            tick.Run(Step);

            CollectionAssert.AreEqual(new[] { 0 }, heroes.Fallen, "the hero falls");
            CollectionAssert.AreEqual(new[] { 5 }, enemies.Fallen, "the enemy is defeated, so it still pays coins");
        }

        [Test]
        public void CastleAndLastEnemyFallingTogetherAreReportedOnlyAfterBothTookHealth()
        {
            var tick = new CombatTick();
            var castle = new FakeOwner("Castle");
            castle.Add(0, 5f);
            var enemies = new FakeOwner("Enemy");
            enemies.Add(9, 5f);
            var castleId = tick.AddReceiver(castle);
            var enemyId = tick.AddReceiver(enemies);
            float enemyHealthSeenByCastleFall = float.NaN;
            float castleHealthSeenByEnemyFall = float.NaN;
            castle.OnFall = (_, __) => enemyHealthSeenByCastleFall = enemies.Health(9);
            enemies.OnFall = (_, __) => castleHealthSeenByEnemyFall = castle.Health(0);
            var reportsBeforeDone = 0;
            tick.TickDone += () => reportsBeforeDone = castle.Fallen.Count + enemies.Fallen.Count;
            tick.Hit(new CombatHit(castleId, 0, 6f, CombatSource.Enemy));
            tick.Hit(new CombatHit(enemyId, 9, 6f, CombatSource.Castle));

            tick.Run(Step);

            Assert.LessOrEqual(enemyHealthSeenByCastleFall, 0f);
            Assert.LessOrEqual(castleHealthSeenByEnemyFall, 0f);
            Assert.AreEqual(2, reportsBeforeDone, "tick done comes after every fall was reported");
        }

        [Test]
        public void HitOnEnemyOutsideTheScreenStillLandsWhenTheDealerChoseIt()
        {
            var tick = new CombatTick();
            var enemies = new FakeOwner("Enemy");
            enemies.Add(3, 40f); // the enemy stands at x = 17u, past the 16u screen edge: combat never looks at places
            var id = tick.AddReceiver(enemies);
            tick.Hit(new CombatHit(id, 3, 14f, CombatSource.Hero));

            tick.Run(Step);

            Assert.AreEqual(26f, enemies.Health(3), 1e-5f);
        }

        [Test]
        public void CheerHealingInTheSameTickSavesTheHeroBeforeDamageIsTaken()
        {
            var tick = new CombatTick();
            var heroes = new FakeOwner("Hero");
            heroes.Add(0, 20f); // 20 of 100 health
            var heroId = tick.AddReceiver(heroes);
            var enemy = new FakeActor("Enemy");
            enemy.OnStrike = () => tick.Hit(new CombatHit(heroId, 0, 35f, CombatSource.Enemy));
            tick.AddActor(enemy);

            heroes.Heal(0, 30f); // Cheer pressed: 30% of 100 max health, applied at once, before the tick takes health
            tick.Run(Step);

            Assert.AreEqual(15f, heroes.Health(0), 1e-5f);
            Assert.IsEmpty(heroes.Fallen);
        }

        [Test]
        public void HitDealtWhileReportingAFallBelongsToTheNextTick()
        {
            var tick = new CombatTick();
            var enemies = new FakeOwner("Enemy");
            enemies.Add(1, 5f);
            enemies.Add(2, 50f);
            var id = tick.AddReceiver(enemies);
            enemies.OnFall = (_, __) => tick.Hit(new CombatHit(id, 2, 10f, CombatSource.Enemy));
            tick.Hit(new CombatHit(id, 1, 5f, CombatSource.Hero));

            tick.Run(Step);
            Assert.AreEqual(50f, enemies.Health(2));

            tick.Run(Step);
            Assert.AreEqual(40f, enemies.Health(2));
        }

        [Test]
        public void HitNamingAnUnknownOwnerFailsLoudly()
        {
            var tick = new CombatTick();
            Assert.Throws<System.ArgumentException>(() => tick.Hit(new CombatHit(3, 0, 1f, CombatSource.Hero)));
        }
    }
}
