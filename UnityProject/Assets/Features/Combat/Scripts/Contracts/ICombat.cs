using System;

namespace Game.Combat
{
    /// <summary>
    /// Settles every hit landed in one fixed step of the battle together, so the result does not depend on which
    /// side is handled first.
    /// </summary>
    public interface ICombat
    {
        /// <summary>Adds an owner of sides that can be hit; returns the number its hits carry as <see cref="CombatHit.Receiver"/>.</summary>
        int AddReceiver(ICombatReceiver receiver);

        void AddActor(ICombatActor actor);

        /// <summary>
        /// A hit landed. Dealt during a tick, it belongs to that tick; dealt between ticks (a skill pressed by the
        /// player), it belongs to the next tick.
        /// </summary>
        void Hit(in CombatHit hit);

        /// <summary>Raised at the end of every tick, after every owner reported its fallen sides.</summary>
        event Action TickDone;
    }
}
