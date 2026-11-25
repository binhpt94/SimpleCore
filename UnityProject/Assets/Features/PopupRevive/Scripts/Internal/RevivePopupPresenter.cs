using System;
using Game.Ads;
using Game.Run;
using Game.Services.Popup;
using Game.SimClock;

namespace Game.PopupRevive
{
    /// <summary>
    /// Runs the revive popup (live: feature-popup-revive/revive-popup/revive-spec). The popup shows itself when the
    /// run enters <see cref="RunStep.ReviveOffer"/>. Only the first tap of each opening counts: "Xem quảng cáo" sends
    /// one ad request and waits with both buttons locked; "Kết thúc lượt" closes. A result closes the popup at once.
    /// The popup always closes first and reports to the run after. The back key does nothing: no handler is given.
    /// The choice goes to the run through the shared clock's queue (feature-sim-clock#revive-result-queued): it is
    /// applied at the start of the next step and recorded with that step's number, so a replay hands the run the
    /// recorded result at the same step. The popup has no timer of its own: the ad wait has no time limit.
    /// </summary>
    internal sealed class RevivePopupPresenter : IDisposable
    {
        /// <summary>Owner name of the revive result in the clock's action record.</summary>
        internal const string ClockOwner = "PopupRevive";

        private enum State
        {
            Closed,
            Choosing,
            WaitingAd,
        }

        private readonly IRunFlow _flow;
        private readonly IRunRevive _revive;
        private readonly IRewardedAds _ads;
        private readonly IPopupService _popups;
        private readonly IPopupViews _views;
        private readonly ISimClock _clock;

        private State _state = State.Closed;
        private PopupHandle _handle;
        private IRevivePopupView _view;
        private int _lastOfferId;
        private bool _disposed;

        public RevivePopupPresenter(
            IRunFlow flow, IRunRevive revive, IRewardedAds ads, IPopupService popups, IPopupViews views,
            ISimClock clock)
        {
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _flow = flow ?? throw new ArgumentNullException(nameof(flow));
            _revive = revive ?? throw new ArgumentNullException(nameof(revive));
            _ads = ads ?? throw new ArgumentNullException(nameof(ads));
            _popups = popups ?? throw new ArgumentNullException(nameof(popups));
            _views = views ?? throw new ArgumentNullException(nameof(views));
        }

        /// <summary>True while the popup is open, waiting for a tap or an ad result.</summary>
        public bool IsOpen => _state != State.Closed;

        /// <summary>True while an ad request is out and both buttons are locked.</summary>
        public bool IsWaitingAd => _state == State.WaitingAd;

        /// <summary>The id sent with the current ad request; 0 when none was sent.</summary>
        public int CurrentOfferId => _state == State.WaitingAd ? _lastOfferId : 0;

        /// <summary>Starts following the run. Opens the popup at once if the run already offers a revive.</summary>
        public void Start()
        {
            _flow.StepChanged += OnStepChanged;
            if (_flow.Step == RunStep.ReviveOffer) Open();
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _flow.StepChanged -= OnStepChanged;
            CloseWithoutReport();
        }

        private void OnStepChanged(RunStep step)
        {
            if (step == RunStep.ReviveOffer)
            {
                if (_state == State.Closed) Open();
                return;
            }

            // The run left the offer on its own (left the battle): nothing to report any more.
            CloseWithoutReport();
        }

        private void Open()
        {
            var handle = _popups.Show(PopupReviveIds.Main);
            if (!handle.IsValid) return; // The popup service already logged why.

            if (!_views.TryGetView(handle, out IRevivePopupView view))
            {
                _popups.Close(handle);
                throw new InvalidOperationException(
                    $"Popup {PopupReviveIds.Main} has no component implementing {nameof(IRevivePopupView)} on its root.");
            }

            _handle = handle;
            _view = view;
            _view.SetTexts(
                RevivePopupTexts.Title, RevivePopupTexts.Gain, RevivePopupTexts.Waiting,
                RevivePopupTexts.Watch, RevivePopupTexts.End);
            _view.SetWaiting(false);
            _view.WatchTapped += OnWatchTapped;
            _view.EndTapped += OnEndTapped;
            _state = State.Choosing;
        }

        private void OnWatchTapped()
        {
            if (_state != State.Choosing) return;

            _state = State.WaitingAd;
            _view.SetWaiting(true);
            var offerId = ++_lastOfferId;
            // The reply may come inside Show (busy provider): the state is set before.
            _ads.Show(offerId, OnAdReply);
        }

        private void OnEndTapped()
        {
            if (_state != State.Choosing) return;

            ClosePopup();
            _clock.Enqueue(ClockOwner, _revive.DeclineRevive);
        }

        private void OnAdReply(AdReply reply)
        {
            if (_state != State.WaitingAd || reply.OfferId != _lastOfferId) return;

            ClosePopup();
            if (reply.Result == AdResult.Watched) _clock.Enqueue(ClockOwner, _revive.Revive);
            else _clock.Enqueue(ClockOwner, _revive.FailRevive);
        }

        private void CloseWithoutReport()
        {
            if (_state == State.Closed) return;
            if (_state == State.WaitingAd) _ads.Cancel(_lastOfferId);
            ClosePopup();
        }

        private void ClosePopup()
        {
            _state = State.Closed;
            if (_view != null)
            {
                _view.WatchTapped -= OnWatchTapped;
                _view.EndTapped -= OnEndTapped;
                _view = null;
            }

            var handle = _handle;
            _handle = default;
            _popups.Close(handle);
        }
    }
}
