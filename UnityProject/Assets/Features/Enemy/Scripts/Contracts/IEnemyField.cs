using System;
using System.Collections.Generic;

namespace Game.Enemy
{
    /// <summary>The enemies on the battlefield: bring them in, look at them, hurt and slow them, hear when one falls.</summary>
    public interface IEnemyField
    {
        /// <summary>Brings one enemy in at its side's spawn point, at full health.</summary>
        void Spawn(EnemySpawn spawn);

        /// <summary>Enemies spawned and not yet defeated or removed, including those not yet on screen.</summary>
        int AliveCount { get; }

        /// <summary>Clears <paramref name="into"/> and fills it with every living enemy, in spawn order.</summary>
        void GetAlive(List<EnemyTarget> into);

        /// <summary>
        /// Takes <paramref name="amount"/> health from the enemy. Ignored when the enemy is already gone.
        /// Returns true when this hit defeated it.
        /// </summary>
        bool Damage(int handle, float amount);

        /// <summary>Slows every living enemy for <paramref name="seconds"/>; an enemy already slowed starts over.</summary>
        void SlowAll(float seconds);

        /// <summary>Raised once per enemy when its health reaches 0, after its coins were paid.</summary>
        event Action<EnemyDefeat> Defeated;

        /// <summary>
        /// Adds a group of foes enemies attack. Called by the foes' owner during its Install, so the enemy
        /// feature never has to know who they are.
        /// </summary>
        void AddFoes(IEnemyFoes foes);
    }
}
