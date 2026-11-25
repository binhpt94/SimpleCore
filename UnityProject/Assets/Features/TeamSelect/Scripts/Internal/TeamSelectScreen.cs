using System;
using System.Collections.Generic;

namespace Game.TeamSelect
{
    /// <summary>
    /// <see cref="ITeamSelectScreen"/> as the screen flow sees it: forwards to the presenter, which holds the rules.
    /// Registered by the installer so the flow can find it before the session's run step creates the view.
    /// </summary>
    internal sealed class TeamSelectScreen : ITeamSelectScreen
    {
        private readonly TeamSelectPresenter _presenter;

        public TeamSelectScreen(TeamSelectPresenter presenter)
        {
            _presenter = presenter ?? throw new ArgumentNullException(nameof(presenter));
        }

        public void Show() => _presenter.Show();

        public void Hide() => _presenter.Hide();

        public void SetEnterBattleEnabled(bool enabled) => _presenter.SetEnterBattleEnabled(enabled);

        public void ShowBuildError(string text) => _presenter.ShowBuildError(text);

        public void HideBuildError() => _presenter.HideBuildError();

        public void RememberTeam(IReadOnlyList<string> heroCodes) => _presenter.RememberTeam(heroCodes);

        public event Action<IReadOnlyList<string>> EnterBattlePressed
        {
            add => _presenter.EnterBattlePressed += value;
            remove => _presenter.EnterBattlePressed -= value;
        }

        public event Action BackPressed
        {
            add => _presenter.BackPressed += value;
            remove => _presenter.BackPressed -= value;
        }
    }
}
