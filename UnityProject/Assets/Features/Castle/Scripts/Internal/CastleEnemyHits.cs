using System;
using Game.Combat;
using Game.Enemy;

namespace Game.Castle
{
    /// <summary>How the castle's own attacks (auto shot, Thunder) land on an enemy. Damage is fixed when dealt.</summary>
    internal interface ICastleEnemyHits
    {
        void Hit(int enemyHandle, float damage);
    }

    /// <summary>
    /// The castle's hits go through the shared combat (feature-combat/combat-tick/tick-values, on from PROTOTYPE):
    /// one <see cref="CombatHit"/> per enemy hit, to the enemies' receiver, keyed by the enemy's handle, source Castle.
    /// Combat adds them up with the other hits of the tick and the enemies take the total; the castle never takes
    /// enemy health itself.
    /// </summary>
    internal sealed class CombatEnemyHits : ICastleEnemyHits
    {
        private readonly ICombat _combat;
        private readonly IEnemyCombat _enemies;

        public CombatEnemyHits(ICombat combat, IEnemyCombat enemies)
        {
            _combat = combat ?? throw new ArgumentNullException(nameof(combat));
            _enemies = enemies ?? throw new ArgumentNullException(nameof(enemies));
        }

        public void Hit(int enemyHandle, float damage)
        {
            if (damage <= 0f) return;
            _combat.Hit(new CombatHit(_enemies.CombatReceiver, enemyHandle, damage, CombatSource.Castle));
        }
    }
}
