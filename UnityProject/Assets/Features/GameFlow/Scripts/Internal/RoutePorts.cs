using Game.Run;

namespace Game.GameFlow
{
    /// <summary>Shows the main screens. Adapters to the screens' owners sit behind it.</summary>
    internal interface IMainScreens
    {
        /// <summary>Hides every other main screen and shows <paramref name="screen"/>, all within this one call (same frame).</summary>
        void Show(MainScreen screen);
    }

    /// <summary>The parts of the team select screen the flow drives while a battle is being built.</summary>
    internal interface ITeamSelectPanel
    {
        void SetEnterBattleEnabled(bool enabled);

        void ShowBuildError(string text);

        void HideBuildError();
    }

    /// <summary>
    /// Builds a battle for a team. Reports back through <see cref="ScreenRouter.BattleBuilt"/> or
    /// <see cref="ScreenRouter.BattleBuildFailed"/>, possibly from inside <see cref="Build"/>.
    /// </summary>
    internal interface IBattleBuilder
    {
        void Build(RunTeam team);

        /// <summary>The build ran past its time limit: throw away whatever it made.</summary>
        void Cancel();
    }

    /// <summary>The remembered team; it changes only when a battle was built for a confirmed team.</summary>
    internal interface ITeamMemory
    {
        void Remember(RunTeam team);
    }

    internal interface IFlowLog
    {
        void Warning(string message);

        void Error(string message);
    }
}
