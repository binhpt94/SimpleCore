#if UNITY_EDITOR || DEVELOPMENT_BUILD
namespace Game.Run
{
    /// <summary>Dev builds only: holds a cheat state's request to open the Game scene straight in battle.</summary>
    internal sealed class RunCheatStart : IRunCheatStart
    {
        private readonly RunTeam _defaultTeam;
        private RunTeam _asked;

        public RunCheatStart(RunTeam defaultTeam) => _defaultTeam = defaultTeam;

        public bool BattleRequested { get; private set; }

        public RunTeam Team => _asked.Count > 0 ? _asked : _defaultTeam;

        public void RequestBattle(RunTeam team)
        {
            BattleRequested = true;
            if (team.Count > 0) _asked = team;
        }
    }
}
#endif
