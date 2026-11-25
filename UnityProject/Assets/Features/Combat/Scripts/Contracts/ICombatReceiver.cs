namespace Game.Combat
{
    /// <summary>
    /// The owner of sides that can be hit. Combat never keeps health: it asks the owner who can still be hit,
    /// hands it one total per side, then lets it report the sides that fell.
    /// </summary>
    public interface ICombatReceiver
    {
        /// <summary>
        /// False when the side named <paramref name="key"/> no longer takes hits (fallen, appearing on revive,
        /// defeated, destroyed). Asked before any health of the tick is taken.
        /// </summary>
        bool CanBeHit(int key);

        /// <summary>Takes <paramref name="total"/> health from the side, once per side per tick. Reports nothing yet.</summary>
        void TakeTotal(int key, float total);

        /// <summary>
        /// Every owner hit this tick has taken its totals: report each side whose health went from above 0 to 0,
        /// once per side.
        /// </summary>
        void ReportFallen();
    }
}
