using System;
using Game.Core;
using Game.Run;
using Game.Services.Popup;

namespace Game.PopupRating
{
    /// <summary>
    /// The rating invite in one Game scene session.
    /// <para>Timing (node rating-timing/timing-spec): counts invite_delay real seconds from
    /// <see cref="IRunFlow.BattleStopped"/> (the run settled the loss), not counting time out of the app. Then
    /// invites only when the run set a new best and this app launch has shown fewer than max_invites_per_launch
    /// invites. <see cref="IRunFlow.BattleCleared"/> (the player chose on the lose popup) cancels the count.
    /// The invite counts once the popup is shown; a refusal of the popup service does not count.</para>
    /// <para>Invite (node rating-invite/invite-spec): first touch only. "Đánh giá" closes the popup, then opens the
    /// game's Google Play page (app, else browser, else only a log error). "Để sau" and the back key while the
    /// popup is on top close it. The popup never closes on the backdrop: the popup service ignores it.</para>
    /// </summary>
    internal sealed class RatingParticipant : SessionParticipantBase
    {
        private readonly IRunFlow _flow;
        private readonly IRunScore _score;
        private readonly IPopupService _popups;
        private readonly IPopupViews _views;
        private readonly IStorePage _store;
        private readonly IRatingClock _clock;
        private readonly IRatingLog _log;
        private readonly RatingConfig _config;
        private readonly RatingLaunchMemory _memory;

        private IDisposable _clockRun;
        private IDisposable _jobUsage;
        private float _elapsed;
        private bool _appActive = true;
        private bool _skipNextTick;

        private PopupHandle _handle;
        private IRatingView _view;
        private IDisposable _viewUsage;
        private bool _choiceTaken;

        internal RatingParticipant(IRunFlow flow, IRunScore score, IPopupService popups, IPopupViews views,
            IStorePage store, IRatingClock clock, IRatingLog log, RatingConfig config, RatingLaunchMemory memory)
        {
            _flow = flow ?? throw new ArgumentNullException(nameof(flow));
            _score = score ?? throw new ArgumentNullException(nameof(score));
            _popups = popups ?? throw new ArgumentNullException(nameof(popups));
            _views = views ?? throw new ArgumentNullException(nameof(views));
            _store = store ?? throw new ArgumentNullException(nameof(store));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _log = log ?? throw new ArgumentNullException(nameof(log));
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _memory = memory ?? throw new ArgumentNullException(nameof(memory));
        }

        internal bool IsCounting => _clockRun != null;
        internal bool IsOpen => _handle.IsValid;

        protected override void OnRun()
        {
            _flow.BattleStopped += OnBattleStopped;
            _flow.BattleCleared += OnBattleCleared;
            WhenDisposed(() => _flow.BattleStopped -= OnBattleStopped);
            WhenDisposed(() => _flow.BattleCleared -= OnBattleCleared);
            WhenDisposed(StopCounting);
            WhenDisposed(Close);   // scene ends: the popup goes silently, no choice is reported
        }

        private void OnBattleStopped()
        {
            StopCounting();
            _elapsed = 0f;
            _appActive = true;
            _skipNextTick = false;
            _jobUsage = TrackJob();
            _clockRun = _clock.Start(OnTick, OnAppActive);
        }

        private void OnBattleCleared() => StopCounting();

        private void OnAppActive(bool active)
        {
            // Back in front: the first frame's delta still holds the time spent outside; skip it.
            if (active && !_appActive) _skipNextTick = true;
            _appActive = active;
        }

        private void OnTick(float seconds)
        {
            if (!_appActive) return;
            if (_skipNextTick)
            {
                _skipNextTick = false;
                return;
            }
            _elapsed += seconds;
            if (_elapsed < _config.InviteDelay) return;
            StopCounting();
            if (!_score.IsNewBest) return;
            if (_memory.InvitesShown >= _config.MaxInvitesPerLaunch) return;
            Open();
        }

        private void StopCounting()
        {
            _clockRun?.Dispose();
            _clockRun = null;
            _jobUsage?.Dispose();
            _jobUsage = null;
        }

        /// <summary>
        /// Asks the popup service for the invite now and counts it when shown. True when shown.
        /// The dev cheat calls it directly, skipping the timing rules.
        /// </summary>
        internal bool Open()
        {
            if (IsOpen) return true;
            var handle = _popups.Show(RatingPopupIds.Main);
            if (!handle.IsValid)
            {
                _log.Warning($"the popup service refused {RatingPopupIds.Main}; no invite this time.");
                return false;
            }
            if (!_views.TryGetView<IRatingView>(handle, out var view))
            {
                _log.Error($"popup {RatingPopupIds.Main} has no {nameof(IRatingView)} on its root; closing it.");
                _popups.Close(handle);
                return false;
            }

            _handle = handle;
            _view = view;
            if (view is UnityEngine.Object unityView) _viewUsage = TrackView(unityView);
            _choiceTaken = false;
            _memory.InvitesShown++;
            _view.ShowText(_config.InviteText, _config.RateLabel, _config.LaterLabel);
            _view.RatePressed += OnRatePressed;
            _view.LaterPressed += OnLaterPressed;
            _view.BackPressed += OnBackPressed;
            return true;
        }

        private void OnRatePressed()
        {
            if (!TakeFirstChoice()) return;
            Close();
            OpenStorePage();
        }

        private void OnLaterPressed()
        {
            if (TakeFirstChoice()) Close();
        }

        private void OnBackPressed()
        {
            if (TakeFirstChoice()) Close();
        }

        /// <summary>True for the first touch while the invite is on top; then both buttons lock.</summary>
        private bool TakeFirstChoice()
        {
            if (_choiceTaken || !IsOpen || !_views.IsOnTop(_handle)) return false;
            _choiceTaken = true;
            _view.LockButtons();
            return true;
        }

        private void OpenStorePage()
        {
            var package = _store.PackageName;
            switch (_config.StoreOpen)
            {
                case StoreOpenOrder.AppThenBrowser:
                    if (_store.TryOpenApp(package)) return;
                    if (_store.TryOpenBrowser(package)) return;
                    break;
            }
            _log.Error($"could not open the Google Play page of '{package}' in the app or a browser.");
        }

        private void Close()
        {
            if (!IsOpen) return;
            var handle = _handle;
            _view.RatePressed -= OnRatePressed;
            _view.LaterPressed -= OnLaterPressed;
            _view.BackPressed -= OnBackPressed;
            _view = null;
            _viewUsage?.Dispose();
            _viewUsage = null;
            _handle = default;
            _popups.Close(handle);
        }
    }
}
