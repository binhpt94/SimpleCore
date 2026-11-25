using NUnit.Framework;

namespace Game.Hero.Tests
{
    /// <summary>
    /// The real HeroConfig asset carries the numbers of the fixed design (live: hero-roster-values, respawn-values,
    /// hero-level-values, targeting-values), so a balance change is a change of the asset, seen here.
    /// </summary>
    public sealed class HeroConfigValuesTests
    {
        private const float Tolerance = 1e-4f;

        [TestCase("knight", HeroGroup.Melee, HeroAttack.SingleTarget, 300f, 12f, 1.0f, 1.2f, 2.0f, 0f)]
        [TestCase("axeman", HeroGroup.Melee, HeroAttack.FrontCleave, 240f, 14f, 1.4f, 1.3f, 2.4f, 1.5f)]
        [TestCase("swordsman", HeroGroup.Melee, HeroAttack.SingleTarget, 180f, 16f, 0.7f, 1.2f, 3.5f, 0f)]
        [TestCase("archer", HeroGroup.Ranged, HeroAttack.SingleTarget, 120f, 14f, 0.8f, 7f, 3.0f, 0f)]
        [TestCase("mage", HeroGroup.Ranged, HeroAttack.Blast, 100f, 18f, 1.6f, 6f, 2.6f, 1.5f)]
        public void Each_hero_starts_at_its_designed_level_1_numbers(
            string code, HeroGroup group, HeroAttack attack,
            float maxHealth, float damage, float secondsPerAttack, float range, float moveSpeed, float areaSize)
        {
            var hero = HeroFakes.LoadConfig().Find(code);

            Assert.That(hero, Is.Not.Null, code);
            Assert.That(hero.group, Is.EqualTo(group));
            Assert.That(hero.attack, Is.EqualTo(attack));
            Assert.That(hero.maxHealth, Is.EqualTo(maxHealth).Within(Tolerance));
            Assert.That(hero.damage, Is.EqualTo(damage).Within(Tolerance));
            Assert.That(hero.secondsPerAttack, Is.EqualTo(secondsPerAttack).Within(Tolerance));
            Assert.That(hero.range, Is.EqualTo(range).Within(Tolerance));
            Assert.That(hero.moveSpeed, Is.EqualTo(moveSpeed).Within(Tolerance));
            Assert.That(hero.areaSize, Is.EqualTo(areaSize).Within(Tolerance));
        }

        [Test]
        public void No_two_heroes_run_at_the_same_speed_and_all_run_between_2_and_3_5u_per_second()
        {
            var config = HeroFakes.LoadConfig();
            var speeds = new System.Collections.Generic.List<float>();

            foreach (var hero in config.Heroes)
            {
                Assert.That(hero.moveSpeed, Is.InRange(2f - Tolerance, 3.5f + Tolerance), hero.code);
                foreach (var other in speeds)
                    Assert.That(System.Math.Abs(other - hero.moveSpeed), Is.GreaterThan(Tolerance), $"{hero.code} shares a speed");
                speeds.Add(hero.moveSpeed);
            }

            Assert.That(speeds, Has.Count.EqualTo(5));
            Assert.That(config.Find("knight").moveSpeed, Is.EqualTo(2f).Within(Tolerance), "slowest");
            Assert.That(config.Find("swordsman").moveSpeed, Is.EqualTo(3.5f).Within(Tolerance), "fastest");
        }

        [Test]
        public void Every_hero_shares_a_box_1u_wide_and_1_5u_tall()
        {
            var config = HeroFakes.LoadConfig();

            Assert.That(config.BoxWidth, Is.EqualTo(1f).Within(Tolerance));
            Assert.That(config.BoxHeight, Is.EqualTo(1.5f).Within(Tolerance));
        }

        [Test]
        public void The_team_has_three_slots_spawning_at_the_castle_edges()
        {
            var config = HeroFakes.LoadConfig();

            Assert.That(config.SlotSpawnX, Is.EqualTo(new[] { -1.5f, 1.5f, -1.5f }));
        }

        [Test]
        public void A_fallen_hero_shrinks_in_0_3s_waits_8s_and_grows_back_in_0_3s()
        {
            var config = HeroFakes.LoadConfig();

            Assert.That(config.FallSeconds, Is.EqualTo(0.3f).Within(Tolerance));
            Assert.That(config.RespawnSeconds, Is.EqualTo(8f).Within(Tolerance));
            Assert.That(config.RespawnGrowSeconds, Is.EqualTo(0.3f).Within(Tolerance));
        }

        [Test]
        public void Each_level_adds_15_percent_health_and_damage_and_3_percent_attack_speed_up_to_60_percent()
        {
            var config = HeroFakes.LoadConfig();

            Assert.That(config.LevelStatStep, Is.EqualTo(0.15f).Within(Tolerance));
            Assert.That(config.LevelAttackSpeedStep, Is.EqualTo(0.03f).Within(Tolerance));
            Assert.That(config.LevelAttackSpeedCap, Is.EqualTo(0.6f).Within(Tolerance));
            Assert.That(config.LevelUpPopScale, Is.EqualTo(1.2f).Within(Tolerance));
            Assert.That(config.LevelUpPopSeconds, Is.EqualTo(0.3f).Within(Tolerance));
        }

        [Test]
        public void Heroes_target_enemies_within_16u_and_walk_within_15u_of_the_castle_centre()
        {
            var config = HeroFakes.LoadConfig();

            Assert.That(config.TargetHalfRange, Is.EqualTo(16f).Within(Tolerance));
            Assert.That(config.WalkHalfRange, Is.EqualTo(15f).Within(Tolerance));
        }
    }
}
