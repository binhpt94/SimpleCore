#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System.Collections.Generic;

namespace Game.Core
{
    /// <summary>
    /// A feature's installer asset that can build cheat states declares them here, for a cheat list to show.
    /// Dev builds only: release builds carry no cheat state code. No game rules: the owning feature alone builds
    /// its states (in its own Install), this only names them.
    /// </summary>
    public interface ICheatStateSource
    {
        /// <summary>The feature's display name, shown before each state ("&lt;feature title&gt;: &lt;state title&gt;") and used to sort features A to Z.</summary>
        string CheatFeatureTitle { get; }

        /// <summary>The states this build can build, in the order of the ideas of the feature's cheat design node. Never null; may be empty.</summary>
        IReadOnlyList<CheatState> CheatStates { get; }
    }

    /// <summary>One cheat state a feature can build.</summary>
    public readonly struct CheatState
    {
        public CheatState(string key, string title)
        {
            Key = key;
            Title = title;
        }

        /// <summary>Stable key, unique in the whole game, written "&lt;Feature&gt;/&lt;State&gt;" (for example "PopupLose/NewRecord"). Never shown.</summary>
        public string Key { get; }

        /// <summary>The state's display title: the title of its idea in the cheat design node.</summary>
        public string Title { get; }
    }
}
#endif
