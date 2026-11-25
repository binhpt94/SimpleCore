using System.Collections.Generic;

namespace Game.Hero
{
    /// <summary>
    /// Where each team slot starts and respawns on the battlefield, given from outside (the castle's edges).
    /// Takes effect from the next run start; a hero already on the field keeps its point until then.
    /// </summary>
    public interface IHeroSpawnPoints
    {
        /// <summary>
        /// Start and respawn x (u) by team slot, slot 0 first. Must have one entry per slot; a shorter or empty
        /// list is refused with an error naming the missing slot.
        /// </summary>
        void SetSpawnX(IReadOnlyList<float> xBySlot);
    }
}
