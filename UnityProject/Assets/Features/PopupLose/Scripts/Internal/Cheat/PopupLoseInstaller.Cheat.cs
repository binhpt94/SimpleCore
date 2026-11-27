#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections.Generic;
using Game.Core;

namespace Game.PopupLose
{
    /// <summary>Dev builds only: the installer names its cheat states for the cheat list and picks which one to build.</summary>
    public sealed partial class PopupLoseInstaller : ICheatStateSource
    {
        /// <summary>The feature's title in the design (node feature-popup-lose).</summary>
        public string CheatFeatureTitle => LoseCheats.FeatureTitle;

        /// <summary>In the order of the ideas of node feature-popup-lose/cheat.</summary>
        public IReadOnlyList<CheatState> CheatStates => LoseCheats.States;

        /// <summary>
        /// The state this scene builds: once something was picked in this run of the game, the pick alone (only when
        /// it is this feature's); before that, the state chosen on the asset.
        /// </summary>
        private LoseCheatState CheatToBuild(ICheatSelection cheats) =>
            cheats.HasPick ? LoseCheats.StateOf(cheats.PickedKey) : _cheat;

        private static ICheatSelection NeedCheatSelection(IServiceRegistry registry)
        {
            if (registry.TryResolve<ICheatSelection>(out var cheats)) return cheats;
            throw new InvalidOperationException(
                $"PopupLose: {nameof(ICheatSelection)} is missing. Core's Boot registers it before any installer runs (dev builds).");
        }
    }
}
#endif
