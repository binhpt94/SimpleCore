using NUnit.Framework;
using static Game.TeamSelect.Tests.Fakes;

namespace Game.TeamSelect.Tests
{
    /// <summary>Design: feature-team-select/team-slots/slots-spec.</summary>
    public class TeamSlotsTests
    {
        private static TestScreen OpenEmpty()
        {
            var s = new TestScreen().Open();
            s.Clear();
            return s;
        }

        [Test]
        public void TeamHasThreeSlotsPickedFromFiveHeroes()
        {
            var s = OpenEmpty();
            Assert.AreEqual(3, s.Presenter.Slots.Count);
            Assert.AreEqual(5, s.Presenter.Cards.Count);
            foreach (var code in new[] { Knight, Axeman, Swordsman, Archer, Mage }) s.Presenter.TapCard(code);
            var picked = 0;
            foreach (var card in s.Presenter.Cards) if (card.Picked) picked++;
            Assert.AreEqual(3, picked, "only 3 of the 5 heroes fit; 2 stay out");
        }

        [Test]
        public void SameHeroCannotBeInTwoSlots()
        {
            var s = OpenEmpty();
            s.Presenter.TapCard(Knight);
            s.Presenter.TapCard(Knight);
            s.Presenter.TapCard(Knight);
            CollectionAssert.AreEqual(new[] { Knight, null, null }, s.SlotCodes());
        }

        [Test]
        public void TappingUnpickedHeroFillsLowestEmptySlot()
        {
            var s = OpenEmpty();
            s.Presenter.TapCard(Knight);
            s.Presenter.TapCard(Axeman);
            s.Presenter.TapCard(Archer);
            s.Presenter.TapSlot(1);
            s.Presenter.TapCard(Mage);
            CollectionAssert.AreEqual(new[] { Knight, Mage, Archer }, s.SlotCodes());
        }

        [Test]
        public void TappingPickedCardOrItsSlotRemovesHeroWithoutShifting()
        {
            var s = new TestScreen().Open();
            s.Presenter.TapCard(Axeman);
            CollectionAssert.AreEqual(new[] { Knight, null, Archer }, s.SlotCodes());
            s.Presenter.TapSlot(0);
            CollectionAssert.AreEqual(new string[] { null, null, Archer }, s.SlotCodes());
        }

        [Test]
        public void TappingUnpickedHeroWhenFullChangesNothing()
        {
            var s = new TestScreen().Open();
            var shownBefore = s.View.CardsShown;
            s.Presenter.TapCard(Mage);
            CollectionAssert.AreEqual(DefaultTeam, s.SlotCodes());
            Assert.IsFalse(s.Presenter.Slots.Contains(Mage));
            Assert.AreEqual(shownBefore, s.View.CardsShown, "nothing changed, nothing redrawn");
        }

        [Test]
        public void StartButtonIsLockedAndDoesNothingUntilThreeSlotsFilled()
        {
            var s = new TestScreen().Open();
            s.Presenter.TapSlot(2);
            Assert.IsFalse(s.View.StartEnabled);
            Assert.IsFalse(s.Presenter.TapStart());
            Assert.AreEqual(0, s.Entered.Count);
            Assert.IsTrue(s.Presenter.IsOpen);

            s.Presenter.TapCard(Mage);
            Assert.IsTrue(s.View.StartEnabled);
        }

        [Test]
        public void StartWithFullTeamReportsTeamToFlowOnceAndLocksTapsUntilBuildResult()
        {
            var s = new TestScreen().Open();
            s.BuildSucceeds = false; // the battle is still being built
            Assert.IsTrue(s.Presenter.TapStart());
            Assert.IsFalse(s.Presenter.TapStart());
            s.Presenter.TapSlot(0);
            s.Presenter.TapCard(Mage);

            Assert.AreEqual(1, s.Entered.Count, "team reported to the screen flow once");
            CollectionAssert.AreEqual(DefaultTeam, s.Entered[0]);
            CollectionAssert.AreEqual(DefaultTeam, s.SlotCodes(), "taps while waiting are ignored");
            Assert.IsFalse(s.View.StartEnabled);

            // Result: build failed, the flow unlocks the screen; taps count again.
            s.Presenter.SetEnterBattleEnabled(true);
            s.Presenter.TapSlot(0);
            CollectionAssert.AreEqual(new string[] { null, Axeman, Archer }, s.SlotCodes());

            // Result: build done, the flow hides the screen.
            s.Presenter.TapCard(Knight);
            s.BuildSucceeds = true;
            Assert.IsTrue(s.Presenter.TapStart());
            Assert.IsFalse(s.View.Visible);
        }

