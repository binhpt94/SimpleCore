namespace Game.Enemy
{
    /// <summary>
    /// The enemies' side of the shared combat (feature-combat/combat-tick, on from PROTOTYPE). Hits on an enemy go
    /// through combat with <see cref="CombatReceiver"/> as receiver and <see cref="EnemyTarget.Handle"/> as key; the
    /// enemies hit their foes through combat with the number each foes' owner gave in <see cref="AddCombatFoes"/>.
    /// Replaces <see cref="IEnemyField.Damage"/>, <see cref="IEnemyField.AddFoes"/> and <see cref="IEnemyFoes.Hit"/>,
    /// which stay only until every feature has moved over.
    /// </summary>
    public interface IEnemyCombat
    {
        /// <summary>The number combat gave the enemies when they were added as a receiver.</summary>
        int CombatReceiver { get; }

        /// <summary>
        /// Adds a group of foes the enemies attack through combat. The owner has already added itself to combat as a
        /// receiver and got <paramref name="combatReceiver"/>; enemy hits carry that number and the foe's
        /// <see cref="EnemyFoe.Key"/>. Called by the foes' owner during its Install.
        /// </summary>
        void AddCombatFoes(IEnemyFoes foes, int combatReceiver);
    }
}
