#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections.Generic;
using Game.Core;

namespace Game.HudUpgrade
{
    /// <summary>Dev builds only: the installer names its cheat states for the cheat list and picks which one to build.</summary>
    public sealed partial class HudUpgradeInstaller : ICheatStateSource
    {
        /// <summary>The feature's title in the design (node feature-hud-upgrade).</summary>
        public string CheatFeatureTitle => HudUpgradeCheats.FeatureTitle;

        /// <summary>In the order of the ideas of node feature-hud-upgrade/cheat.</summary>
        public IReadOnlyList<CheatState> CheatStates => HudUpgradeCheats.States;

        /// <summary>
        /// The state this scene builds: once something was picked in this run of the game, the pick alone (only when
        /// it is this feature's); before that, the state chosen on the config asset.
        /// </summary>
        private HudUpgradeCheatState CheatToBuild(ICheatSelection cheats) =>
            cheats.HasPick ? HudUpgradeCheats.StateOf(cheats.PickedKey) : config.CheatState;

        private static ICheatSelection NeedCheatSelection(IServiceRegistry registry)
        {
            if (registry.TryResolve<ICheatSelection>(out var cheats)) return cheats;
            throw new InvalidOperationException(
                $"HudUpgradeInstaller: {nameof(ICheatSelection)} is missing. Core's Boot registers it before any installer runs (dev builds).");
        }
    }

    /// <summary>Names and keys of this feature's cheat states, for the cheat list (Core's ICheatStateSource).</summary>
    internal static class HudUpgradeCheats
    {
        /// <summary>Title of the design node feature-hud-upgrade.</summary>
        internal const string FeatureTitle = "Thanh nâng cấp";

        internal const string MixedButtonsKey = "HudUpgrade/MixedButtons";

        /// <summary>In the order of the ideas of node feature-hud-upgrade/cheat, titled as those ideas.</summary>
        internal static readonly IReadOnlyList<CheatState> States = new[]
        {
            new CheatState(MixedButtonsKey, "Giữa trận, đủ tiền mua năm mục, thiếu tiền cho một mục"),
        };

        /// <summary>The state a picked key stands for: None when it is not this feature's (or normal play).</summary>
        internal static HudUpgradeCheatState StateOf(string pickedKey) =>
            pickedKey == MixedButtonsKey ? HudUpgradeCheatState.MixedButtons : HudUpgradeCheatState.None;

        internal static string KeyOf(HudUpgradeCheatState state) => state switch
        {
            HudUpgradeCheatState.MixedButtons => MixedButtonsKey,
            _ => throw new ArgumentOutOfRangeException(nameof(state), state, "not a cheat state"),
        };
    }
}
#endif
