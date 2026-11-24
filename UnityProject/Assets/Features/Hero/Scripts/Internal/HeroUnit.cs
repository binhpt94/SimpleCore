namespace Game.Hero
{
    /// <summary>What a standing hero is doing (live: feature-hero/hero-targeting).</summary>
    internal enum HeroMode
    {
        /// <summary>No enemy on screen: going back to, or standing at, its slot's spawn point.</summary>
        Waiting,
        /// <summary>Has a target out of range: walking towards it (or held at the walk limit).</summary>
        Approaching,
        /// <summary>Target in range: standing and hitting.</summary>
        Attacking,
    }

    /// <summary>One hero of the team on the battlefield. Changed only by <see cref="HeroSquad"/>.</summary>
    internal sealed class HeroUnit
    {
        public const int NoTarget = -1;

        public HeroUnit(int slot, HeroConfig.HeroEntry hero, float spawnX, HeroLevelStats stats)
        {
            Slot = slot;
            Hero = hero;
            SpawnX = spawnX;
            X = spawnX;
            Facing = spawnX < 0f ? -1 : 1;
            Level = 1;
            Stats = stats;
            Health = stats.MaxHealth;
            IsStanding = true;
            TargetHandle = NoTarget;
        }

        /// <summary>Team slot, 0 for slot 1.</summary>
        public int Slot { get; }

        public HeroConfig.HeroEntry Hero { get; }

        /// <summary>Where the hero spawns, respawns and waits, in u.</summary>
        public float SpawnX { get; }

        /// <summary>Centre of the hero's box, in u (x = 0 at the castle centre).</summary>
        public float X { get; internal set; }

        /// <summary>+1 facing right, -1 facing left.</summary>
        public int Facing { get; internal set; }

        /// <summary>True on frames the hero actually moved.</summary>
        public bool IsMoving { get; internal set; }

        public HeroMode Mode { get; internal set; }

        public int TargetHandle { get; internal set; }

        /// <summary>Seconds until the next hit may land; counts down while walking too.</summary>
        public float Cooldown { get; internal set; }

        public int Level { get; internal set; }

        public HeroLevelStats Stats { get; internal set; }

        /// <summary>Kept as a real number; shown rounded up. At most <see cref="HeroLevelStats.MaxHealth"/>.</summary>
        public float Health { get; internal set; }

        /// <summary>False from the frame health reached 0 until the hero respawns.</summary>
        public bool IsStanding { get; internal set; }

        /// <summary>Seconds left before a fallen hero respawns.</summary>
        public float RespawnLeft { get; internal set; }

        /// <summary>Seconds left of the respawn grow; meanwhile the hero neither hits nor is hit.</summary>
        public float GrowLeft { get; internal set; }

        /// <summary>Enemies see it and may hit it.</summary>
        public bool IsTargetable => IsStanding && GrowLeft <= 0f;
    }
}
