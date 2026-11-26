using System;
using Game.Home;
using Game.Run;
using Game.TeamSelect;

namespace Game.GameFlow
{
    /// <summary>
    /// Shows one main screen through the screens' owners. Only the screen shown last is hidden, so a screen that was
    /// never shown is never asked to hide. The battle screen belongs to the run: showing it hides the other two.
    /// </summary>
    internal sealed class MainScreensAdapter : IMainScreens
    {
        private readonly IHomeScreen _home;
        private readonly ITeamSelectScreen _teamSelect;
        private MainScreen _shown = MainScreen.None;

        public MainScreensAdapter(IHomeScreen home, ITeamSelectScreen teamSelect)
        {
            _home = home ?? throw new ArgumentNullException(nameof(home));
            _teamSelect = teamSelect ?? throw new ArgumentNullException(nameof(teamSelect));
        }

        public void Show(MainScreen screen)
        {
            if (screen == _shown) return;

            if (_shown == MainScreen.Home) _home.Hide();
            else if (_shown == MainScreen.TeamSelect) _teamSelect.Hide();

            if (screen == MainScreen.Home) _home.Show();
            else if (screen == MainScreen.TeamSelect) _teamSelect.Show();

            _shown = screen;
        }
    }

    /// <summary>The team select parts the flow drives while a battle is being built.</summary>
    internal sealed class TeamSelectPanelAdapter : ITeamSelectPanel
    {
        private readonly ITeamSelectScreen _screen;

        public TeamSelectPanelAdapter(ITeamSelectScreen screen) =>
            _screen = screen ?? throw new ArgumentNullException(nameof(screen));

        public void SetEnterBattleEnabled(bool enabled) => _screen.SetEnterBattleEnabled(enabled);

        public void ShowBuildError(string text) => _screen.ShowBuildError(text);

        public void HideBuildError() => _screen.HideBuildError();
    }

    /// <summary>The remembered team lives on team select; the flow tells it when a battle was built.</summary>
    internal sealed class TeamMemoryAdapter : ITeamMemory
    {
        private readonly ITeamSelectScreen _screen;

        public TeamMemoryAdapter(ITeamSelectScreen screen) =>
            _screen = screen ?? throw new ArgumentNullException(nameof(screen));

        public void Remember(RunTeam team) => _screen.RememberTeam(team.HeroCodes);
    }

    /// <summary>Asks the run for a battle; the run's answers come back through <see cref="GameFlowParticipant"/>.</summary>
    internal sealed class BattleBuilderAdapter : IBattleBuilder
    {
        private readonly IRunBattle _battle;

        public BattleBuilderAdapter(IRunBattle battle) =>
            _battle = battle ?? throw new ArgumentNullException(nameof(battle));

        public void Build(RunTeam team) => _battle.BuildBattle(team);

        public void Cancel() => _battle.CancelBuild();
    }

    /// <summary>Maps the run's leave choice to the flow's.</summary>
    internal static class LeaveChoices
    {
        public static LeaveChoice From(RunLeaveChoice choice)
        {
            switch (choice)
            {
                case RunLeaveChoice.PlayAgain: return LeaveChoice.Replay;
                case RunLeaveChoice.BackToStart: return LeaveChoice.GoHome;
                default: throw new ArgumentOutOfRangeException(nameof(choice), choice, "Unknown leave choice");
            }
        }
    }
}
