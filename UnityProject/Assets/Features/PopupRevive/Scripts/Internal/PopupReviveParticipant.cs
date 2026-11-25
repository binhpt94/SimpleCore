using Game.Ads;
using Game.Core;
using Game.Run;
using Game.Services.Popup;

namespace Game.PopupRevive
{
    /// <summary>The revive popup's part of the scene session: follows the run while the session runs.</summary>
    internal sealed class PopupReviveParticipant : SessionParticipantBase
    {
        private readonly RevivePopupPresenter _presenter;

        public PopupReviveParticipant(
            IRunFlow flow, IRunRevive revive, IRewardedAds ads, IPopupService popups, IPopupViews views)
        {
            _presenter = new RevivePopupPresenter(flow, revive, ads, popups, views);
        }

        internal RevivePopupPresenter Presenter => _presenter;

        protected override void OnRun()
        {
            _presenter.Start();
            WhenDisposed(_presenter.Dispose);
        }
    }
}
