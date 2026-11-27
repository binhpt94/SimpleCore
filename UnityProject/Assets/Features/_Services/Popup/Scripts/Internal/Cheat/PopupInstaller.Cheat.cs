#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections.Generic;
using Game.Core;

namespace Game.Services.Popup
{
    /// <summary>Dev builds only: the installer names its cheat states for the cheat list and picks which one to build.</summary>
    public sealed partial class PopupInstaller : ICheatStateSource
    {
        /// <summary>The service's title in the design (node feature-popup).</summary>
        public string CheatFeatureTitle => PopupCheat.FeatureTitle;

        /// <summary>In the order of the ideas of node feature-popup/cheat.</summary>
        public IReadOnlyList<CheatState> CheatStates => PopupCheat.States;

        /// <summary>
        /// The state this scene builds: once something was picked in this run of the game, the pick alone (only when
        /// it is this service's); before that, the state chosen on the asset.
        /// </summary>
        private PopupCheatState CheatToBuild(ICheatSelection cheats) =>
            cheats.HasPick ? PopupCheat.StateOf(cheats.PickedKey) : cheat;

        private static ICheatSelection NeedCheatSelection(IServiceRegistry registry)
        {
            if (registry.TryResolve<ICheatSelection>(out var cheats)) return cheats;
            throw new InvalidOperationException(
                $"Popup: {nameof(ICheatSelection)} is missing. Core's Boot registers it before any installer runs (dev builds).");
        }
    }
}
#endif
