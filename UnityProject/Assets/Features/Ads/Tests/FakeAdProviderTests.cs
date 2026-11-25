using System.Collections.Generic;
using NUnit.Framework;

namespace Game.Ads.Tests
{
    /// <summary>The dev fake provider: the outcome is picked beforehand to try the revive flow without an ad network.</summary>
    public class FakeAdProviderTests
    {
        private const float ScreenSeconds = 2f;

        private FakeAdProvider _fake;
        private RecordingScreen _screen;
        private SilentLog _log;
        private AdRequests _ads;
        private List<AdReply> _replies;

        [SetUp]
        public void SetUp()
        {
            _fake = new FakeAdProvider(FakeAdOutcome.Watched, ScreenSeconds);
            _screen = new RecordingScreen();
            _fake.AttachScreen(_screen);
            _log = new SilentLog();
            _ads = new AdRequests(_fake, _log, 8f, 5f);
            _replies = new List<AdReply>();
        }

        private void Run(float seconds, float step = 0.1f)
        {
            for (var t = 0f; t < seconds - 0.0001f; t += step)
            {
                _fake.Tick(step);
                _ads.Tick(step, true);
            }
        }

        [Test]
        public void WatchedShowsTheWaitScreenForTwoSecondsThenGivesWatched()
        {
            _fake.Outcome = FakeAdOutcome.Watched;
            _ads.Show(1, _replies.Add);
            Assert.IsTrue(_screen.Visible, "the wait screen stands in for the ad");

            Run(1.9f);
            Assert.IsEmpty(_replies);
            Assert.IsTrue(_screen.Visible);

            Run(0.2f);
            Assert.IsFalse(_screen.Visible);
            Assert.AreEqual(1, _replies.Count);
            Assert.AreEqual(AdResult.Watched, _replies[0].Result);
        }

        [Test]
        public void NotWatchedGivesNotWatchedOnTheNextFrameWithoutAScreen()
        {
            _fake.Outcome = FakeAdOutcome.NotWatched;
            _ads.Show(1, _replies.Add);
            Assert.AreEqual(0, _screen.ShowCount);

            Run(0.1f);
            Assert.AreEqual(AdResult.NotWatched, _replies[0].Result);
            CollectionAssert.AreEqual(new[] { AdFailReason.NoFill }, _log.Reasons);
        }

        [Test]
        public void TimedOutPlaysTwoSecondsThenWaitsFiveForAConfirmationThatNeverComes()
        {
            _fake.Outcome = FakeAdOutcome.TimedOut;
            _ads.Show(1, _replies.Add);
            Assert.IsTrue(_screen.Visible);

            Run(2.1f);
            Assert.IsFalse(_screen.Visible, "the fake ad closed");
            Run(4.7f);
            Assert.IsEmpty(_replies, "still inside the confirm limit");

            Run(0.4f);
            Assert.AreEqual(AdResult.NotWatched, _replies[0].Result);
            CollectionAssert.AreEqual(new[] { AdFailReason.TimedOut }, _log.Reasons);
        }

        [Test]
        public void OutcomeCanBeChangedBetweenAds()
        {
            _fake.Outcome = FakeAdOutcome.NotWatched;
            _ads.Show(1, _replies.Add);
            Run(0.1f);

            _fake.Outcome = FakeAdOutcome.Watched;
            _ads.Show(2, _replies.Add);
            Run(2.2f);

            Assert.AreEqual(AdResult.NotWatched, _replies[0].Result);
            Assert.AreEqual(AdResult.Watched, _replies[1].Result);
        }

        [Test]
        public void SessionEndingHidesTheWaitScreen()
        {
            _ads.Show(1, _replies.Add);
            Assert.IsTrue(_screen.Visible);

            _ads.Shutdown();
            Run(3f);

            Assert.IsFalse(_screen.Visible);
            Assert.IsEmpty(_replies);
        }

        [Test]
        public void WorksWithoutAScreen()
        {
            _fake.AttachScreen(null);
            _ads.Show(1, _replies.Add);
            Run(2.1f);

            Assert.AreEqual(AdResult.Watched, _replies[0].Result);
        }

        private sealed class RecordingScreen : IFakeAdScreen
        {
            public bool Visible { get; private set; }
            public int ShowCount { get; private set; }

            public void Show(FakeAdOutcome outcome)
            {
                Visible = true;
                ShowCount++;
            }

            public void Hide() => Visible = false;
        }

        private sealed class SilentLog : IAdsLog
        {
            public readonly List<AdFailReason> Reasons = new List<AdFailReason>();

            public void NotWatched(int offerId, AdFailReason reason) => Reasons.Add(reason);

            public void Dropped(int offerId, AdResult result)
            {
            }
        }
    }
}
