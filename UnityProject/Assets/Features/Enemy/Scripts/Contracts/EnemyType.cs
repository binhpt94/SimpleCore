namespace Game.Enemy
{
    /// <summary>The kinds of enemy. Each kind has its own box, attack and stats in the enemy config.</summary>
    public enum EnemyType
    {
        Soldier,
        Wolf,
        Giant,
        Archer,
        Boss,
    }

    /// <summary>The battlefield side an enemy comes from and walks on; it never changes side.</summary>
    public enum EnemySide
    {
        Left,
        Right,
    }
}
