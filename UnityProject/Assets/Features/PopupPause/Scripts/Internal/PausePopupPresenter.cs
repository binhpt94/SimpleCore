using System;
using Game.Run;
using Game.Services.Popup;

namespace Game.PopupPause
{
    /// <summary>
    /// Rules of the pause popup (feature-popup-pause/pause-popup/pause-popup-spec): the popup is the face of the
    /// run's Paused step. It opens at the main step when the run pauses and closes when the run leaves Paused.
    /// It switches between its two steps itself and reports the final choice (resume or abandon) to the run once.
    /// </summary>
    internal sealed class PausePopupPresenter
    {
        private readonly IRunFlow _run;
        private readonly IPopupService _popups;
        private readonly IPopupViews _views;
        private readonly IPausePopupLog _log;
        private readonly IFrameClock _clock;

        private bool _started;
        private int _openedFrame;
        private PopupHandle _handle;
        private IPausePopupView _view;
        private bool _reported;

        public PausePopupPresenter(
            IRunFlow run, IPopupService popups, IPopupViews views, IPausePopupLog log, IFrameClock clock)
        {
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _run = run ?? throw new ArgumentNullException(nameof(run));
            _popups = popups ?? throw new ArgumentNullException(nameof(popups));
            _views = views ?? throw new ArgumentNullException(nameof(views));
            _log = log ?? throw new ArgumentNullException(nameof(log));
        }

        public bool IsOpen => _handle.IsValid;

        /// <summary>The step shown while the popup is open.</summary>
        public PausePopupStep Step { get; private set; } = PausePopupStep.Main;

        /// <summary>Starts following the run; opens at once when the run is already paused.</summary>
        public void Start()
        {
            if (_started) return;
            _started = true;
            _run.StepChanged += OnRunStepChanged;
            if (_run.Step == RunStep.Paused) Open();
        }

        /// <summary>Stops following the run and lets go of the popup.</summary>
        public void Stop()
        {
            if (!_started) return;
            _started = false;
            _run.StepChanged -= OnRunStepChanged;
            Close();
        }

        /// <summary>Main step, "Tiếp tục".</summary>
        public void PressResume()
        {
            if (!CanAct || Step != PausePopupStep.Main) return;
            Report();
            _run.Resume();
        }

        /// <summary>Main step, "Bỏ lượt": switches to the confirm step in the same popup.</summary>
        public void PressQuit()
        {
            if (!CanAct || Step != PausePopupStep.Main) return;
            ShowStep(PausePopupStep.ConfirmQuit);
        }

        /// <summary>Confirm step, "Bỏ lượt".</summary>
        public void PressConfirmQuit()
        {
            if (!CanAct || Step != PausePopupStep.ConfirmQuit) return;
            Report();
            _run.Abandon();
        }

        /// <summary>Confirm step, "Không": back to the main step; the run stays paused.</summary>
        public void PressCancelQuit()
        {
            if (!CanAct || Step != PausePopupStep.ConfirmQuit) return;
            ShowStep(PausePopupStep.Main);
        }

        /// <summary>
        /// Device back key: "Tiếp tục" at the main step, "Không" at the confirm step. Only for the top popup.
        /// Ignored in the frame the popup opened: that press is the one that paused the run.
        /// </summary>
        public void PressBack()
        {
            if (!CanAct || !_views.IsOnTop(_handle)) return;
            if (_clock.Frame == _openedFrame) return;
            if (Step == PausePopupStep.Main) PressResume();
            else PressCancelQuit();
        }

        private bool CanAct => _handle.IsValid && !_reported;

        private void OnRunStepChanged(RunStep step)
        {
            if (step == RunStep.Paused) Open();
            else Close();
        }

        private void Open()
        {
            // Already open (the player left the app while paused): keep the popup and its step.
            if (_handle.IsValid) return;

            var handle = _popups.Show(PausePopupIds.Main);
            if (!handle.IsValid) return; // the popup service logged why

            if (!_views.TryGetView(handle, out IPausePopupView view))
            {
                _log.Error($"PopupPause: the prefab of popup '{PausePopupIds.Main}' has no PausePopupView on its root.");
                _popups.Close(handle);
                return;
            }

            _handle = handle;
            _view = view;
            _reported = false;
            _openedFrame = _clock.Frame;
            _view.ResumePressed += PressResume;
            _view.QuitPressed += PressQuit;
            _view.ConfirmQuitPressed += PressConfirmQuit;
            _view.CancelQuitPressed += PressCancelQuit;
            _view.BackPressed += PressBack;
            _view.SetInteractable(true);
            ShowStep(PausePopupStep.Main);
        }

        private void Close()
        {
            if (!_handle.IsValid) return;
            var handle = _handle;
            _view.ResumePressed -= PressResume;
            _view.QuitPressed -= PressQuit;
            _view.ConfirmQuitPressed -= PressConfirmQuit;
            _view.CancelQuitPressed -= PressCancelQuit;
            _view.BackPressed -= PressBack;
            _view = null;
            _handle = default;
            Step = PausePopupStep.Main;
            _popups.Close(handle);
        }

        private void ShowStep(PausePopupStep step)
        {
            Step = step;
            _view.ShowStep(step);
        }

        private void Report()
        {
            _reported = true;
            _view.SetInteractable(false);
        }
    }
}
