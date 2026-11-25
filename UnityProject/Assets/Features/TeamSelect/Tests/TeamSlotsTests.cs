using NUnit.Framework;
using Game.Run;
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
            Assert.AreEqual(0, s.Run.Entered.Count);
            Assert.AreEqual(RunStep.TeamSelect, s.Run.Step);

            s.Presenter.TapCard(Mage);
            Assert.IsTrue(s.View.StartEnabled);
        }

        [Test]
        public void StartWithFullTeamReportsTeamOnceThenIgnoresTaps()
        {
            var s = new TestScreen().Open();
            s.Run.MoveToPlayingOnEnter = false;
            Assert.IsTrue(s.Presenter.TapStart());
            Assert.IsFalse(s.Presenter.TapStart());
            s.Presenter.TapSlot(0);
            s.Presenter.TapCard(Mage);

            Assert.AreEqual(1, s.Run.Entered.Count);
            CollectionAssert.AreEqual(DefaultTeam, s.Run.Entered[0].HeroCodes);
            CollectionAssert.AreEqual(DefaultTeam, s.SlotCodes(), "taps after Enter battle are ignored");
            Assert.IsFalse(s.View.StartEnabled);
        }

        [Test]
        public void ScreenShowsFiveCardsThreeSlotsAndOneStartButton()
        {
            var s = new TestScreen();
            Assert.IsFalse(s.View.Visible, "hidden outside the team select step");
            s.Open();
            Assert.IsTrue(s.View.Visible);
            Assert.AreEqual(5, s.View.Cards.Count);
            Assert.AreEqual(3, s.View.Slots.Count);
            Assert.IsTrue(s.View.StartEnabled);
            s.Presenter.TapStart();
            Assert.IsFalse(s.View.Visible, "the run moved to battle");
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
        public void DeviceBackKeyDoesNothing()
        {
            var s = new TestScreen().Open();
            s.Presenter.TapBack();
            Assert.AreEqual(RunStep.TeamSelect, s.Run.Step);
            Assert.IsTrue(s.Presenter.IsOpen);
            CollectionAssert.AreEqual(DefaultTeam, s.SlotCodes());
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
        }
    }
}
