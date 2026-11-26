using Game.Core;
using NUnit.Framework;
using static Game.PopupRating.Tests.RatingFakes;

namespace Game.PopupRating.Tests
{
    /// <summary>Node feature-popup-rating/rating-timing/timing-spec.</summary>
    public sealed class RatingTimingTests
    {
        private Rig _rig;

        [SetUp] public void SetUp() => _rig = new Rig();
        [TearDown] public void TearDown() => _rig.Dispose();

        [Test]
        public void CountStartsWhenTheRunSettlesTheLoss()
        {
            _rig.Participant.Run();
            Assert.That(_rig.Clock.Running, Is.False, "nothing counts before the loss");
            _rig.Flow.Stop();
            Assert.That(_rig.Clock.Running, Is.True);
        }

        [Test]
        public void InviteDelay_AsksForTheInvite1Point5SecondsAfterTheBattleStops()
        {
            Assert.That(_rig.Config.InviteDelay, Is.EqualTo(1.5f), "invite_delay at IDEA");
            _rig.NewBest();
            _rig.Clock.Tick(1.4f);
            Assert.That(_rig.Popups.Shown.Count, Is.EqualTo(0), "not yet at 1.4 s");
            _rig.Clock.Tick(0.1f);
            Assert.That(_rig.Popups.Shown, Is.EqualTo(new[] { RatingPopupIds.Main }));
            Assert.That(_rig.Clock.Running, Is.False);
        }

        [Test]
        public void NoInviteWhenTheRunDidNotSetANewBest()
        {
            _rig.Participant.Run();
            _rig.Score.IsNewBest = false;
            _rig.Flow.Stop();
            _rig.Clock.Tick(2f);
            Assert.That(_rig.Popups.Shown.Count, Is.EqualTo(0));
            Assert.That(_rig.Memory.InvitesShown, Is.EqualTo(0));
        }

        [Test]
        public void ScoreZero_NeverSetsANewBestSoNoInvite()
        {
            _rig.Participant.Run();
            _rig.Score.Score = 0;
            _rig.Score.IsNewBest = false;   // score-rules#record-update: 0 never beats the best
            _rig.Flow.Stop();
            _rig.Clock.Tick(2f);
            Assert.That(_rig.Popups.Shown.Count, Is.EqualTo(0));
        }

        [Test]
        public void FirstRunAfterInstallWithScoreAboveZeroIsInvited()
        {
            _rig.Score.Score = 3;
            _rig.NewBest();      // old best 0, flag on
            _rig.Clock.Tick(1.5f);
            Assert.That(_rig.Popups.Shown.Count, Is.EqualTo(1));
        }

        [Test]
        public void SavingTheBestFailedButTheFlagIsStillOnInMemory_StillInvites()
        {
            _rig.NewBest();      // score-rules#record-keep-on-fail: the flag stays on after a failed save
            _rig.Clock.Tick(1.5f);
            Assert.That(_rig.Popups.Shown.Count, Is.EqualTo(1));
        }

        [Test]
        public void InvitedFlagTurnsOnWhenShownAndBlocksASecondInviteThisLaunch()
        {
            Assert.That(_rig.Config.MaxInvitesPerLaunch, Is.EqualTo(1), "max_invites_per_launch at IDEA");
            _rig.NewBest();
            _rig.Clock.Tick(1.5f);
            Assert.That(_rig.Memory.InvitesShown, Is.EqualTo(1));
            _rig.Popups.View.PressLater();

            // Next session of the same launch (Play again): same memory.
            var next = _rig.NewSessionSameLaunch();
            next.Run();
            _rig.Flow.Stop();
            _rig.Clock.Tick(2f);
            Assert.That(_rig.Popups.Shown.Count, Is.EqualTo(1), "max_invites_per_launch = 1");
        }

        [Test]
        public void MaxInvitesPerLaunch_ANewLaunchStartsWithNoInviteShown()
        {
            var installer = UnityEngine.ScriptableObject.CreateInstance<PopupRatingInstaller>();
            try
            {
                Assert.That(installer.Launch.InvitesShown, Is.EqualTo(0), "nothing saved on disk: a launch starts at 0");
            }
            finally { UnityEngine.Object.DestroyImmediate(installer); }
        }

