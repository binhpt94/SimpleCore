#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;

namespace Game.Ads
{
    /// <summary>
    /// Dev-only provider: no network, the outcome is chosen beforehand. Watched and TimedOut show the wait screen
    /// for <c>screenSeconds</c> as if an ad played; NotWatched reports no fill at once. The cheat
    /// <see cref="HoldNextAd"/> makes the next ad play forever without a screen, until it is cancelled or aborted.
    /// </summary>
    internal sealed class FakeAdProvider : IAdProvider, IAdsCheat
    {
        private readonly float _screenSeconds;
        private IFakeAdScreen _screen;
        private IAdProviderEvents _events;
        private float _elapsed;
        private bool _holdNext;
        private bool _held;

        public FakeAdProvider(FakeAdOutcome outcome, float screenSeconds)
        {
            if (screenSeconds < 0f) throw new ArgumentOutOfRangeException(nameof(screenSeconds));
            Outcome = outcome;
            _screenSeconds = screenSeconds;
        }

        /// <summary>What the next ad does. Can be changed between ads (cheat, tests).</summary>
        public FakeAdOutcome Outcome { get; set; }

        /// <summary>The screen shown while the fake ad plays; none in tests.</summary>
        public void AttachScreen(IFakeAdScreen screen) => _screen = screen;

        public void HoldNextAd() => _holdNext = true;

        public void Begin(IAdProviderEvents events)
        {
            if (events == null) throw new ArgumentNullException(nameof(events));
            Stop();

            if (_holdNext)
            {
                // Playing time has no limit, so the request waits with no reply. No screen: the caller's waiting
                // state stays visible.
                _holdNext = false;
                _held = true;
                _events = events;
                events.Started();
                return;
            }

            if (Outcome == FakeAdOutcome.NotWatched)
            {
                events.Failed(AdFailReason.NoFill);
                return;
            }

            _events = events;
            _elapsed = 0f;
            _screen?.Show(Outcome);
            events.Started();
        }

        public void Abort() => Stop();

        public bool EndOnCancel()
        {
            if (!_held) return false;
            Stop();
            return true;
        }

        /// <summary>Real time passes while the fake ad is on screen.</summary>
        public void Tick(float deltaSeconds)
        {
            if (_events == null || _held) return;
            _elapsed += deltaSeconds;
            if (_elapsed < _screenSeconds) return;

            var events = _events;
            var watched = Outcome == FakeAdOutcome.Watched;
            Stop();
            if (watched) events.Rewarded();
            events.Closed();
        }

        private void Stop()
        {
            if (_events == null) return;
            _events = null;
            if (_held)
            {
                _held = false;
                return;
            }

            _screen?.Hide();
        }
    }
}
#endif
