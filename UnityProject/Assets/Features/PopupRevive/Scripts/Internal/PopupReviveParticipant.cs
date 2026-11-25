using Game.Ads;
using Game.Core;
using Game.Run;
using Game.Services.Popup;
using Game.SimClock;

namespace Game.PopupRevive
{
    /// <summary>The revive popup's part of the scene session: follows the run while the session runs.</summary>
    internal sealed class PopupReviveParticipant : SessionParticipantBase
    {
        private readonly RevivePopupPresenter _presenter;

        public PopupReviveParticipant(
            IRunFlow flow, IRunRevive revive, IRewardedAds ads, IPopupService popups, IPopupViews views,
            ISimClock clock)
        {
            _presenter = new RevivePopupPresenter(flow, revive, ads, popups, views, clock);
        }

        internal RevivePopupPresenter Presenter => _presenter;

        protected override void OnRun()
        {
            _presenter.Start();
            WhenDisposed(_presenter.Dispose);
        }
    }
}
