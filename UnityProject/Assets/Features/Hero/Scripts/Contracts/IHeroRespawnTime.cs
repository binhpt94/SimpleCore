namespace Game.Hero
{
    /// <summary>
    /// How long each downed hero still waits before it comes back (feature-hero/hero-respawn), by slot (0 for slot 1),
    /// for displays that show the wait. Read it each frame; <see cref="IHeroDownState.DownChanged"/> tells when a wait
    /// starts and ends.
    /// </summary>
    public interface IHeroRespawnTime
    {
        /// <summary>
        /// Seconds left before the hero in <paramref name="slot"/> comes back; 0 while it stands or when no run is going.
        /// Counts only battle steps, so it stands still while the battle is paused or waits for the castle's revive.
        /// </summary>
        float RespawnSecondsLeft(int slot);
    }
}
