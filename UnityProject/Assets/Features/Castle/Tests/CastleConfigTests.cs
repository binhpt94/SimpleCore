using NUnit.Framework;
using UnityEditor;

namespace Game.Castle.Tests
{
    /// <summary>The castle's config asset holds the round-4 numbers of live/feature-castle at the PROTOTYPE stage.</summary>
    public class CastleConfigTests
    {
        private const string ConfigPath = "Assets/Features/Castle/FeatureAssets/Private/CastleConfig.asset";

        private static CastleConfig Load()
        {
            var config = AssetDatabase.LoadAssetAtPath<CastleConfig>(ConfigPath);
            Assert.That(config, Is.Not.Null, $"Missing {ConfigPath}");
            return config;
        }

        [Test]
        public void Castle_body_numbers_match_the_design()
        {
            var config = Load();

            Assert.That(config.MaxHealth, Is.EqualTo(1000f));
            Assert.That(config.HalfWidth, Is.EqualTo(1.5f), "3u wide, edges at x = -1.5 and +1.5");
            Assert.That(config.Height, Is.EqualTo(4f));
            Assert.That(config.HitFlashSeconds, Is.EqualTo(0.1f).Within(1e-5f));
            Assert.That(config.CollapseSeconds, Is.EqualTo(0.5f).Within(1e-5f));
            Assert.That(config.ReviveFraction, Is.EqualTo(0.3f).Within(1e-5f));
            Assert.That(config.ReviveGrowSeconds, Is.EqualTo(0.5f).Within(1e-5f));
            Assert.That(config.HeroSlotX, Is.EqualTo(new[] { -1.5f, 1.5f, -1.5f }), "slot 1 and 3 left edge, slot 2 right edge");
        }

        [Test]
        public void Castle_body_prototype_numbers_shake_a_tenth_of_a_second_and_show_damage_under_half_health()
        {
            var config = Load();

            Assert.That(config.HitShakeSeconds, Is.EqualTo(0.1f).Within(1e-5f), "PROTOTYPE: flash 0.1s + light shake 0.1s");
            Assert.That(config.DamagedHealthFraction, Is.EqualTo(0.5f).Within(1e-5f), "PROTOTYPE: a picture under 50% health");
            Assert.That(config.MaxHealth, Is.EqualTo(1000f), "PROTOTYPE keeps 1000");
        }

        [Test]
        public void Auto_shot_numbers_match_the_design()
        {
            var config = Load();

            Assert.That(config.ShotDamage, Is.EqualTo(10f));
            Assert.That(config.SecondsPerShot, Is.EqualTo(1f));
            Assert.That(config.ShotRange, Is.EqualTo(6f));
            Assert.That(config.ShotFlightSeconds, Is.EqualTo(0.15f).Within(1e-5f));
            Assert.That(config.ScreenHalfWidth, Is.EqualTo(16f));
        }

        // ShotParams table of live/feature-castle/castle-attack/auto-shot-values, IDEA column: one test per row key.

        [Test]
        public void Shot_damage_lv1_is_ten_health_per_shot()
        {
            // shot_damage_lv1 = 10
            Assert.That(Load().ShotDamage, Is.EqualTo(10f));
        }

        [Test]
        public void Shot_interval_is_one_second_per_shot()
        {
            // shot_interval = 1.0 s
            Assert.That(Load().SecondsPerShot, Is.EqualTo(1f));
        }

        [Test]
        public void Shot_range_is_six_units_from_the_castle_edge()
        {
            // shot_range = 6 u, counted from the castle edge (edges at x = -1.5 and +1.5)
            var config = Load();
            Assert.That(config.ShotRange, Is.EqualTo(6f));
            Assert.That(config.HalfWidth, Is.EqualTo(1.5f));
        }

        [Test]
        public void Bullet_fly_time_is_fifteen_hundredths_of_a_second()
        {
            // bullet_fly_time = 0.15 s
            Assert.That(Load().ShotFlightSeconds, Is.EqualTo(0.15f).Within(1e-5f));
        }

        [Test]
        public void Target_scope_is_enemy_centres_within_sixteen_units()
        {
            // target_scope: enemy centre |x| <= 16
            Assert.That(Load().ScreenHalfWidth, Is.EqualTo(16f));
        }

        [Test]
        public void Skill_numbers_match_the_design_with_all_three_skills_at_the_prototype_stage()
        {
            var config = Load();

            Assert.That(config.Skills, Is.EqualTo(new[] { CastleSkill.Thunder, CastleSkill.Frost, CastleSkill.Rally }),
                "PROTOTYPE: Thunder, Frost, Rally");
            Assert.That(config.ThunderDamage, Is.EqualTo(80f));
            Assert.That(config.ThunderRange, Is.EqualTo(6f));
            Assert.That(config.ThunderCooldown, Is.EqualTo(20f));
            Assert.That(config.ThunderEffectSeconds, Is.EqualTo(0.3f).Within(1e-5f));
            Assert.That(config.FrostSeconds, Is.EqualTo(5f));
            Assert.That(config.FrostCooldown, Is.EqualTo(35f));
            Assert.That(config.RallyFraction, Is.EqualTo(0.3f).Within(1e-5f));
            Assert.That(config.RallyCooldown, Is.EqualTo(25f));
        }

        [Test]
        public void Upgrade_numbers_match_the_design()
        {
            var config = Load();

            Assert.That(config.HealthPerLevel, Is.EqualTo(150f));
            Assert.That(config.AttackBonusPerLevel, Is.EqualTo(0.2f).Within(1e-5f));
            Assert.That(config.SkillBonusPerLevel, Is.EqualTo(0.2f).Within(1e-5f));
            Assert.That(config.LevelUpPopScale, Is.EqualTo(1.1f).Within(1e-5f));
            Assert.That(config.LevelUpPopSeconds, Is.EqualTo(0.3f).Within(1e-5f));
        }
    }
}
