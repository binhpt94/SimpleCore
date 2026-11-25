using NUnit.Framework;
using static Game.TeamSelect.Tests.Fakes;

namespace Game.TeamSelect.Tests
{
    /// <summary>
    /// The screen as the screen flow drives it (design: feature-game-flow/flow-routes/routes-values: build-limit,
    /// back-button, back-key-map; memory changes only when the flow says a battle was built).
    /// </summary>
    public class TeamSelectFlowTests
    {
        private const string BuildErrorText = TeamSelectScreenSpec.BuildErrorText;

        [Test]
        public void FailedBuildKeepsTeamInSlotsShowsErrorAndEnterBattleWorksAgain()
        {
            var s = new TestScreen().Open();
            s.BuildSucceeds = false;
            s.Presenter.TapSlot(1);
            s.Presenter.TapCard(Mage);
            Assert.IsTrue(s.Presenter.TapStart());
            Assert.IsFalse(s.View.StartEnabled, "locked while the battle is built");

            // The flow: build failed or took too long.
            s.Presenter.ShowBuildError(BuildErrorText);
            s.Presenter.SetEnterBattleEnabled(true);

            Assert.AreEqual(BuildErrorText, s.View.BuildError);
            Assert.IsTrue(s.Presenter.IsOpen, "stays on the team select screen");
            CollectionAssert.AreEqual(new[] { Knight, Mage, Archer }, s.SlotCodes(), "confirmed team still in the slots");
            Assert.IsFalse(s.Memory.HasTeam, "remembered team unchanged");
            Assert.IsTrue(s.View.StartEnabled);

            s.BuildSucceeds = false;
            Assert.IsTrue(s.Presenter.TapStart(), "retry with the same team");
            Assert.IsNull(s.View.BuildError, "the error line hides on the tap");
            Assert.AreEqual(2, s.Entered.Count);
            CollectionAssert.AreEqual(new[] { Knight, Mage, Archer }, s.Entered[1]);

            s.Presenter.RememberTeam(s.Entered[1]); // the flow: built this time
            s.Presenter.Hide();
            CollectionAssert.AreEqual(new[] { Knight, Mage, Archer }, s.Memory.Team);
        }

        [Test]
        public void RememberedTeamChangesOnlyWhenFlowCallsRememberTeam()
        {
            var s = new TestScreen().Open();
            s.BuildSucceeds = false;
            s.Presenter.TapSlot(0);
            s.Presenter.TapCard(Swordsman);
            s.Presenter.TapStart();
            Assert.IsFalse(s.Memory.HasTeam, "Enter battle alone does not remember");

            s.Presenter.Hide();
            s.Open();
            CollectionAssert.AreEqual(DefaultTeam, s.SlotCodes(), "no battle was built: default team again");

            s.Presenter.RememberTeam(new[] { Mage, Archer, Knight });
            s.Open();
            CollectionAssert.AreEqual(new[] { Mage, Archer, Knight }, s.SlotCodes());
        }

        [Test]
        public void BackButtonReportsBackKeepsRememberedTeamAndDropsHalfPickedSlots()
        {
            var s = new TestScreen().Open();
            s.Presenter.TapStart(); // default team built and remembered
            s.Open();
            s.Presenter.TapSlot(2);
            s.Presenter.TapCard(Mage);
            s.Presenter.TapBack();
            s.Presenter.TapBack(); // the screen is gone: no second report

            Assert.AreEqual(1, s.BackCount, "reported to the screen flow exactly once");
            Assert.IsFalse(s.View.Visible, "back to the home screen");
            CollectionAssert.AreEqual(DefaultTeam, s.Memory.Team, "remembered team unchanged");
            s.Open();
            CollectionAssert.AreEqual(DefaultTeam, s.SlotCodes());
        }

        [Test]
        public void BackIsIgnoredWhileTheBattleIsBeingBuilt()
        {
            var s = new TestScreen().Open();
            s.BuildSucceeds = false;
            s.Presenter.TapStart();
            s.Presenter.TapBack(); // Enter battle, then Back right away
            Assert.AreEqual(0, s.BackCount);
            Assert.IsTrue(s.Presenter.IsOpen);
            Assert.AreEqual(1, s.Entered.Count, "the battle build goes on");
        }

        [Test]
        public void BackAfterFailedBuildGoesHomeWithLastBuiltTeamRemembered()
        {
            var s = new TestScreen().Open();
            s.Presenter.TapStart(); // default team built
            s.Open();
            s.BuildSucceeds = false;
            s.Presenter.TapSlot(0);
            s.Presenter.TapCard(Mage);
            s.Presenter.TapStart();
            s.Presenter.ShowBuildError(BuildErrorText);
            s.Presenter.SetEnterBattleEnabled(true);

            s.Presenter.TapBack();
            Assert.AreEqual(1, s.BackCount);
            CollectionAssert.AreEqual(DefaultTeam, s.Memory.Team);
            Assert.IsNull(s.View.BuildError, "error line gone with the screen");
        }

        [Test]
        public void LockedByFlowTakesNoTapsAndShowsLockedButton()
        {
            var s = new TestScreen().Open();
            s.Presenter.SetEnterBattleEnabled(false);
            s.Presenter.TapSlot(0);
            s.Presenter.TapCard(Mage);
            Assert.IsFalse(s.Presenter.TapStart());
            Assert.IsFalse(s.View.StartEnabled);
            CollectionAssert.AreEqual(DefaultTeam, s.SlotCodes());
            Assert.AreEqual(0, s.Entered.Count);
        }

        [Test]
        public void BuildErrorLineShowsHidesAndClearsOnNextShow()
        {
            var s = new TestScreen().Open();
            Assert.IsNull(s.View.BuildError);
            s.Presenter.ShowBuildError(BuildErrorText);
            Assert.AreEqual(BuildErrorText, s.View.BuildError);
            s.Presenter.HideBuildError();
            Assert.IsNull(s.View.BuildError);

            s.Presenter.ShowBuildError(BuildErrorText);
            s.Presenter.Hide();
            s.Open();
            Assert.IsNull(s.View.BuildError, "a new showing starts without the error");
        }

        [Test]
        public void HideDropsSlotsPickedButNotConfirmed()
        {
            var s = new TestScreen().Open();
            s.Presenter.TapSlot(0);
            s.Presenter.TapSlot(1);
            s.Presenter.Hide();
            Assert.IsFalse(s.View.Visible);
            s.Presenter.TapCard(Mage);
            s.Open();
            CollectionAssert.AreEqual(DefaultTeam, s.SlotCodes());
        }
    }
}
