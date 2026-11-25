using System;
using Game.Core;

namespace Game.PopupPause
{
    /// <summary>Starts the pause popup when the session runs and lets go of it when the session ends.</summary>
    internal sealed class PopupPauseParticipant : SessionParticipantBase
    {
        private readonly PausePopupPresenter _presenter;

        public PopupPauseParticipant(PausePopupPresenter presenter)
        {
            _presenter = presenter ?? throw new ArgumentNullException(nameof(presenter));
        }

        protected override void OnRun()
        {
            _presenter.Start();
            WhenDisposed(_presenter.Stop);
        }
    }
}
