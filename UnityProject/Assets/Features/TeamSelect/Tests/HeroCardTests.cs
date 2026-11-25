using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Game.Hero;
using static Game.TeamSelect.Tests.Fakes;

namespace Game.TeamSelect.Tests
{
    /// <summary>Design: feature-team-select/hero-cards/card-spec.</summary>
    public class HeroCardTests
    {
        [Test]
        public void FiveCardsInFixedRosterOrderWhateverTheTeam()
        {
            var s = new TestScreen().Open();
            var expected = new[] { "Hiệp sĩ", "Đấu sĩ rìu", "Kiếm khách", "Cung thủ", "Pháp sư" };
            CollectionAssert.AreEqual(expected, s.View.Cards.Select(c => c.Name).ToArray());
            s.Presenter.TapCard(Knight);
            s.Presenter.TapCard(Mage);
            CollectionAssert.AreEqual(expected, s.View.Cards.Select(c => c.Name).ToArray());
        }

        [Test]
        public void EachCardHasPortraitNameStyleLineAndRoleMark()
        {
            var s = new TestScreen().Open();
            foreach (var card in s.View.Cards)
            {
                Assert.IsTrue(card.HasPortrait, card.HeroCode);
                Assert.IsFalse(string.IsNullOrEmpty(card.Name), card.HeroCode);
                Assert.IsFalse(string.IsNullOrEmpty(card.StyleLine), card.HeroCode);
            }
            Assert.AreEqual(RoleMark.Melee, s.View.Cards[0].Role);
        }

        [Test]
        public void StyleLineIsGroupAndAttackWord()
        {
            var s = new TestScreen().Open();
            var expected = new[]
            {
                "Cận chiến · Chắn", "Cận chiến · Chém vùng", "Cận chiến · Đánh nhanh", "Bắn xa · Tầm xa", "Bắn xa · Nổ vùng",
            };
            CollectionAssert.AreEqual(expected, s.View.Cards.Select(c => c.StyleLine).ToArray());
        }

        [Test]
        public void TwoRoleMarksThreeMeleeTwoRanged()
        {
            var s = new TestScreen().Open();
            var marks = s.View.Cards.Select(c => c.Role).ToArray();
            Assert.AreEqual(3, marks.Count(m => m == RoleMark.Melee));
            Assert.AreEqual(2, marks.Count(m => m == RoleMark.Ranged));
            Assert.AreEqual(2, marks.Distinct().Count());
            Assert.AreEqual(marks[3], marks[4], "both ranged heroes share one mark");
        }

        [Test]
        public void PickedMarkFollowsTeamAtOnce()
        {
            var s = new TestScreen().Open();
            CollectionAssert.AreEqual(new[] { true, true, false, true, false }, s.View.Cards.Select(c => c.Picked).ToArray());
            s.Presenter.TapCard(Axeman);
            Assert.IsFalse(s.View.Cards[1].Picked, "mark gone in the same refresh as the removal");
            s.Presenter.TapCard(Swordsman);
            Assert.IsTrue(s.View.Cards[2].Picked);
        }

        [Test]
        public void CardShowsNoStatNumbers()
        {
            var s = new TestScreen().Open();
            foreach (var card in s.View.Cards)
            {
                Assert.IsFalse(card.Name.Any(char.IsDigit), card.Name);
                Assert.IsFalse(card.StyleLine.Any(char.IsDigit), card.StyleLine);
            }
        }

        [Test]
        public void NewRosterHeroGetsCardAtItsRosterPlaceWithStyleFromGroupAndAttack()
        {
            var heroes = FiveHeroes();
            heroes.Insert(3, new HeroInfo("spearman", "Thương binh", HeroGroup.Melee, HeroAttack.FrontCleave));
            var s = new TestScreen(roster: new FakeRoster(heroes)).Open();
            Assert.AreEqual(6, s.View.Cards.Count);
            var card = s.View.Cards[3];
            Assert.AreEqual("spearman", card.HeroCode);
            Assert.AreEqual("Cận chiến · Chém vùng", card.StyleLine);
            Assert.AreEqual(RoleMark.Melee, card.Role);
        }

        [Test]
        public void HeroWithoutPictureStillShowsTextAndCanBePicked()
        {
            var s = new TestScreen().Open();
            s.Portraits.Missing.Add(Mage);
            s.Presenter.TapCard(Archer);
            s.Presenter.TapCard(Mage);
            var card = s.View.Cards[4];
            Assert.IsFalse(card.HasPortrait);
            Assert.AreEqual("Pháp sư", card.Name);
            Assert.AreEqual("Bắn xa · Nổ vùng", card.StyleLine);
            Assert.IsTrue(card.Picked);
        }

        [Test]
        public void TappingUnpickedCardWhenTeamFullLeavesCardUnchanged()
        {
            var s = new TestScreen().Open();
            var before = s.View.Cards[2];
            s.Presenter.TapCard(Swordsman);
            Assert.IsFalse(s.View.Cards[2].Picked);
            Assert.AreEqual(before.StyleLine, s.View.Cards[2].StyleLine);
        }

        [Test]
        public void DesignValuesFiveCardsTwoMarksStyleTable()
        {
            var rules = Rules();
            var lines = new Dictionary<string, string>();
            foreach (var hero in FiveHeroes()) lines[hero.Code] = rules.StyleLine(hero);
            Assert.AreEqual(5, lines.Count);
            Assert.AreEqual("Cận chiến · Chắn", lines[Knight]);
            Assert.AreEqual("Cận chiến · Chém vùng", lines[Axeman]);
            Assert.AreEqual("Cận chiến · Đánh nhanh", lines[Swordsman]);
            Assert.AreEqual("Bắn xa · Tầm xa", lines[Archer]);
            Assert.AreEqual("Bắn xa · Nổ vùng", lines[Mage]);
            Assert.AreEqual(2, System.Enum.GetValues(typeof(RoleMark)).Length);
        }
    }
}
