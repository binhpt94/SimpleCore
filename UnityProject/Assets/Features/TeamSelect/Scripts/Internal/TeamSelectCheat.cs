#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System.Collections.Generic;

namespace Game.TeamSelect
{
    /// <summary>Dev-only states of the team select screen (design: feature-team-select/cheat).</summary>
    internal static class TeamSelectCheat
    {
        public const string FullTeam = "full-team";

        /// <summary>
        /// State <c>full-team</c>: the remembered team is the default team (Knight, Axeman, Archer in slots 1-3),
        /// poured in through the normal memory path, then the screen opens: 3 cards picked, 2 not, button lit.
        /// </summary>
        public static void ApplyFullTeam(TeamSelectPresenter screen, TeamMemory memory, IReadOnlyList<string> team)
        {
            memory.Remember(team);
            screen.Open();
        }
    }
}
#endif
