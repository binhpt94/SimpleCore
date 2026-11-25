using System;
using Game.Core;
using Game.Hero;
using Game.Run;

namespace Game.TeamSelect
{
    /// <summary>
    /// The team select screen's part of the Game scene session: on Run it creates the view and the presenter,
    /// at the end it lets both go. The remembered team lives here, so it lasts as long as the game is open.
    /// </summary>
    internal sealed class TeamSelectSession : SessionParticipantBase
    {
        private readonly IRunFlow _run;
        private readonly IHeroRoster _roster;
        private readonly TeamSelectRules _rules;
        private readonly ITeamSelectViewFactory _views;

        public TeamSelectSession(IRunFlow run, IHeroRoster roster, TeamSelectRules rules, ITeamSelectViewFactory views)
        {
            _run = run ?? throw new ArgumentNullException(nameof(run));
            _roster = roster ?? throw new ArgumentNullException(nameof(roster));
            _rules = rules ?? throw new ArgumentNullException(nameof(rules));
            _views = views ?? throw new ArgumentNullException(nameof(views));
        }

        public TeamMemory Memory { get; } = new TeamMemory();

        /// <summary>The running screen; null before Run and after the session ended.</summary>
        public TeamSelectPresenter Screen { get; private set; }

        protected override void OnRun()
        {
            var view = _views.Create();
            if (view == null) throw new InvalidOperationException("TeamSelect: the view factory returned no view.");
            WhenDisposed(() => _views.Destroy(view));

            Screen = new TeamSelectPresenter(_run, _roster, _rules, Memory, view as IHeroPortraits);
            WhenDisposed(() =>
            {
                Screen.Dispose();
                Screen = null;
            });
            Screen.Attach(view);
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        /// <summary>Dev only: state <c>full-team</c> of feature-team-select/cheat.</summary>
        public void ApplyCheatFullTeam()
        {
            if (Screen == null) return;
            TeamSelectCheat.ApplyFullTeam(Screen, Memory, _rules.DefaultTeam);
        }
#endif
    }
}
