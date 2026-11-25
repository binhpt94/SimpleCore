using System;
using Game.Core;
using Game.Hero;

namespace Game.TeamSelect
{
    /// <summary>
    /// The team select screen's part of the Game scene session. The presenter exists from install, so the screen
    /// flow can drive it through <see cref="ITeamSelectScreen"/>; on Run the view is created and attached, at the end
    /// the view is destroyed and the presenter lets go of its listeners. The remembered team lives here.
    /// </summary>
    internal sealed class TeamSelectSession : SessionParticipantBase
    {
        private readonly ITeamSelectViewFactory _views;

        public TeamSelectSession(IHeroRoster roster, TeamSelectRules rules, ITeamSelectViewFactory views)
        {
            if (roster == null) throw new ArgumentNullException(nameof(roster));
            if (rules == null) throw new ArgumentNullException(nameof(rules));
            _views = views ?? throw new ArgumentNullException(nameof(views));
            Rules = rules;
            Presenter = new TeamSelectPresenter(roster, rules, Memory);
        }

        public TeamMemory Memory { get; } = new TeamMemory();

        public TeamSelectRules Rules { get; }

        /// <summary>The screen's rules and state; the screen flow reaches it through <see cref="TeamSelectScreen"/>.</summary>
        public TeamSelectPresenter Presenter { get; }

        /// <summary>True from the run step until the session ends: the view exists.</summary>
        public bool HasView { get; private set; }

        protected override bool OpensViewsOnRun => true;

        protected override void OnRun()
        {
            var view = _views.Create();
            if (view == null) throw new InvalidOperationException("TeamSelect: the view factory returned no view.");
            WhenDisposed(() => _views.Destroy(view));

            // Counted for the session's clean-up check. A view that is not a Unity object (a test fake) holds nothing.
            var tracked = _views.HolderOf(view);
            if (tracked != null)
            {
                var held = TrackView(tracked);
                WhenDisposed(held.Dispose);
            }

            Presenter.Attach(view, view as IHeroPortraits);
            HasView = true;
            WhenDisposed(() =>
            {
                HasView = false;
                Presenter.Dispose();
            });
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        /// <summary>Dev only: state <c>full-team</c> of feature-team-select/cheat.</summary>
        public void ApplyCheatFullTeam() => TeamSelectCheat.ApplyFullTeam(Presenter, Memory, Rules.DefaultTeam);
#endif
    }
}
