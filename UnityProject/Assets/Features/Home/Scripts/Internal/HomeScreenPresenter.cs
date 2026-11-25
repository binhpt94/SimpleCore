using System;
using System.Globalization;
using Game.Run;

namespace Game.Home
{
    /// <summary>
    /// Rules of the start screen, the <see cref="IHomeScreen"/> the screen flow drives: shown only when the flow
    /// calls <see cref="Show"/>, reads the best score each time it shows (and when it changes while shown), and
    /// reports Play once per showing. Which screen comes next is the flow's job.
    /// The view is attached when the scene session runs; a Show that comes earlier is kept and shown on attach.
    /// </summary>
    internal sealed class HomeScreenPresenter : IHomeScreen
    {
        private readonly IRunScore _score;
        private readonly HomeConfig _config;

        private IHomeScreenDisplay _display;
        private bool _shown;
        private int _tapsLeft;

        public HomeScreenPresenter(IRunScore score, HomeConfig config)
        {
            _score = score ?? throw new ArgumentNullException(nameof(score));
            _config = config ? config : throw new ArgumentNullException(nameof(config));
        }

        public event Action PlayPressed;

        public bool IsShown => _shown;

        public void Show()
        {
            if (!_shown) _score.ScoreChanged += OnScoreChanged;
            _shown = true;
            _tapsLeft = _config.TapsPerShow;
            Refresh();
        }

        public void Hide()
        {
            if (!_shown) return;
            _shown = false;
            _score.ScoreChanged -= OnScoreChanged;
            _display?.Hide();
        }

        /// <summary>Gives the presenter its view (the session runs). Shows it at once if the flow already asked.</summary>
        public void Attach(IHomeScreenDisplay display)
        {
            if (display == null) throw new ArgumentNullException(nameof(display));
            ReleaseDisplay();
            _display = display;
            _display.PlayPressed += OnPlayPressed;
            if (_shown) Refresh();
            else _display.Hide();
        }

        /// <summary>Lets go of the view (the session ends). The screen counts as hidden.</summary>
        public void Detach()
        {
            Hide();
            ReleaseDisplay();
        }

        /// <summary>Unhooks the current view without touching whether the flow wants the screen shown.</summary>
        private void ReleaseDisplay()
        {
            if (_display == null) return;
            _display.PlayPressed -= OnPlayPressed;
            _display = null;
        }

        /// <summary>The record line for <paramref name="best"/>: every digit, no shortening; none saved yet shows the default.</summary>
        public static string RecordText(HomeConfig config, int best)
        {
            var value = best < 0 ? config.RecordWhenNone : best;
            return string.Format(CultureInfo.InvariantCulture, config.RecordFormat, value.ToString(CultureInfo.InvariantCulture));
        }

        private void OnScoreChanged()
        {
            if (_shown) Refresh();
        }

        private void Refresh()
        {
            _display?.Show(new HomeScreenContent(_config.Title, RecordText(_config, _score.Best), _config.PlayLabel));
        }

        private void OnPlayPressed()
        {
            if (!_shown || _tapsLeft <= 0) return;
            _tapsLeft--;
            PlayPressed?.Invoke();
        }
    }
}
