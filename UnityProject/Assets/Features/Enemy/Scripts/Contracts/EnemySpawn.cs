namespace Game.Enemy
{
    /// <summary>One enemy to bring onto the battlefield.</summary>
    public readonly struct EnemySpawn
    {
        public EnemySpawn(EnemyType type, EnemySide side, float statScale)
        {
            Type = type;
            Side = side;
            StatScale = statScale;
        }

        public EnemyType Type { get; }

        public EnemySide Side { get; }

        /// <summary>Multiplies the kind's base health and damage (1 = base). Decided by whoever spawns.</summary>
        public float StatScale { get; }
    }
}
