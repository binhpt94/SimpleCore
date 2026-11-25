namespace Game.Combat
{
    /// <summary>Something that moves and attacks on the battlefield (heroes, enemies, the castle).</summary>
    internal interface ICombatActor
    {
        /// <summary>First part of a tick: move and pick targets, from the state at the start of the tick.</summary>
        void Aim(float stepSeconds);

        /// <summary>Second part of a tick, after every actor aimed: deal hits through <see cref="ICombat.Hit"/>.</summary>
        void Strike(float stepSeconds);
    }
}
