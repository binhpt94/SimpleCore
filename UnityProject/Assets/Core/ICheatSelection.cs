#if UNITY_EDITOR || DEVELOPMENT_BUILD
namespace Game.Core
{
    /// <summary>
    /// Which cheat state the game should run, kept for as long as the game process lives (through scene loads,
    /// never written to the device). Boot registers the one shared instance in every scene before any installer
    /// runs. Dev builds only. No game rules.
    /// <para>
    /// Two values: the pick (what a cheat list asked for, lives through scene loads) and the running state
    /// (what the current scene actually built, reported by the owning installer, cleared when a scene boots).
    /// </para>
    /// </summary>
    public interface ICheatSelection
    {
        /// <summary>
        /// True once something was picked in this run of the game, a state or normal play. While true, every
        /// installer ignores the cheat state chosen on its asset and builds only <see cref="PickedKey"/> if it owns it.
        /// </summary>
        bool HasPick { get; }

        /// <summary>Key (<see cref="CheatState.Key"/>) of the picked state; null when nothing or normal play was picked.</summary>
        string PickedKey { get; }

        /// <summary>Picks a state by key, or normal play with null. Takes effect when the next scene boots.</summary>
        void Pick(string key);

        /// <summary>Key of the cheat state the current scene built (picked or chosen on an asset); null when none. Saving to the device is off while it is not null.</summary>
        string RunningKey { get; }

        /// <summary>Called by the owning installer, during Install, when it builds a cheat state.</summary>
        void ReportRunning(string key);

        /// <summary>Called by Boot when a scene starts booting, before any installer: clears <see cref="RunningKey"/>. Keeps the pick.</summary>
        void BeginScene();
    }
}
#endif
