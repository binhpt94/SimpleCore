using System;
using Game.Core;
using Game.Run;

namespace Game.Home
{
    /// <summary>The start screen's part of the Game scene session: builds the view in Run, removes it when the session ends.</summary>
    internal sealed class HomeParticipant : SessionParticipantBase
    {
        private readonly IRunFlow _flow;
        private readonly IRunScore _score;
        private readonly HomeConfig _config;
        private readonly IHomeScreenFactory _factory;

        public HomeParticipant(IRunFlow flow, IRunScore score, HomeConfig config, IHomeScreenFactory factory)
        {
            _flow = flow ?? throw new ArgumentNullException(nameof(flow));
            _score = score ?? throw new ArgumentNullException(nameof(score));
            _config = config ? config : throw new ArgumentNullException(nameof(config));
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
        }

        internal HomeScreenPresenter Presenter { get; private set; }

        protected override void OnRun()
        {
            var display = _factory.Create(HomeLayout.From(_config));
            WhenDisposed(display.Dispose);
            Presenter = new HomeScreenPresenter(_flow, _score, _config, display);
            Presenter.Start();
            WhenDisposed(Presenter.Stop);
        }
    }
}
