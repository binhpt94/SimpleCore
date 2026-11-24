using System;
using Game.Core;
using Game.Run;
using Game.Services.Popup;

namespace Game.PopupLose
{
    /// <summary>
    /// Opens the lose popup while the run is at <see cref="RunStep.Lost"/> and closes it when the run leaves that step.
    /// Reads score, best and the new-best flag once, when the popup opens. The first button touch is reported to the
    /// run, then every button is locked. The popup never closes itself: the run moves on and this class closes it.
    /// </summary>
    internal sealed class LosePopupParticipant : SessionParticipantBase
    {
        private readonly IRunFlow _flow;
        private readonly IRunScore _score;
        private readonly IPopupService _popups;
        private readonly IPopupViews _views;
        private readonly ILoseLog _log;

        private PopupHandle _handle;
        private ILoseView _view;
        private bool _choiceSent;

        internal LosePopupParticipant(IRunFlow flow, IRunScore score, IPopupService popups, IPopupViews views, ILoseLog log)
        {
            _flow = flow ?? throw new ArgumentNullException(nameof(flow));
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
            _choiceSent = false;
            _view.ShowResult(LoseResultText.From(_score.Score, _score.Best, _score.IsNewBest));
            _view.ReplayPressed += OnReplayPressed;
        }

        private void OnStepChanged(RunStep step)
        {
            if (step == RunStep.Lost) Open();
            else Close();
        }

        private void OnReplayPressed()
        {
            if (_choiceSent || !_views.IsOnTop(_handle)) return;
            _choiceSent = true;
            _view.LockButtons();
            _flow.Replay();
        }

        private void Close()
        {
            if (!IsOpen) return;
            var handle = _handle;
            _view.ReplayPressed -= OnReplayPressed;
            _view = null;
            _handle = default;
            _popups.Close(handle);
        }
    }
}
