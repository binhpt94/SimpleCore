using Game.Core;
using Game.Run;

namespace Game.Battlefield
{
    /// <summary>
    /// The backdrop's life in the Game scene: it appears when the battle is set up and goes when the player leaves
    /// the battle (or the scene ends). Pause, castle destroyed and revive change nothing.
    /// </summary>
    internal sealed class BattlefieldParticipant : SessionParticipantBase
    {
        private readonly IRunFlow _run;
        private readonly BackdropLayout _layout;
        private readonly IBackdropViewFactory _views;
        private IBackdropView _view;

        public BattlefieldParticipant(IRunFlow run, BackdropLayout layout, IBackdropViewFactory views)
        {
            _run = run;
            _layout = layout;
            _views = views;
        }

        internal bool IsShown => _view != null;

        protected override void OnRun()
        {
            _run.BattleStarted += Show;
            _run.BattleCleared += Hide;
            WhenDisposed(() =>
            {
                _run.BattleStarted -= Show;
                _run.BattleCleared -= Hide;
            });
            WhenDisposed(Hide);

            if (InBattle(_run.Step)) Show();
        }

        private static bool InBattle(RunStep step) =>
            step == RunStep.Playing || step == RunStep.Paused || step == RunStep.Ending || step == RunStep.Lost;

        private void Show()
        {
            if (_view != null) return;
            _view = _views.Create();
            _view.Show(_layout.Build());
        }

        private void Hide()
        {
            if (_view == null) return;
            var view = _view;
            _view = null;
            _views.Destroy(view);
        }
    }
}
