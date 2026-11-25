using NUnit.Framework;
using UnityEditor;

namespace Game.Castle.Tests
{
    /// <summary>The castle's config asset holds the round-4 numbers of live/feature-castle at the IDEA stage.</summary>
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

        [Test]
        public void Skill_numbers_match_the_design_with_only_thunder_at_the_idea_stage()
        {
            var config = Load();

            Assert.That(config.Skills, Is.EqualTo(new[] { CastleSkill.Thunder }));
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