        [Test]
        public void ScreenHasFourPartsCardsSlotsStartAndBack()
        {
            var s = new TestScreen();
            Assert.IsFalse(s.View.Visible, "hidden until the screen flow shows it");
            s.Open();
            Assert.IsTrue(s.View.Visible);
            Assert.AreEqual(5, s.View.Cards.Count, "5 hero cards");
            Assert.AreEqual(3, s.View.Slots.Count, "3 team slots");
            Assert.IsTrue(s.View.StartEnabled, "Enter battle button");
            Assert.AreEqual(2, TeamSelectScreenSpec.ButtonCount, "Enter battle and Back");
            Assert.AreEqual(40f, TeamSelectScreenSpec.BackX, "Back sits in the top left corner");
            Assert.AreEqual(40f, TeamSelectScreenSpec.BackY);
            Assert.GreaterOrEqual(TeamSelectScreenSpec.BackW, 120f);
            Assert.GreaterOrEqual(TeamSelectScreenSpec.BackH, 120f);

            s.View.Input.TapBack();
            Assert.AreEqual(1, s.BackCount, "the Back button reaches the screen flow");
        }

        [Test]
        public void TwoCardsTappedAtOnceWithOneSlotLeftOnlyFirstGetsIn()
        {
            var s = new TestScreen().Open();
            s.Presenter.TapSlot(1);
            s.Presenter.TapCard(Swordsman);
            s.Presenter.TapCard(Mage);
            CollectionAssert.AreEqual(new[] { Knight, Swordsman, Archer }, s.SlotCodes());
        }

        [Test]
        public void TappingEmptySlotDoesNothing()
        {
            var s = new TestScreen().Open();
            s.Presenter.TapSlot(1);
            var shownBefore = s.View.CardsShown;
            s.Presenter.TapSlot(1);
            CollectionAssert.AreEqual(new[] { Knight, null, Archer }, s.SlotCodes());
            Assert.AreEqual(shownBefore, s.View.CardsShown);
        }

        [Test]
        public void RemovedHeroPickedAgainGoesToLowestEmptySlot()
        {
            var s = new TestScreen().Open();
            s.Presenter.TapCard(Archer);
            s.Presenter.TapCard(Knight);
            s.Presenter.TapCard(Archer);
            CollectionAssert.AreEqual(new[] { Archer, Axeman, null }, s.SlotCodes(), "Archer left slot 3, came back in slot 1");
        }

        [Test]
        public void RememberedTeamWithMissingHeroLeavesStartLockedUntilFull()
        {
            var memory = new TeamMemory();
            memory.Remember(new[] { Knight, "removed-hero", Archer });
            var s = new TestScreen(memory).Open();
            CollectionAssert.AreEqual(new[] { Knight, null, Archer }, s.SlotCodes());
            Assert.IsFalse(s.View.StartEnabled);
            s.Presenter.TapCard(Mage);
            Assert.IsTrue(s.View.StartEnabled);
        }

        [Test]
        public void DeviceBackKeyActsAsBackButton()
        {
            // feature-game-flow/flow-routes/routes-values#back-key-map: on the team select screen the device back
            // key does what the Back button does (live slots-spec still says "nothing"; the routing node replaces it).
            var s = new TestScreen().Open();
            s.Presenter.TapSlot(0);
            s.View.Input.TapBack(); // the view sends the device key and the Back button the same way
            Assert.AreEqual(1, s.BackCount);
            Assert.IsFalse(s.Presenter.IsOpen);
            Assert.IsFalse(s.Memory.HasTeam, "remembered team unchanged");
            s.Open();
            CollectionAssert.AreEqual(DefaultTeam, s.SlotCodes(), "half-picked slots were dropped");

            s.BuildSucceeds = false;
            s.Presenter.TapStart();
            s.View.Input.TapBack();
            Assert.AreEqual(1, s.BackCount, "ignored while waiting for the battle to be built");
        }

        [Test]
        public void DesignValuesAreThreeSlotsAndDefaultTeam()
        {
            var rules = Rules();
            Assert.AreEqual(3, rules.SlotCount);
            CollectionAssert.AreEqual(DefaultTeam, rules.DefaultTeam);
            var config = UnityEngine.ScriptableObject.CreateInstance<TeamSelectConfig>();
            var fromConfig = config.ToRules();
            Assert.AreEqual(3, fromConfig.SlotCount);
            Assert.AreEqual(3, fromConfig.DefaultTeam.Count);
            UnityEngine.Object.DestroyImmediate(config);

            Assert.AreEqual(2, TeamSelectScreenSpec.ButtonCount, "Enter battle and Back");
            Assert.AreEqual(120f, TeamSelectScreenSpec.BackMinSide);
            Assert.AreEqual("Không vào được trận, thử lại", TeamSelectScreenSpec.BuildErrorText);
            Assert.AreEqual(1, TeamSelectScreenSpec.StartTapsPerOpening);
            Assert.AreEqual(10, TeamCount(5, rules.SlotCount), "10 teams from 5 heroes");
        }

        private static int TeamCount(int heroes, int slots)
        {
            var n = 1;
            for (var i = 0; i < slots; i++) n = n * (heroes - i) / (i + 1);
            return n;
        }
    }
}
