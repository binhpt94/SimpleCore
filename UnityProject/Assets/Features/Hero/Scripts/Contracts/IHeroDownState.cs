using System;

namespace Game.Hero
{
    /// <summary>
    /// Whether each hero of the team is down (fallen and waiting to come back, feature-hero/hero-respawn), by slot
    /// (0 for slot 1), for displays that mark a downed hero.
    /// </summary>
    public interface IHeroDownState
    {
        /// <summary>True while the hero in <paramref name="slot"/> is down; false when standing or when no run is going.</summary>
        bool IsDown(int slot);

        /// <summary>
        /// Raised with the slot when its hero goes down or stands up again, in the step it happened. A new run
        /// raises it for every slot that was down, so a display never keeps a stale mark.
        /// </summary>
        event Action<int> DownChanged;
    }
}
