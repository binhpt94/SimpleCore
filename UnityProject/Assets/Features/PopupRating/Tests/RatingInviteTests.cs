using NUnit.Framework;
using static Game.PopupRating.Tests.RatingFakes;

namespace Game.PopupRating.Tests
{
    /// <summary>Node feature-popup-rating/rating-invite/invite-spec, and the cheat.</summary>
    public sealed class RatingInviteTests
    {
        private Rig _rig;

        [SetUp]
        public void SetUp()
        {
            _rig = new Rig();
            _rig.NewBest();
            _rig.Clock.Tick(1.5f);
        }

        [TearDown] public void TearDown() => _rig.Dispose();

        [Test]
        public void TheInviteIsPopupRatingMain()
        {
            Assert.That(RatingPopupIds.Main.Value, Is.EqualTo("PopupRating/Main"));
            Assert.That(_rig.Popups.Shown, Is.EqualTo(new[] { RatingPopupIds.Main }));
        }

        [Test]
        public void InviteText_TheLineReadsDanhGiaGameTrenGooglePlay()
        {
            Assert.That(_rig.Config.InviteText, Is.EqualTo("Đánh giá game trên Google Play"), "invite_text");
            Assert.That(_rig.Popups.View.Text, Is.EqualTo("Đánh giá game trên Google Play"));
        }

        [Test]
        public void RateLabel_TheMainButtonReadsDanhGia()
        {
            Assert.That(_rig.Config.RateLabel, Is.EqualTo("Đánh giá"), "rate_label");
            Assert.That(_rig.Popups.View.Rate, Is.EqualTo("Đánh giá"));
        }

        [Test]
        public void LaterLabel_TheSecondButtonReadsDeSau()
        {
            Assert.That(_rig.Config.LaterLabel, Is.EqualTo("Để sau"), "later_label");
            Assert.That(_rig.Popups.View.Later, Is.EqualTo("Để sau"));
        }

        [Test]
        public void TwoButtons_RateIsMainLaterIsSecond()
        {
            Assert.That(_rig.Popups.View.Rate, Is.EqualTo("Đánh giá"));
            Assert.That(_rig.Popups.View.Later, Is.EqualTo("Để sau"));
        }

        [Test]
        public void RateClosesThePopupFirstThenOpensThePlayAppByThisBuildsPackage()
        {
            _rig.Store.OnAppTry = () => Assert.That(_rig.Popups.OpenCount, Is.EqualTo(0), "closed before the store opens");
            _rig.Popups.View.PressRate();
            Assert.That(_rig.Store.AppPackages, Is.EqualTo(new[] { "com.test.game" }));
            Assert.That(_rig.Store.BrowserTries, Is.EqualTo(0));
        }

        [Test]
        public void StoreOpenOrder_NoPlayAppOrInTheEditorOpensTheWebPageInABrowser()
        {
            Assert.That(_rig.Config.StoreOpen, Is.EqualTo(StoreOpenOrder.AppThenBrowser), "store_open_order");
            _rig.Store.AppWorks = false;
            _rig.Popups.View.PressRate();
            Assert.That(_rig.Store.AppTries, Is.EqualTo(1));
            Assert.That(_rig.Store.BrowserPackages, Is.EqualTo(new[] { "com.test.game" }));
            Assert.That(_rig.Log.Errors, Is.Empty);
        }

        [Test]
        public void NeitherAppNorBrowserOpens_PopupAlreadyClosedOnlyALogError()
        {
            _rig.Store.AppWorks = false;
            _rig.Store.BrowserWorks = false;
            _rig.Popups.View.PressRate();
            Assert.That(_rig.Popups.OpenCount, Is.EqualTo(0));
            Assert.That(_rig.Log.Errors.Count, Is.EqualTo(1));
        }

        [Test]
        public void GameNotYetOnGooglePlay_StillOpensByPackageName()
        {
            _rig.Popups.View.PressRate();   // Play shows "not found"; the game just opens the page
            Assert.That(_rig.Store.AppPackages, Is.EqualTo(new[] { "com.test.game" }));
            Assert.That(_rig.Log.Errors, Is.Empty);
        }

        [Test]
        public void LaterOnlyClosesThePopup()
        {
            _rig.Popups.View.PressLater();
            Assert.That(_rig.Popups.OpenCount, Is.EqualTo(0));
            Assert.That(_rig.Store.AppTries + _rig.Store.BrowserTries, Is.EqualTo(0));
        }

