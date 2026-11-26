using Game.Run;
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
        public void InviteDelay_ALongFrameRightAfterTheStopNeverOpensTheInviteBeforeTheLosePopup()
        {
            _rig.Config.MaxFrameSeconds = 0.125f;   // the shipped cap
            _rig.Participant.Run();
            _rig.Score.IsNewBest = true;
            _rig.Flow.Step = RunStep.Ending;
            _rig.Flow.Stop();
            _rig.Clock.Tick(0.6f);            // one long frame, like the castle falling
            Assert.That(_rig.Popups.Shown.Count, Is.EqualTo(0), "the long frame counts only the per-frame cap");
            for (var i = 0; i < 20 && _rig.Clock.Running; i++) _rig.Clock.Tick(0.6f);
            Assert.That(_rig.Popups.Shown.Count, Is.EqualTo(0), "invite_delay reached, but the run is not at Lost yet");

            _rig.Popups.ShowLoseBelow();      // the run reaches Lost: the lose popup opens first
            _rig.Flow.GoTo(RunStep.Lost);
            Assert.That(_rig.Popups.Shown, Is.EqualTo(new[] { RatingPopupIds.Main }));
            Assert.That(_rig.Popups.TopIsInvite, Is.True, "the invite sits on top of the lose popup");
        }

        [Test]
        public void InviteDelay_EachFrameCountsAtMostTheBattleClockCap()
        {
            var shipped = UnityEngine.ScriptableObject.CreateInstance<RatingConfig>();
            Assert.That(shipped.MaxFrameSeconds, Is.EqualTo(0.125f), "SimClock cap: 5 steps of 1/40 s");
            UnityEngine.Object.DestroyImmediate(shipped);
            _rig.Config.MaxFrameSeconds = 0.125f;
            _rig.NewBest();
            for (var i = 0; i < 11; i++) _rig.Clock.Tick(1f);   // 11 x 0.125 = 1.375 s < invite_delay
            Assert.That(_rig.Popups.Shown.Count, Is.EqualTo(0));
            _rig.Clock.Tick(1f);                                // 1.5 s
            Assert.That(_rig.Popups.Shown.Count, Is.EqualTo(1));
        }

        [Test]
        public void LeavingTheBattleWhileTheInviteIsOpenClosesItSilently()
        {
            _rig.NewBest();
            _rig.Clock.Tick(1.5f);
            Assert.That(_rig.Participant.IsOpen, Is.True);
            _rig.Flow.Clear();                // Play again touched while the invite was open
            Assert.That(_rig.Participant.IsOpen, Is.False);
            Assert.That(_rig.Popups.OpenCount, Is.EqualTo(0), "no invite over the next screen");
            Assert.That(_rig.Store.AppTries + _rig.Store.BrowserTries, Is.EqualTo(0), "not a touch on a button");
            Assert.That(_rig.Participant.InvitesShown, Is.EqualTo(1), "it was shown, so it counts");
        }

        [Test]
        public void LeavingTheBattleWhileWaitingForLostCancelsTheInvite()
        {
            _rig.Participant.Run();
            _rig.Flow.Step = RunStep.Ending;
            _rig.Flow.Stop();
            for (var i = 0; i < 12; i++) _rig.Clock.Tick(0.125f);
            _rig.Flow.Clear();
            _rig.Flow.GoTo(RunStep.Lost);
            Assert.That(_rig.Popups.Shown.Count, Is.EqualTo(0));
        }

        [Test]
        public void NoInviteWhenTheRunDidNotSetANewBest()
        {
            _rig.Participant.Run();
            _rig.Score.IsNewBest = false;
            _rig.Flow.Stop();
            _rig.Clock.Tick(2f);
            Assert.That(_rig.Popups.Shown.Count, Is.EqualTo(0));
            Assert.That(_rig.Participant.InvitesShown, Is.EqualTo(0));
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
        public void InvitedFlagTurnsOnWhenShownAndBlocksASecondInviteAfterPlayAgain()
        {
            _rig.NewBest();
            Assert.That(_rig.Participant.InvitesShown, Is.EqualTo(0), "not yet shown while counting");
            _rig.Clock.Tick(1.5f);
            Assert.That(_rig.Participant.InvitesShown, Is.EqualTo(1), "on once the invite is shown");
            _rig.Popups.View.PressLater();

            // Play again in the same Game scene, beat the best again: same participant, flag still on.
            _rig.Flow.Clear();
            _rig.Score.IsNewBest = true;
            _rig.Flow.Stop();
            _rig.Clock.Tick(2f);
            Assert.That(_rig.Popups.Shown.Count, Is.EqualTo(1));
            Assert.That(_rig.Participant.InvitesShown, Is.EqualTo(1));
        }

        [Test]
        public void MaxInvitesPerLaunch_IsOneSoTheSecondNewBestOfTheLaunchIsNotInvited()
        {
            Assert.That(_rig.Config.MaxInvitesPerLaunch, Is.EqualTo(1), "max_invites_per_launch at IDEA");
            Assert.That(_rig.Participant.InvitesShown, Is.EqualTo(0), "nothing saved on disk: a launch starts at 0");
            _rig.Participant.Run();
            for (var run = 1; run <= 3; run++)
            {
                _rig.Score.IsNewBest = true;
                _rig.Flow.Stop();
                _rig.Clock.Tick(1.5f);
                if (_rig.Participant.IsOpen) _rig.Popups.View.PressLater();
                _rig.Flow.Clear();
            }
            Assert.That(_rig.Popups.Shown.Count, Is.EqualTo(_rig.Config.MaxInvitesPerLaunch),
                "max_invites_per_launch caps the invites of one launch");
        }

        [Test]
        public void MaxInvitesPerLaunch_RaisedToTwoAllowsASecondInvite()
        {
            _rig.Config.MaxInvitesPerLaunch = 2;   // the cap is read from max_invites_per_launch, not hard-coded
            _rig.NewBest();
            _rig.Clock.Tick(1.5f);
            _rig.Popups.View.PressLater();
            _rig.Flow.Clear();
            _rig.Flow.Stop();
            _rig.Clock.Tick(1.5f);
            Assert.That(_rig.Popups.Shown.Count, Is.EqualTo(2));
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
            Assert.That(_rig.Participant.InvitesShown, Is.EqualTo(0));

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
            Assert.That(_rig.Participant.InvitesShown, Is.EqualTo(0));
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

            Assert.That(_rig.Participant.InvitesShown, Is.EqualTo(1), "the invite counted: it was shown");
            _rig.Flow.Stop();
            _rig.Clock.Tick(2f);
            Assert.That(_rig.Popups.Shown.Count, Is.EqualTo(1), "an ended session invites no more");
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
