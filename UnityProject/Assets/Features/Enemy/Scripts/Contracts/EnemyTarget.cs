namespace Game.Enemy
{
    /// <summary>
    /// A living enemy as seen from outside, at the moment it was read. Positions are in battlefield units (u),
    /// x = 0 at the castle centre, negative to the left.
    /// </summary>
    public readonly struct EnemyTarget
    {
        public EnemyTarget(int handle, EnemySide side, float x, float halfWidth, int spawnOrder)
        {
            Handle = handle;
            Side = side;
            X = x;
            HalfWidth = halfWidth;
            SpawnOrder = spawnOrder;
        }

        /// <summary>Names this enemy for <see cref="IEnemyField.Damage"/>; never reused within a run.</summary>
        public int Handle { get; }

        public EnemySide Side { get; }

        /// <summary>Centre of the enemy's box.</summary>
        public float X { get; }

        public float HalfWidth { get; }

        /// <summary>Lower means it entered the battlefield earlier.</summary>
        public int SpawnOrder { get; }
    }

    /// <summary>An enemy whose health reached 0. Raised once per enemy; removed enemies are not defeated.</summary>
    public readonly struct EnemyDefeat
    {
        public EnemyDefeat(int handle, EnemyType type, EnemySide side)
        {
            Handle = handle;
            Type = type;
            Side = side;
        }

        public int Handle { get; }

        public EnemyType Type { get; }

        public EnemySide Side { get; }
    }
}
