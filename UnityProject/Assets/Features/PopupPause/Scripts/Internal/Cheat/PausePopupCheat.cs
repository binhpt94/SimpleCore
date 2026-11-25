#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using Game.Core;
using Game.Run;

namespace Game.PopupPause
{
    /// <summary>
    /// Dev only (feature-popup-pause/cheat): as soon as a battle runs, pauses it through the run's own Pause and,
    /// for the confirm state, presses "Bỏ lượt" through the popup's own path. Acts once per session.
    /// The wave shown behind the popup is the battle's business (the run's cheat), not this one.
    /// </summary>
    internal sealed class PausePopupCheat : SessionParticipantBase
    {
        private readonly IRunFlow _run;
        private readonly PausePopupPresenter _presenter;
        private readonly PopupPauseCheatState _state;
        private bool _applied;

        public PausePopupCheat(IRunFlow run, PausePopupPresenter presenter, PopupPauseCheatState state)
        {
            _run = run ?? throw new ArgumentNullException(nameof(run));
            _presenter = presenter ?? throw new ArgumentNullException(nameof(presenter));
            _state = state;
        }

        protected override void OnRun()
        {
            if (_state == PopupPauseCheatState.None) return;
            _run.BattleStarted += Apply;
            WhenDisposed(() => _run.BattleStarted -= Apply);
            if (_run.Step == RunStep.Playing || _run.Step == RunStep.Paused) Apply();
        }

        private void Apply()
        {
            if (_applied) return;
            _applied = true;
            _run.Pause();
            if (_state == PopupPauseCheatState.PausedConfirm) _presenter.PressQuit();
        }
    }
}
#endif
