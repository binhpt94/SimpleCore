using System;
using Game.Core;
using Game.Run;
using Game.Services.Popup;

namespace Game.PopupLose
{
    /// <summary>
    /// Opens the lose popup while the run is at <see cref="RunStep.Lost"/> and closes it when the run leaves that step.
    /// "Về màn bắt đầu" is shown and wired only when <c>homeButton</c> is on (off at IDEA: no start screen yet, node actions-spec).
    /// Reads score, best, the new-best flag and the failed-revive flag once, when the popup opens. The first touch on either button
    /// ("Chơi lại" → Replay, "Về màn bắt đầu" → GoHome) is reported to the run, then both buttons are locked. The popup never closes itself: the run moves on and this class closes it.
    /// </summary>
    internal sealed class LosePopupParticipant : SessionParticipantBase
    {
        private readonly IRunFlow _flow;
        private readonly IRunRevive _revive;
        private readonly IRunScore _score;
        private readonly IPopupService _popups;
        private readonly IPopupViews _views;
        private readonly ILoseLog _log;
        private readonly bool _homeButton;

        private PopupHandle _handle;
        private ILoseView _view;
        private IDisposable _viewUsage;
        private bool _choiceSent;

        internal LosePopupParticipant(IRunFlow flow, IRunRevive revive, IRunScore score, IPopupService popups,
            IPopupViews views, ILoseLog log, bool homeButton)
        {
            _homeButton = homeButton;
            _flow = flow ?? throw new ArgumentNullException(nameof(flow));
            _revive = revive ?? throw new ArgumentNullException(nameof(revive));
            _score = score ?? throw new ArgumentNullException(nameof(score));
            _popups = popups ?? throw new ArgumentNullException(nameof(popups));
            _views = views ?? throw new ArgumentNullException(nameof(views));
            _log = log ?? throw new ArgumentNullException(nameof(log));
        }

        internal bool IsOpen => _handle.IsValid;

        protected override void OnRun()
        {
            _flow.StepChanged += OnStepChanged;
            WhenDisposed(() => _flow.StepChanged -= OnStepChanged);
            WhenDisposed(Close);
            if (_flow.Step == RunStep.Lost) Open();
        }

        /// <summary>Opens the popup now, whatever the run step. Used by the dev cheat; normal play goes through the step.</summary>
        internal void Open()
        {
            if (IsOpen) return;
            var handle = _popups.Show(LosePopupIds.Main);
            if (!handle.IsValid) return;            // the popup service already logged why

            if (!_views.TryGetView<ILoseView>(handle, out var view))
            {
                _log.Error($"popup {LosePopupIds.Main} has no {nameof(ILoseView)} on its root; closing it.");
                _popups.Close(handle);
                return;
            }

            _handle = handle;
            _view = view;
            // Held views are counted for the session's clean-up check (proposal 2026-11-25-session-usage-tracking).
            if (view is UnityEngine.Object unityView) _viewUsage = TrackView(unityView);
            _choiceSent = false;
            _view.ShowResult(LoseResultText.From(_score.Score, _score.Best, _score.IsNewBest, _revive.LostAfterFailedRevive));
            _view.ShowHomeButton(_homeButton);
            _view.ReplayPressed += OnReplayPressed;
            if (_homeButton) _view.HomePressed += OnHomePressed;
        }

        private void OnStepChanged(RunStep step)
        {
            // Lost again while open (e.g. the app came back to the front): Open() keeps the popup exactly as it was.
            if (step == RunStep.Lost) Open();
            else Close();
        }

        private void OnReplayPressed()
        {
            if (TakeFirstChoice()) _flow.Replay();
        }

        private void OnHomePressed()
        {
            if (TakeFirstChoice()) _flow.GoHome();
        }

        /// <summary>True for the first touch on either button while this popup is on top; then both buttons lock.</summary>
        private bool TakeFirstChoice()
        {
            if (_choiceSent || !_views.IsOnTop(_handle)) return false;
            _choiceSent = true;
            _view.LockButtons();
            return true;
        }

        private void Close()
        {
            if (!IsOpen) return;
            var handle = _handle;
            _view.ReplayPressed -= OnReplayPressed;
            _view.HomePressed -= OnHomePressed;
            _view = null;
            _viewUsage?.Dispose();
            _viewUsage = null;
            _handle = default;
            _popups.Close(handle);
        }
    }
}
