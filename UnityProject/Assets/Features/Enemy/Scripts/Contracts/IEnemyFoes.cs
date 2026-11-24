using System.Collections.Generic;

namespace Game.Enemy
{
    /// <summary>
    /// Something enemies walk up to, stop at and hit, at the moment it was read. Positions are in battlefield
    /// units (u), x = 0 at the castle centre, negative to the left.
    /// </summary>
    public readonly struct EnemyFoe
    {
        public EnemyFoe(int key, float x, float halfWidth)
        {
            Key = key;
            X = x;
            HalfWidth = halfWidth;
        }

        /// <summary>Names the foe for <see cref="IEnemyFoes.Hit"/>; chosen by whoever owns the foes.</summary>
        public int Key { get; }

        /// <summary>Centre of the foe's box.</summary>
        public float X { get; }

        public float HalfWidth { get; }
    }

    /// <summary>
    /// A group of foes that enemies attack (for example the heroes, or the castle). The owner of the foes
    /// implements it and adds it with <see cref="IEnemyField.AddFoes"/> during its Install; enemies read it every frame.
    /// </summary>
    public interface IEnemyFoes
    {
        /// <summary>Clears <paramref name="into"/> and fills it with every foe that can be hit now.</summary>
        void GetFoes(List<EnemyFoe> into);

        /// <summary>An enemy hit the foe named <paramref name="key"/> for <paramref name="damage"/>.</summary>
        void Hit(int key, float damage);
    }
}
