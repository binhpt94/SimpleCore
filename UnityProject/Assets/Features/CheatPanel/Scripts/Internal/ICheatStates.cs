using System.Collections.Generic;

namespace Game.CheatPanel
{
    /// <summary>
    /// The cheat states of the running build and the one pick of the game process. Dev builds read them from Core's
    /// cheat catalog and selection; release builds carry no cheat state code and use <see cref="NoCheatStates"/>.
    /// Nothing here writes to the device.
    /// </summary>
    internal interface ICheatStates
    {
        /// <summary>Every state the build can build, grouped by feature in the feature's own order. Never null.</summary>
        IReadOnlyList<CheatEntry> Entries { get; }

        /// <summary>Key of the state the current scene built; null when none.</summary>
        string RunningKey { get; }

        /// <summary>Picks a state by key, or normal play with null; takes effect when the next scene boots.</summary>
        void Pick(string key);
    }

    /// <summary>A build without cheat state code: no state, nothing running, a pick changes nothing.</summary>
    internal sealed class NoCheatStates : ICheatStates
    {
        public IReadOnlyList<CheatEntry> Entries { get; } = new CheatEntry[0];
        public string RunningKey => null;
        public void Pick(string key) { }
    }
}
