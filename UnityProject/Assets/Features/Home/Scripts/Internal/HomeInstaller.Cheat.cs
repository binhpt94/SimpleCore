#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections.Generic;
using Game.Core;

namespace Game.Home
{
    /// <summary>Dev builds only: the installer names its cheat states for the cheat list and picks which one to build.</summary>
    public sealed partial class HomeInstaller : ICheatStateSource
    {
        /// <summary>The feature's title in the design (node feature-home).</summary>
        public string CheatFeatureTitle => HomeCheats.FeatureTitle;

        /// <summary>In the order of the ideas of node feature-home/cheat.</summary>
        public IReadOnlyList<CheatState> CheatStates => HomeCheats.States;

        /// <summary>
        /// The state this scene builds: once something was picked in this run of the game, the pick alone (only when
        /// it is this feature's); before that, the state chosen on the asset.
        /// </summary>
        private HomeCheatState CheatToBuild(ICheatSelection cheats) =>
            cheats.HasPick ? HomeCheats.StateOf(cheats.PickedKey) : cheat;

        private ICheatSelection NeedCheatSelection(IServiceRegistry registry)
        {
            if (registry.TryResolve<ICheatSelection>(out var cheats)) return cheats;
            throw new InvalidOperationException(
                $"{name}: {nameof(ICheatSelection)} is missing. Core's Boot registers it before any installer runs (dev builds).");
        }
    }

    /// <summary>Names and keys of this feature's cheat states, for the cheat list (Core's ICheatStateSource).</summary>
    internal static class HomeCheats
    {
        /// <summary>Title of the design node feature-home.</summary>
        internal const string FeatureTitle = "Màn bắt đầu";

        internal const string WithRecordKey = "Home/WithRecord";

        /// <summary>In the order of the ideas of node feature-home/cheat, titled as those ideas.</summary>
        internal static readonly IReadOnlyList<CheatState> States = new[]
        {
            new CheatState(WithRecordKey, "Màn bắt đầu đang hiện, đã có kỉ lục"),
        };

        /// <summary>The state a picked key stands for: <see cref="HomeCheatState.None"/> when it is not this feature's (or normal play).</summary>
        internal static HomeCheatState StateOf(string pickedKey) =>
            pickedKey == WithRecordKey ? HomeCheatState.WithRecord : HomeCheatState.None;

        internal static string KeyOf(HomeCheatState state) => state switch
        {
            HomeCheatState.WithRecord => WithRecordKey,
            _ => throw new ArgumentOutOfRangeException(nameof(state), state, "not a cheat state"),
        };
    }
}
#endif
