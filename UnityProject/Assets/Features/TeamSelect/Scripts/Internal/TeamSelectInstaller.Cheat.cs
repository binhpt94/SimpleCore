#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections.Generic;
using Game.Core;
using UnityEngine;

namespace Game.TeamSelect
{
    /// <summary>Dev builds only: the installer names its cheat states for the cheat list and picks which one to build.</summary>
    public sealed partial class TeamSelectInstaller : ICheatStateSource
    {
        [Tooltip("Dev only: a state of feature-team-select/cheat to build at start (ignored once a cheat list picked something).")]
        [SerializeField] private TeamSelectCheatState cheat = TeamSelectCheatState.None;

        /// <summary>The state chosen on the asset (tests set it and put it back).</summary>
        internal TeamSelectCheatState Cheat
        {
            get => cheat;
            set => cheat = value;
        }

        /// <summary>The feature's title in the design (node feature-team-select).</summary>
        public string CheatFeatureTitle => TeamSelectCheats.FeatureTitle;

        /// <summary>In the order of the ideas of node feature-team-select/cheat.</summary>
        public IReadOnlyList<CheatState> CheatStates => TeamSelectCheats.States;

        /// <summary>
        /// The state this scene builds: once something was picked in this run of the game, the pick alone (only when
        /// it is this feature's); before that, the state chosen on the asset.
        /// </summary>
        private TeamSelectCheatState CheatToBuild(ICheatSelection cheats) =>
            cheats.HasPick ? TeamSelectCheats.StateOf(cheats.PickedKey) : cheat;

        /// <summary>Builds the state (if any) on the joined session and reports it as running.</summary>
        private static void BuildCheat(TeamSelectCheatState state, TeamSelectSession session, ICheatSelection cheats)
        {
            switch (state)
            {
                case TeamSelectCheatState.None:
                    return;
                case TeamSelectCheatState.FullTeam:
                    session.ApplyCheatFullTeam();
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(state), state, "not a cheat state");
            }
            cheats.ReportRunning(TeamSelectCheats.KeyOf(state));
        }

        private ICheatSelection NeedCheatSelection(IServiceRegistry registry)
        {
            if (registry.TryResolve<ICheatSelection>(out var cheats)) return cheats;
            throw new InvalidOperationException(
                $"{name}: {nameof(ICheatSelection)} is missing. Core's Boot registers it before any installer runs (dev builds).");
        }
    }

    /// <summary>The states of node feature-team-select/cheat, in the order of its ideas.</summary>
    internal enum TeamSelectCheatState
    {
        None,
        /// <summary>The screen is open, the remembered team is Knight, Axeman, Archer, the Enter battle button lit.</summary>
        FullTeam,
    }

    /// <summary>Names and keys of this feature's cheat states, for the cheat list (Core's ICheatStateSource).</summary>
    internal static class TeamSelectCheats
    {
        /// <summary>Title of the design node feature-team-select.</summary>
        internal const string FeatureTitle = "Chọn đội";

        internal const string FullTeamKey = "TeamSelect/FullTeam";

        /// <summary>In the order of the ideas of node feature-team-select/cheat, titled as those ideas.</summary>
        internal static readonly IReadOnlyList<CheatState> States = new[]
        {
            new CheatState(FullTeamKey, "Màn chọn đội đang mở, đội nhớ đã điền đủ 3 chỗ, nút \"Vào trận\" sáng"),
        };

        /// <summary>The state a picked key stands for: <see cref="TeamSelectCheatState.None"/> when it is not this feature's (or normal play).</summary>
        internal static TeamSelectCheatState StateOf(string pickedKey) =>
            pickedKey == FullTeamKey ? TeamSelectCheatState.FullTeam : TeamSelectCheatState.None;

        internal static string KeyOf(TeamSelectCheatState state) => state switch
        {
            TeamSelectCheatState.FullTeam => FullTeamKey,
            _ => throw new ArgumentOutOfRangeException(nameof(state), state, "not a cheat state"),
        };
    }
}
#endif
