using System;

namespace Game.Castle
{
    /// <summary>
    /// The castle's body as others see it: its health and the moment it is destroyed. Enemies hit the castle
    /// through Game.Enemy.IEnemyFoes, which the castle feature adds to the enemy field itself.
    /// </summary>
    public interface ICastle
    {
        float Health { get; }

        float MaxHealth { get; }

        bool IsDestroyed { get; }

        /// <summary>Raised after Health or MaxHealth changed.</summary>
        event Action HealthChanged;

        /// <summary>Raised once, when health reaches 0, after the run was told it is lost.</summary>
        event Action Destroyed;
    }
}
