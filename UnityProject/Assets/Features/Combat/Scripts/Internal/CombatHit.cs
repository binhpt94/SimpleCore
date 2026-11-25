namespace Game.Combat
{
    /// <summary>Who dealt a hit.</summary>
    internal enum CombatSource
    {
        Hero,
        Enemy,
        Castle,
    }

    /// <summary>
    /// One landed hit: who dealt it, who takes it and its damage, fixed when the hit was dealt. The dealer
    /// chooses every side it hits; an area hit is one <see cref="CombatHit"/> per side hit.
    /// </summary>
    internal readonly struct CombatHit
    {
        public CombatHit(int receiver, int targetKey, float damage, CombatSource source)
        {
            Receiver = receiver;
            TargetKey = targetKey;
            Damage = damage;
            Source = source;
        }

        /// <summary>The owner of the side hit, as returned by <see cref="ICombat.AddReceiver"/>.</summary>
        public int Receiver { get; }

        /// <summary>The side hit, named by its owner.</summary>
        public int TargetKey { get; }

        /// <summary>Real number, never rounded by combat.</summary>
        public float Damage { get; }

        public CombatSource Source { get; }
    }
}
