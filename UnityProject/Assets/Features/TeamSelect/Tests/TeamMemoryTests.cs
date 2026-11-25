using System.Linq;
using NUnit.Framework;
using Game.Run;
using static Game.TeamSelect.Tests.Fakes;

namespace Game.TeamSelect.Tests
{
    /// <summary>Design: feature-team-select/team-memory/memory-rules and feature-team-select/cheat.</summary>
    public class TeamMemoryTests
    {
        [Test]
        public void WithoutRememberedTeamSlotsHoldDefaultTeam()
        {
            var s = new TestScreen().Open();
            Assert.IsFalse(s.Memory.HasTeam);
            CollectionAssert.AreEqual(new[] { Knight, Axeman, Archer }, s.SlotCodes());
            Assert.IsTrue(s.View.StartEnabled, "a new player can enter battle at once");
        }

        [Test]
        public void EnterBattleRemembersTeamInSlotOrder()
        {
            var s = new TestScreen().Open();
            s.Presenter.TapSlot(0);
            s.Presenter.TapCard(Mage);
            s.Presenter.TapStart();
            CollectionAssert.AreEqual(new[] { Mage, Axeman, Archer }, s.Memory.Team);
        }

        [Test]
        public void OpeningFillsSlotsFromRememberedTeamInOrder()
        {
            var s = new TestScreen().Open();
            s.Clear();
            s.Presenter.TapCard(Mage);
            s.Presenter.TapCard(Swordsman);
            s.Presenter.TapCard(Knight);
            s.Presenter.TapStart();
            s.Run.Lose();
            s.Run.Replay();
            CollectionAssert.AreEqual(new[] { Mage, Swordsman, Knight }, s.SlotCodes());
        }

        [Test]
        public void NewGameForgetsTeamAndUsesDefault()
        {
            var first = new TestScreen().Open();
            first.Presenter.TapSlot(0);
            first.Presenter.TapCard(Mage);
            first.Presenter.TapStart();
            // Quitting drops every object of the game; a new game builds a new, empty memory.
            var second = new TestScreen().Open();
            CollectionAssert.AreEqual(DefaultTeam, second.SlotCodes());
        }

        [Test]
        public void AbandonThenPlayAgainKeepsAbandonedRunTeam()
        {
            var s = new TestScreen().Open();
            s.Presenter.TapSlot(2);
            s.Presenter.TapCard(Swordsman);
            s.Presenter.TapStart();
            s.Run.Abandon();
            s.Run.Play();
            CollectionAssert.AreEqual(new[] { Knight, Axeman, Swordsman }, s.SlotCodes());
        }

        [Test]
        public void ChangingTeamOnScreenOnlyRemembersAtEnterBattle()
        {
            var s = new TestScreen().Open();
            s.Presenter.TapStart();
            s.Run.Lose();
            s.Run.Replay();
            s.Presenter.TapSlot(0);
            s.Presenter.TapCard(Mage);
            CollectionAssert.AreEqual(DefaultTeam, s.Memory.Team, "not remembered while still choosing");
            s.Presenter.TapStart();
            CollectionAssert.AreEqual(new[] { Mage, Axeman, Archer }, s.Memory.Team);
        }

        [Test]
        public void RememberedHeroMissingFromRosterLeavesSlotEmpty()
        {
            var memory = new TeamMemory();
            memory.Remember(new[] { Knight, Axeman, Mage });
            var heroes = FiveHeroes().Where(h => h.Code != Axeman).ToList();
            var s = new TestScreen(memory, new FakeRoster(heroes)).Open();
            CollectionAssert.AreEqual(new[] { Knight, null, Mage }, s.SlotCodes());
            Assert.IsFalse(s.View.StartEnabled);
            s.Presenter.TapCard(Archer);
            CollectionAssert.AreEqual(new[] { Knight, Archer, Mage }, s.SlotCodes());
        }

        [Test]
        public void DefaultHeroMissingFromRosterLeavesSlotEmpty()
        {
            var heroes = FiveHeroes().Where(h => h.Code != Archer).ToList();
            var s = new TestScreen(roster: new FakeRoster(heroes)).Open();
            CollectionAssert.AreEqual(new[] { Knight, Axeman, null }, s.SlotCodes());
            Assert.IsFalse(s.View.StartEnabled);
        }

        [Test]
        public void DesignValuesOneTeamDefaultKnightAxemanArcher()
        {
            var memory = new TeamMemory();
            Assert.IsNull(memory.Team);
            CollectionAssert.AreEqual(DefaultTeam, memory.TeamToShow(DefaultTeam));
            memory.Remember(new[] { Mage, Archer, Knight });
            memory.Remember(new[] { Knight, Swordsman, Archer });
            CollectionAssert.AreEqual(new[] { Knight, Swordsman, Archer }, memory.TeamToShow(DefaultTeam), "one team only, the last one");

            var config = UnityEngine.ScriptableObject.CreateInstance<TeamSelectConfig>();
            CollectionAssert.AreEqual(new[] { "knight", "axeman", "archer" }, config.ToRules().DefaultTeam);
            UnityEngine.Object.DestroyImmediate(config);
        }

        [Test]
        public void CheatFullTeamOpensScreenWithKnightAxemanArcherAndLitButton()
        {
            var s = new TestScreen();
            s.Run.SetStep(RunStep.TeamSelect);
            s.Presenter.TapSlot(0);
            TeamSelectCheat.ApplyFullTeam(s.Presenter, s.Memory, DefaultTeam);
            CollectionAssert.AreEqual(DefaultTeam, s.SlotCodes());
            CollectionAssert.AreEqual(new[] { true, true, false, true, false }, s.View.Cards.Select(c => c.Picked).ToArray());
            Assert.IsTrue(s.View.Visible);
            Assert.IsTrue(s.View.StartEnabled);
        }

        [Test]
        public void CheatParamsStateNameIsFullTeam()
        {
            Assert.AreEqual("full-team", TeamSelectCheat.FullTeam);
        }
    }
}
