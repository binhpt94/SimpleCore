using System;
using Game.Core;

namespace Game.Home
{
    /// <summary>The start screen's part of the Game scene session: builds the view in Run, removes it when the session ends.</summary>
    internal sealed class HomeParticipant : SessionParticipantBase
    {
        private readonly HomeScreenPresenter _presenter;
        private readonly HomeConfig _config;
        private readonly IHomeScreenFactory _factory;

        public HomeParticipant(HomeScreenPresenter presenter, HomeConfig config, IHomeScreenFactory factory)
        {
            _presenter = presenter ?? throw new ArgumentNullException(nameof(presenter));
            _config = config ? config : throw new ArgumentNullException(nameof(config));
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
        }

        /// <summary>The game opens on the start screen, so its view is built in the run step.</summary>
        protected override bool OpensViewsOnRun => true;

        protected override void OnRun()
        {
            var display = _factory.Create(HomeLayout.From(_config));
            var tracked = TrackView(display.Root);
            WhenDisposed(display.Dispose);
            WhenDisposed(tracked.Dispose);
            _presenter.Attach(display);
            WhenDisposed(_presenter.Detach);
        }
    }
}
