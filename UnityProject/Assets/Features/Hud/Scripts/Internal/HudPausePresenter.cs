using System;
using Game.Castle;
using Game.Run;

namespace Game.Hud
{
    /// <summary>
    /// The pause button (feature-hud/hud-pause-button/pause-button-spec): a tap while the battle runs reports
    /// "pause pressed" to the run once. While paused, after the run ended, or in the frame the castle fell the tap
    /// reports nothing. The button never stops the battle or opens a popup itself.
    /// </summary>
    internal sealed class HudPausePresenter
    {
        private readonly IHudView _view;
        private readonly IRunFlow _run;
        private readonly ICastle _castle;
        private bool _started;

        public HudPausePresenter(IHudView view, IRunFlow run, ICastle castle)
        {
            _view = view ?? throw new ArgumentNullException(nameof(view));
            _run = run ?? throw new ArgumentNullException(nameof(run));
            _castle = castle ?? throw new ArgumentNullException(nameof(castle));
        }

        public void Start()
        {
            if (_started) return;
            _started = true;
            _view.PausePressed += OnPressed;
        }

        public void Stop()
        {
            if (!_started) return;
            _started = false;
            _view.PausePressed -= OnPressed;
        }

        private void OnPressed()
        {
            if (!_started) return;
            if (_run.Step != RunStep.Playing || _run.IsPaused) return;
            // The castle falling is handled first: a tap in that same frame is dropped.
            if (_castle.IsDestroyed) return;
            _run.Pause();
        }
    }
}
