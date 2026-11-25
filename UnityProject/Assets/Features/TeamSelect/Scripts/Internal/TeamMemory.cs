using System;
using System.Collections.Generic;

namespace Game.TeamSelect
{
    /// <summary>
    /// The remembered team (design: team-memory/memory-rules; glossary: remembered-team): the team that last
    /// entered battle, slot order kept. Lives only while the game is open; nothing is written to disk, so a new
    /// game starts with no remembered team and the default team is used.
    /// </summary>
    internal sealed class TeamMemory
    {
        private string[] _team;

        public bool HasTeam => _team != null;

        /// <summary>The remembered team, or null when none.</summary>
        public IReadOnlyList<string> Team => _team;

        /// <summary>The team entered battle: it becomes the remembered team.</summary>
        public void Remember(IReadOnlyList<string> team)
        {
            if (team == null) throw new ArgumentNullException(nameof(team));
            var copy = new string[team.Count];
            for (var i = 0; i < copy.Length; i++) copy[i] = team[i];
            _team = copy;
        }

        /// <summary>The team to put in the slots when the screen opens: the remembered one, else the default.</summary>
        public IReadOnlyList<string> TeamToShow(IReadOnlyList<string> defaultTeam) => _team ?? defaultTeam;
    }
}
