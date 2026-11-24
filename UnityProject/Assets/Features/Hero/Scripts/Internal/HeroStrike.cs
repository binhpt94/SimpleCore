namespace Game.Hero
{
    /// <summary>One hit a hero just made, for the view to draw (lunge, projectile, cleave streak, blast ring).</summary>
    internal readonly struct HeroStrike
    {
        public HeroStrike(HeroGroup group, HeroAttack attack, float fromX, float frontX, float targetX, int facing, float areaSize)
        {
            Group = group;
            Attack = attack;
            FromX = fromX;
            FrontX = frontX;
            TargetX = targetX;
            Facing = facing;
            AreaSize = areaSize;
        }

        public HeroGroup Group { get; }
        public HeroAttack Attack { get; }

        /// <summary>The hero's centre, in u.</summary>
        public float FromX { get; }

        /// <summary>The hero's front edge (towards the target), in u.</summary>
        public float FrontX { get; }

        /// <summary>The target's centre, in u.</summary>
        public float TargetX { get; }

        public int Facing { get; }

        /// <summary>Cleave length or blast radius, in u; 0 for a single-target hit.</summary>
        public float AreaSize { get; }
    }
}
