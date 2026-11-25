using System;

namespace Game.Ads
{
    /// <summary>
    /// Runs rewarded-ad requests, one at a time: waiting to open, playing, waiting for the reward confirmation, then
    /// the result, given back on the first frame the game is on screen. Waiting to open and waiting for the
    /// confirmation each have a limit; time spent playing does not count. Pure C#: the frame driver calls
    /// <see cref="Tick"/> with real (unscaled) time and whether the game is on screen.
    /// </summary>
    internal sealed class AdRequests : IRewardedAds
    {
        private enum Step
        {
            WaitingOpen,
            Playing,
            WaitingConfirm,
            HasResult,
        }

        private readonly IAdProvider _provider;
        private readonly IAdsLog _log;
        private readonly float _openLimit;
        private readonly float _confirmLimit;

        private Request _current;

        public AdRequests(IAdProvider provider, IAdsLog log, float openLimitSeconds, float confirmLimitSeconds)
        {
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
            _log = log ?? throw new ArgumentNullException(nameof(log));
            if (openLimitSeconds <= 0f) throw new ArgumentOutOfRangeException(nameof(openLimitSeconds));
            if (confirmLimitSeconds <= 0f) throw new ArgumentOutOfRangeException(nameof(confirmLimitSeconds));
            _openLimit = openLimitSeconds;
            _confirmLimit = confirmLimitSeconds;
        }

        /// <summary>True while a request has not yet given its result back.</summary>
        public bool IsBusy => _current != null;

        public void Show(int offerId, Action<AdReply> onReply)
        {
            if (onReply == null) throw new ArgumentNullException(nameof(onReply));

            if (_current != null)
            {
                _log.NotWatched(offerId, AdFailReason.Busy);
                onReply(new AdReply(offerId, AdResult.NotWatched));
                return;
            }

            var request = new Request(this, offerId, onReply);
            _current = request;
            _provider.Begin(request);
        }

        public void Cancel(int offerId)
        {
            // The ad keeps running (it may already be on screen); its result is dropped when it comes.
            if (_current != null && _current.OfferId == offerId) _current.Cancelled = true;
        }

        /// <summary>One frame. <paramref name="deltaSeconds"/> is real time; <paramref name="gameOnScreen"/> is false while the game is hidden.</summary>
        public void Tick(float deltaSeconds, bool gameOnScreen)
        {
            var request = _current;
            if (request == null) return;

            switch (request.Step)
            {
                case Step.WaitingOpen:
                    request.Elapsed += deltaSeconds;
                    if (request.Elapsed >= _openLimit)
                    {
                        _provider.Abort();
                        request.Finish(AdResult.NotWatched, AdFailReason.TimedOut);
                    }
                    break;

                case Step.WaitingConfirm:
                    request.Elapsed += deltaSeconds;
                    if (request.Elapsed >= _confirmLimit)
                        request.Finish(AdResult.NotWatched, request.Problem ?? AdFailReason.TimedOut);
                    break;
            }

            if (request.Step == Step.HasResult && gameOnScreen) Deliver(request);
        }

        /// <summary>The session ends (scene left, game closing): the running request gives no result to anyone.</summary>
        public void Shutdown()
        {
            if (_current == null) return;
            _current = null;
            _provider.Abort();
        }

        private void Deliver(Request request)
        {
            _current = null;

            if (request.Cancelled)
            {
                _log.Dropped(request.OfferId, request.Result);
                return;
            }

            if (request.Result == AdResult.NotWatched) _log.NotWatched(request.OfferId, request.Reason);
            request.OnReply(new AdReply(request.OfferId, request.Result));
        }

        /// <summary>One request; also the sink of its provider events, so events of an older request are ignored.</summary>
        private sealed class Request : IAdProviderEvents
        {
            private readonly AdRequests _owner;

            public Request(AdRequests owner, int offerId, Action<AdReply> onReply)
            {
                _owner = owner;
                OfferId = offerId;
                OnReply = onReply;
            }

            public int OfferId { get; }
            public Action<AdReply> OnReply { get; }
            public Step Step { get; private set; } = Step.WaitingOpen;
            public float Elapsed { get; set; }
            public bool Cancelled { get; set; }
            public bool RewardConfirmed { get; private set; }

            /// <summary>A problem the provider reported after the ad opened; used if no confirmation comes.</summary>
            public AdFailReason? Problem { get; private set; }

            public AdResult Result { get; private set; }
            public AdFailReason Reason { get; private set; }

            private bool IsCurrent => _owner._current == this && Step != Step.HasResult;

            public void Started()
            {
                if (!IsCurrent || Step != Step.WaitingOpen) return;
                Step = Step.Playing;
                Elapsed = 0f;
            }

            public void Failed(AdFailReason reason)
            {
                if (!IsCurrent) return;
                if (Step == Step.WaitingOpen)
                {
                    Finish(AdResult.NotWatched, reason);
                    return;
                }

                // After the ad opened, only the confirmation decides; the problem is kept for the log.
                if (Problem == null) Problem = reason;
            }

            public void Rewarded()
            {
                if (!IsCurrent) return;
                RewardConfirmed = true;
                if (Step == Step.WaitingConfirm) Finish(AdResult.Watched, default);
            }

            public void Closed()
            {
                if (!IsCurrent || Step == Step.WaitingConfirm) return;
                if (RewardConfirmed)
                {
                    Finish(AdResult.Watched, default);
                    return;
                }

                Step = Step.WaitingConfirm;
                Elapsed = 0f;
            }

            public void Finish(AdResult result, AdFailReason reason)
            {
                Result = result;
                Reason = reason;
                Step = Step.HasResult;
            }
        }
    }
}
