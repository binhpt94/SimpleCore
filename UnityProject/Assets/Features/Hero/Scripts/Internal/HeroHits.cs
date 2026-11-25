using System;
using Game.Combat;
using Game.Enemy;

namespace Game.Hero
{
    /// <summary>Where a hero's hit on an enemy goes. Chosen once by the installer, so the squad's rules do not change.</summary>
    internal interface IHeroHits
    {
        /// <summary>Lands <paramref name="damage"/> on the enemy; true only when the enemy is known to be defeated at once.</summary>
        bool Hit(int enemyHandle, float damage);
    }

    /// <summary>
    /// Without the shared combat (live feature-combat/combat-tick/tick-values, combat_enabled off at IDEA): the hit takes
    /// the enemy's health at once.
    /// </summary>
    internal sealed class DirectHeroHits : IHeroHits
    {
        private readonly IEnemyField _field;

        public DirectHeroHits(IEnemyField field) => _field = field ?? throw new ArgumentNullException(nameof(field));

        public bool Hit(int enemyHandle, float damage) => _field.Damage(enemyHandle, damage);
    }

    /// <summary>
    /// With the shared combat (combat_enabled on from PROTOTYPE): the hit is handed to combat, which totals the hits of
    /// the step and lets the enemies take their health after every side struck. Nobody is defeated until then.
    /// </summary>
    internal sealed class CombatHeroHits : IHeroHits
    {
        private readonly ICombat _combat;
        private readonly IEnemyCombat _enemies;

        public CombatHeroHits(ICombat combat, IEnemyCombat enemies)
        {
            _combat = combat ?? throw new ArgumentNullException(nameof(combat));
            _enemies = enemies ?? throw new ArgumentNullException(nameof(enemies));
        }

        public bool Hit(int enemyHandle, float damage)
        {
            _combat.Hit(new CombatHit(_enemies.CombatReceiver, enemyHandle, damage, CombatSource.Hero));
            return false;
        }
    }
}
