using System;
using System.Globalization;
using Game.Run;

namespace Game.Home
{
    /// <summary>
    /// Rules of the start screen: shown while the run is at <see cref="RunStep.Home"/>, reads the best score
    /// each time it shows (and when it changes while shown), and reports Play once per showing.
    /// Moving to the next screen is the run's job.
    /// </summary>
    internal sealed class HomeScreenPresenter
    {
        private readonly IRunFlow _flow;
        private readonly IRunScore _score;
        private readonly HomeConfig _config;
        private readonly IHomeScreenDisplay _display;

        private bool _started;
        private bool _shown;
        private int _tapsLeft;

        public HomeScreenPresenter(IRunFlow flow, IRunScore score, HomeConfig config, IHomeScreenDisplay display)
        {
            _flow = flow ?? throw new ArgumentNullException(nameof(flow));
            _score = score ?? throw new ArgumentNullException(nameof(score));
            _config = config ? config : throw new ArgumentNullException(nameof(config));
            _display = display ?? throw new ArgumentNullException(nameof(display));
        }

        public bool IsShown => _shown;

        public void Start()
        {
            if (_started) return;
            _started = true;
            _flow.StepChanged += OnStepChanged;
            _score.ScoreChanged += OnScoreChanged;
            _display.PlayPressed += OnPlayPressed;
            OnStepChanged(_flow.Step);
        }

        public void Stop()
        {
            if (!_started) return;
            _started = false;
            _flow.StepChanged -= OnStepChanged;
            _score.ScoreChanged -= OnScoreChanged;
            _display.PlayPressed -= OnPlayPressed;
            if (_shown) _display.Hide();
            _shown = false;
        }

        /// <summary>The record line for <paramref name="best"/>: every digit, no shortening; none saved yet shows the default.</summary>
        public static string RecordText(HomeConfig config, int best)
        {
            var value = best < 0 ? config.RecordWhenNone : best;
            return string.Format(CultureInfo.InvariantCulture, config.RecordFormat, value.ToString(CultureInfo.InvariantCulture));
        }

        private void OnStepChanged(RunStep step)
        {
            if (step == RunStep.Home)
            {
                _shown = true;
                _tapsLeft = _config.TapsPerShow;
                Refresh();
            }
            else if (_shown)
            {
                _shown = false;
                _display.Hide();
            }
        }

        private void OnScoreChanged()
        {
            if (_shown) Refresh();
        }

        private void Refresh()
        {
            _display.Show(new HomeScreenContent(_config.Title, RecordText(_config, _score.Best), _config.PlayLabel));
        }

        private void OnPlayPressed()
        {
            if (!_shown || _tapsLeft <= 0) return;
            _tapsLeft--;
            _flow.Play();
        }
    }
}
