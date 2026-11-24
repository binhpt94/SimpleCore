using System;

namespace Game.Hero
{
    /// <summary>The stats of one hero that change with its level (live: feature-hero/hero-stats/hero-level-values).</summary>
    internal readonly struct HeroLevelStats
    {
        private HeroLevelStats(float maxHealth, float damage, float secondsPerAttack)
        {
            MaxHealth = maxHealth;
            Damage = damage;
            SecondsPerAttack = secondsPerAttack;
        }

        public float MaxHealth { get; }
        public float Damage { get; }
        public float SecondsPerAttack { get; }

        /// <summary>
        /// Level n: health and damage = lv1 × (1 + step × (n-1)); seconds per attack = lv1 / (1 + min(speedStep × (n-1), cap)).
        /// Range and move speed never change with level.
        /// </summary>
        public static HeroLevelStats At(HeroConfig.HeroEntry hero, int level, HeroConfig config)
        {
            if (hero == null) throw new ArgumentNullException(nameof(hero));
            if (config == null) throw new ArgumentNullException(nameof(config));
            if (level < 1) throw new ArgumentOutOfRangeException(nameof(level), level, "Hero levels start at 1.");

            var gained = level - 1;
            var statScale = 1f + config.LevelStatStep * gained;
            var speedScale = 1f + Math.Min(config.LevelAttackSpeedStep * gained, config.LevelAttackSpeedCap);
            return new HeroLevelStats(hero.maxHealth * statScale, hero.damage * statScale, hero.secondsPerAttack / speedScale);
        }
    }
}