        [Test]
        public void TouchOnTheLosePopupBeforeTheInviteCancelsItAndTheFlagStaysOff()
        {
            _rig.NewBest();
            _rig.Clock.Tick(1.2f);           // within the 0.5 s after the lose popup shows
            _rig.Flow.Clear();               // the player chose Play again
            Assert.That(_rig.Clock.Running, Is.False);
            _rig.Clock.Tick(1f);
            Assert.That(_rig.Popups.Shown.Count, Is.EqualTo(0));
            Assert.That(_rig.Memory.InvitesShown, Is.EqualTo(0));

            _rig.Flow.Stop();                // a later run beats the best again: still invited
            _rig.Clock.Tick(1.5f);
            Assert.That(_rig.Popups.Shown.Count, Is.EqualTo(1));
        }

        [Test]
        public void PopupServiceRefusesThenNoInviteTheFlagStaysOffAndAWarningIsLogged()
        {
            _rig.Popups.Refuse = true;
            _rig.NewBest();
            _rig.Clock.Tick(1.5f);
            Assert.That(_rig.Participant.IsOpen, Is.False);
            Assert.That(_rig.Memory.InvitesShown, Is.EqualTo(0));
            Assert.That(_rig.Log.Warnings.Count, Is.EqualTo(1));
        }

        [Test]
        public void TimeOutsideTheAppDoesNotCount_TheRestIsCountedOnReturn()
        {
            _rig.NewBest();
            _rig.Clock.Tick(1f);
            _rig.Clock.SetActive(false);
            _rig.Clock.Tick(30f);
            _rig.Clock.SetActive(true);
            _rig.Clock.Tick(30f);            // first frame back carries the time spent outside
            Assert.That(_rig.Popups.Shown.Count, Is.EqualTo(0));
            _rig.Clock.Tick(0.4f);
            Assert.That(_rig.Popups.Shown.Count, Is.EqualTo(0));
            _rig.Clock.Tick(0.1f);
            Assert.That(_rig.Popups.Shown.Count, Is.EqualTo(1));
        }

        [Test]
        public void WhileTheInviteIsOpenOnlyItTakesTheBackKey_TheLosePopupBelowGetsNothing()
        {
            _rig.Popups.ShowLoseBelow();
            _rig.NewBest();
            _rig.Clock.Tick(1.5f);
            Assert.That(_rig.Popups.OpenCount, Is.EqualTo(2), "two popups stacked, under the service's max of 3");
            Assert.That(_rig.Popups.TopIsInvite, Is.True, "the invite is on top of the lose popup");
            _rig.Popups.View.PressBack();
            Assert.That(_rig.Participant.IsOpen, Is.False);
            Assert.That(_rig.Popups.OpenCount, Is.EqualTo(1), "the lose popup stays");
        }

        [Test]
        public void SceneEndsWhileTheInviteIsOpen_ClosedSilentlyAndNotInvitedAgainThisLaunch()
        {
            _rig.NewBest();
            _rig.Clock.Tick(1.5f);
            _rig.Participant.DisposeAsync().GetAwaiter().GetResult();
            Assert.That(_rig.Popups.OpenCount, Is.EqualTo(0));
            Assert.That(_rig.Store.AppTries + _rig.Store.BrowserTries, Is.EqualTo(0), "no touch reported");

            var next = _rig.NewSessionSameLaunch();
            next.Run();
            _rig.Flow.Stop();
            _rig.Clock.Tick(2f);
            Assert.That(_rig.Popups.Shown.Count, Is.EqualTo(1));
        }

        [Test]
        public void CountingIsOneRunningJobAndSessionEndStopsIt()
        {
            var usage = (ISessionParticipantUsage)_rig.Participant;
            _rig.NewBest();
            Assert.That(usage.RunningJobs, Is.EqualTo(1));
            _rig.Participant.DisposeAsync().GetAwaiter().GetResult();
            Assert.That(usage.RunningJobs, Is.EqualTo(0));
            Assert.That(_rig.Clock.Running, Is.False);
        }
    }
}
