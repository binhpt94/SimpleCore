using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Game.Ads.Tests
{
    /// <summary>One rewarded-ad request and its result, with a scripted provider standing in for the ad network.</summary>
    public class AdRequestsTests
    {
        private const float OpenLimit = 8f;
        private const float ConfirmLimit = 5f;

        private ScriptedProvider _provider;
        private RecordingLog _log;
        private AdRequests _ads;
        private List<AdReply> _replies;

        [SetUp]
        public void SetUp()
        {
            _provider = new ScriptedProvider();
            _log = new RecordingLog();
            _ads = new AdRequests(_provider, _log, OpenLimit, ConfirmLimit);
            _replies = new List<AdReply>();
        }

        private void Show(int offerId) => _ads.Show(offerId, _replies.Add);

        private void Run(float seconds, bool onScreen = true, float step = 0.1f)
        {
            for (var t = 0f; t < seconds - 0.0001f; t += step) _ads.Tick(step, onScreen);
        }

        // ---- open-limit ----

        [Test]
        public void AdThatNeverOpensIsNotWatchedAfterEightSeconds()
        {
            Show(1);
            Run(7.9f);
            Assert.IsEmpty(_replies, "still waiting before the open limit");

            Run(0.2f);
            Assert.AreEqual(1, _replies.Count);
            Assert.AreEqual(AdResult.NotWatched, _replies[0].Result);
            Assert.AreEqual(1, _replies[0].OfferId);
            Assert.AreEqual(1, _provider.AbortCount, "the ad that never opened is stopped");
        }

        [Test]
        public void TimePlayingDoesNotCountTowardsAnyLimit()
        {
            Show(1);
            Run(7f);
            _provider.Events.Started();
            Run(60f);
            Assert.IsEmpty(_replies, "a long ad never times out while it plays");

            _provider.Events.Rewarded();
            _provider.Events.Closed();
            Run(0.1f);
            Assert.AreEqual(AdResult.Watched, _replies[0].Result);
        }

        // ---- confirm-limit ----

        [Test]
        public void ClosedAdWithoutConfirmationIsNotWatchedAfterFiveSeconds()
        {
            Show(1);
            _provider.Events.Started();
            _provider.Events.Closed();
            Run(4.9f);
            Assert.IsEmpty(_replies, "still waiting for the confirmation");

            Run(0.2f);
            Assert.AreEqual(AdResult.NotWatched, _replies[0].Result);
            CollectionAssert.AreEqual(new[] { AdFailReason.TimedOut }, _log.Reasons);
        }

        [Test]
        public void ConfirmationWithinFiveSecondsOfClosingIsWatched()
        {
            Show(1);
            _provider.Events.Started();
            _provider.Events.Closed();
            Run(4.8f);
            _provider.Events.Rewarded();
            Run(0.1f);

            Assert.AreEqual(1, _replies.Count);
            Assert.AreEqual(AdResult.Watched, _replies[0].Result);
            Assert.IsEmpty(_log.Reasons);
        }

        [Test]
        public void ResultIsGivenOnlyOnce()
        {
            Show(1);
            _provider.Events.Started();
            _provider.Events.Rewarded();
            _provider.Events.Closed();
            Run(1f);
            _provider.Events.Rewarded();
            _provider.Events.Closed();
            Run(10f);

            Assert.AreEqual(1, _replies.Count);
        }

        // ---- return-wait ----

        [Test]
        public void ResultWaitsWithoutLimitUntilTheGameIsBackOnScreen()
        {
            Show(1);
            _provider.Events.Started();
            _provider.Events.Rewarded();
            _provider.Events.Closed();
            Run(120f, onScreen: false);
            Assert.IsEmpty(_replies, "no result while the player is outside the game");

            _ads.Tick(0.016f, true);
            Assert.AreEqual(1, _replies.Count, "given on the first frame back on screen");
            Assert.AreEqual(AdResult.Watched, _replies[0].Result);
        }

        [Test]
        public void ResultIsNeverGivenInsideTheProviderCallback()
        {
            Show(1);
            _provider.Events.Started();
            _provider.Events.Rewarded();
            _provider.Events.Closed();
            Assert.IsEmpty(_replies, "the ad has just closed; the game has not had a frame yet");

            _ads.Tick(0.016f, true);
            Assert.AreEqual(1, _replies.Count);
        }

        // ---- busy-reply ----

        [Test]
        public void RequestWhileAnotherRunsIsNotWatchedAtOnceAndTheRunningOneGoesOn()
        {
            Show(1);
            _provider.Events.Started();

            Show(2);
            Assert.AreEqual(1, _replies.Count, "answered in the same call, no frame needed");
            Assert.AreEqual(2, _replies[0].OfferId);
            Assert.AreEqual(AdResult.NotWatched, _replies[0].Result);
            Assert.AreEqual(1, _provider.BeginCount, "no second ad is asked for");

            _provider.Events.Rewarded();
            _provider.Events.Closed();
            Run(0.1f);
            Assert.AreEqual(2, _replies.Count);
            Assert.AreEqual(1, _replies[1].OfferId);
            Assert.AreEqual(AdResult.Watched, _replies[1].Result);
        }

        [Test]
        public void NewRequestIsAcceptedOnceTheLastResultWasGiven()
        {
            Show(1);
            _provider.Events.Failed(AdFailReason.NoFill);
            Run(0.1f);

            Show(2);
            Assert.AreEqual(2, _provider.BeginCount);
            Assert.AreEqual(1, _replies.Count, "the second one is running, not refused");
        }

        // ---- log-reason ----

        [Test]
        public void EveryNotWatchedWritesItsReasonToTheLog()
        {
            // No fill.
            Show(1);
            _provider.Events.Failed(AdFailReason.NoFill);
            Run(0.1f);

            // Closed early.
            Show(2);
            _provider.Events.Started();
            _provider.Events.Failed(AdFailReason.ClosedEarly);
            _provider.Events.Closed();
            Run(5.1f);

            // Busy, then the running one times out on opening.
            Show(3);
            Show(4);
            Run(8.1f);

            CollectionAssert.AreEqual(
                new[] { AdFailReason.NoFill, AdFailReason.ClosedEarly, AdFailReason.Busy, AdFailReason.TimedOut },
                _log.Reasons);
            CollectionAssert.AreEqual(new[] { 1, 2, 4, 3 }, _log.OfferIds);
            Assert.That(_replies, Has.All.Matches<AdReply>(r => r.Result == AdResult.NotWatched));
        }

        [Test]
        public void WatchedWritesNothingToTheLog()
        {
            Show(1);
            _provider.Events.Started();
            _provider.Events.Rewarded();
            _provider.Events.Closed();
            Run(0.1f);

            Assert.IsEmpty(_log.Reasons);
            Assert.IsEmpty(_log.Dropped);
        }

        // ---- edge: network lost while the ad plays ----

        [Test]
        public void NetworkLostWhileAdPlaysWaitsForCloseThenFiveSecondsThenNotWatched()
        {
            Show(1);
            _provider.Events.Started();
            _provider.Events.Failed(AdFailReason.NoNetwork);
            Run(30f);
            Assert.IsEmpty(_replies, "waits for the ad to close");

            _provider.Events.Closed();
            Run(4.9f);
            Assert.IsEmpty(_replies, "a confirmation may still come");

            Run(0.2f);
            Assert.AreEqual(AdResult.NotWatched, _replies[0].Result);
            CollectionAssert.AreEqual(new[] { AdFailReason.NoNetwork }, _log.Reasons);
        }

        [Test]
        public void NetworkLostWhileWaitingToOpenIsNotWatchedOnTheNextFrame()
        {
            Show(1);
            _provider.Events.Failed(AdFailReason.NoNetwork);
            Run(0.1f);

            Assert.AreEqual(AdResult.NotWatched, _replies[0].Result);
            CollectionAssert.AreEqual(new[] { AdFailReason.NoNetwork }, _log.Reasons);
        }

        // ---- edge: confirmation before the ad closes ----

        [Test]
        public void ConfirmationBeforeCloseIsKeptAndWatchedAfterCloseAndReturn()
        {
            Show(1);
            _provider.Events.Started();
            _provider.Events.Rewarded();
            Run(10f);
            Assert.IsEmpty(_replies, "the ad is still open");

            _provider.Events.Closed();
            Run(3f, onScreen: false);
            Assert.IsEmpty(_replies, "the game is not back yet");

            _ads.Tick(0.016f, true);
            Assert.AreEqual(AdResult.Watched, _replies[0].Result);
        }

        [Test]
        public void ProblemReportedAfterConfirmationStillGivesWatched()
        {
            Show(1);
            _provider.Events.Started();
            _provider.Events.Rewarded();
            _provider.Events.Failed(AdFailReason.NoNetwork);
            _provider.Events.Closed();
            Run(0.1f);

            Assert.AreEqual(AdResult.Watched, _replies[0].Result);
        }

        // ---- edge: player quits while the ad plays ----

        [Test]
        public void SessionEndingWhileAdPlaysGivesNoResultToAnyone()
        {
            Show(1);
            _provider.Events.Started();
            var oldEvents = _provider.Events;

            _ads.Shutdown();
            oldEvents.Rewarded();
            oldEvents.Closed();
            Run(10f);

            Assert.IsEmpty(_replies);
            Assert.AreEqual(1, _provider.AbortCount);
            Assert.IsFalse(_ads.IsBusy);
        }

        // ---- edge: result after the caller cancelled ----

        [Test]
        public void ResultAfterCancelIsDroppedAndLogged()
        {
            Show(7);
            _provider.Events.Started();
            _ads.Cancel(7);
            _provider.Events.Rewarded();
            _provider.Events.Closed();
            Run(1f);

            Assert.IsEmpty(_replies, "no reward lands on a later offer");
            CollectionAssert.AreEqual(new[] { 7 }, _log.Dropped);
            Assert.IsFalse(_ads.IsBusy);
        }

        [Test]
        public void CancelForAnotherOfferChangesNothing()
        {
            Show(7);
            _ads.Cancel(8);
            _provider.Events.Started();
            _provider.Events.Rewarded();
            _provider.Events.Closed();
            Run(0.1f);

            Assert.AreEqual(1, _replies.Count);
            Assert.AreEqual(7, _replies[0].OfferId);
        }

        [Test]
        public void EventsOfAnEarlierAdDoNotTouchTheNextRequest()
        {
            Show(1);
            var firstEvents = _provider.Events;
            Run(8.1f); // first one timed out

            Show(2);
            firstEvents.Started();
            firstEvents.Rewarded();
            firstEvents.Closed();
            Run(1f);

            Assert.AreEqual(1, _replies.Count, "only the timed-out reply of offer 1");
            Assert.AreEqual(1, _replies[0].OfferId);
            Assert.IsTrue(_ads.IsBusy, "offer 2 is still waiting for its own ad");
        }

        // ---- params ----

        [Test]
        public void ConfigDefaultsAreTheDesignNumbers()
        {
            var config = ScriptableObject.CreateInstance<AdsConfig>();
            try
            {
                Assert.AreEqual(8f, config.OpenLimitSeconds);
                Assert.AreEqual(5f, config.ConfirmLimitSeconds);
                Assert.AreEqual(2f, config.FakeScreenSeconds);
            }
            finally
            {
                Object.DestroyImmediate(config);
            }
        }

        [Test]
        public void LimitsComeFromTheGivenNumbers()
        {
            var ads = new AdRequests(_provider, _log, 2f, 1f);
            ads.Show(1, _replies.Add);
            for (var i = 0; i < 21; i++) ads.Tick(0.1f, true);

            Assert.AreEqual(1, _replies.Count, "open limit of 2 seconds");
        }

        [Test]
        public void OneRequestAtATime()
        {
            Show(1);
            Show(2);
            Show(3);

            Assert.AreEqual(1, _provider.BeginCount);
            Assert.IsTrue(_ads.IsBusy);
            CollectionAssert.AreEqual(new[] { 2, 3 }, _replies.ConvertAll(r => r.OfferId));
        }

        // ---- doubles ----

        private sealed class ScriptedProvider : IAdProvider
        {
            public IAdProviderEvents Events { get; private set; }
            public int BeginCount { get; private set; }
            public int AbortCount { get; private set; }

            public void Begin(IAdProviderEvents events)
            {
                BeginCount++;
                Events = events;
            }

            public void Abort() => AbortCount++;
        }

        private sealed class RecordingLog : IAdsLog
        {
            public readonly List<AdFailReason> Reasons = new List<AdFailReason>();
            public readonly List<int> OfferIds = new List<int>();
            public readonly List<int> Dropped = new List<int>();

            public void NotWatched(int offerId, AdFailReason reason)
            {
                Reasons.Add(reason);
                OfferIds.Add(offerId);
            }

            void IAdsLog.Dropped(int offerId, AdResult result) => Dropped.Add(offerId);
        }
    }
}
