#if UNITY_EDITOR || DEVELOPMENT_BUILD
namespace Game.Run
{
    /// <summary>
    /// Dev builds only. A cheat state that is shown in battle asks for a battle while its feature installs; the
    /// screen flow then skips the start screen and team select and builds the battle as soon as the scene is ready.
    /// One per scene load (the run registers a fresh one each time the Game scene boots). No game rules.
    /// </summary>
    public interface IRunCheatStart
    {
        /// <summary>
        /// Asks for the scene to open straight in battle with <paramref name="team"/>; an empty team means the run's
        /// default team. Call during Install. A later call with a non-empty team replaces the team.
        /// </summary>
        void RequestBattle(RunTeam team);

        /// <summary>True once some cheat state asked for a battle in this scene load.</summary>
        bool BattleRequested { get; }

        /// <summary>The team to build the battle with: the last non-empty team asked for, otherwise the run's default team.</summary>
        RunTeam Team { get; }
    }
}
#endif