        [Test]
        public void OnlyTheFirstTouchCounts_ButtonsLockUntilClosed()
        {
            _rig.Popups.View.PressLater();
            Assert.That(_rig.Popups.View.Locked, Is.True);
            _rig.Popups.View.RaiseRateIgnoringLock();
            Assert.That(_rig.Store.AppTries, Is.EqualTo(0));
        }

        [Test]
        public void TwoButtonsTouchedAlmostTogether_OnlyTheFirstCounts()
        {
            var view = _rig.Popups.View;
            view.RaiseRateIgnoringLock();
            view.RaiseLaterIgnoringLock();
            Assert.That(_rig.Store.AppTries, Is.EqualTo(1));
            Assert.That(_rig.Popups.Closes, Is.EqualTo(1));
        }

        [Test]
        public void BackKeyWhileOnTopActsAsLater()
        {
            _rig.Popups.View.PressBack();
            Assert.That(_rig.Popups.OpenCount, Is.EqualTo(0));
            Assert.That(_rig.Store.AppTries, Is.EqualTo(0));
        }

        [Test]
        public void BackKeyWhenNotOnTopDoesNothing()
        {
            _rig.Popups.OnTop = false;
            _rig.Popups.View.PressBack();
            Assert.That(_rig.Participant.IsOpen, Is.True);
        }

        [Test]
        public void BackdropTouchDoesNotCloseTheInvite()
        {
            // The view reports only the two buttons and the back key; the backdrop is the popup service's, ignored.
            _rig.Clock.Tick(10f);
            Assert.That(_rig.Participant.IsOpen, Is.True);
            Assert.That(_rig.Popups.OpenCount, Is.EqualTo(1));
        }

        [Test]
        public void LeavingTheAppWithThePopupOpen_ItIsStillOpenOnReturnAndNoChoiceIsTaken()
        {
            _rig.Clock.SetActive(false);
            _rig.Clock.SetActive(true);
            Assert.That(_rig.Participant.IsOpen, Is.True);
            Assert.That(_rig.Popups.View.Locked, Is.False);
        }

        [Test]
        public void RateThenBackToTheGame_TheInviteIsGone()
        {
            _rig.Popups.View.PressRate();
            _rig.Clock.SetActive(false);
            _rig.Clock.SetActive(true);
            Assert.That(_rig.Participant.IsOpen, Is.False);
            Assert.That(_rig.Popups.OpenCount, Is.EqualTo(0));
        }

        [Test]
        public void SceneEndsWithThePopupOpen_DisposedSilently()
        {
            _rig.Participant.DisposeAsync().GetAwaiter().GetResult();
            Assert.That(_rig.Popups.OpenCount, Is.EqualTo(0));
            Assert.That(_rig.Store.AppTries + _rig.Store.BrowserTries, Is.EqualTo(0));
        }

        [Test]
        public void ParamsRowsAreTheSameAtEveryStage()
        {
            Assert.That(_rig.Config.InviteText, Is.EqualTo("Đánh giá game trên Google Play"));
            Assert.That(_rig.Config.RateLabel, Is.EqualTo("Đánh giá"));
            Assert.That(_rig.Config.LaterLabel, Is.EqualTo("Để sau"));
            Assert.That(_rig.Config.StoreOpen, Is.EqualTo(StoreOpenOrder.AppThenBrowser));
            Assert.That(_rig.Config.InviteDelay, Is.EqualTo(1.5f));
            Assert.That(_rig.Config.MaxInvitesPerLaunch, Is.EqualTo(1));
        }

        [Test]
        public void CheatInviteOpen_OpensTheFullInviteWithoutANewBestAndNoTouch()
        {
            var rig = new Rig();
            try
            {
                rig.Score.IsNewBest = false;
                rig.Memory.InvitesShown = 1;     // launch limit skipped too
                rig.Participant.Run();
                new RatingCheatParticipant(rig.Participant).Run();
                Assert.That(rig.Participant.IsOpen, Is.True);
                Assert.That(rig.Popups.View.Text, Is.EqualTo("Đánh giá game trên Google Play"));
                Assert.That(rig.Popups.View.Rate, Is.EqualTo("Đánh giá"));
                Assert.That(rig.Popups.View.Later, Is.EqualTo("Để sau"));
                Assert.That(rig.Popups.View.Locked, Is.False);
            }
            finally { rig.Dispose(); }
        }
    }
}
