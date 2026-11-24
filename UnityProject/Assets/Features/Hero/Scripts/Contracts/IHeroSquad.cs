namespace Game.Hero
{
    /// <summary>
    /// The team on the battlefield, by slot (0 for slot 1). Enemies reach the heroes through
    /// Game.Enemy.IEnemyFoes, which the hero feature adds to the enemy field itself.
    /// </summary>
    public interface IHeroSquad
    {
        /// <summary>Every standing hero gains <paramref name="fractionOfMaxHealth"/> of its own max health, capped at max.</summary>
        void HealAll(float fractionOfMaxHealth);

        /// <summary>The hero in <paramref name="slot"/> is now at <paramref name="level"/>; takes effect at once, even when fallen.</summary>
        void ApplyLevel(int slot, int level);
    }
}
