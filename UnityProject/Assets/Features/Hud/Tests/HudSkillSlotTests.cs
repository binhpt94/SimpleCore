using Game.Castle;
using NUnit.Framework;

namespace Game.Hud.Tests
{
    public sealed class HudSkillSlotTests
    {
        private FakeView _view;
        private FakeSkills _skills;
        private FakeRun _run;

        [SetUp]
        public void SetUp()
        {
            _view = new FakeView();
            _skills = new FakeSkills();
            _run = new FakeRun();
        }

        private HudSkillPresenter Start()
        {
            var buttons = new HudSkillPresenter(_view, _skills, _run, HudTestTimings.Default);
            buttons.Start();
            return buttons;
        }

        [Test]
        public void ThreeSkillsFillTheThreeSlotsThunderFrostRally()
        {
            Start();

            CollectionAssert.AreEqual(new[] { 0, 1, 2 }, _view.SkillSlots);
        }

        [Test]
        public void ThunderAloneStaysInTheLeftSlotAndTheOthersStayEmpty()
        {
            _skills.Skills = new[] { CastleSkill.Thunder };

            Start();

            CollectionAssert.AreEqual(new[] { "Sấm sét" }, _view.SkillNames);
            CollectionAssert.AreEqual(new[] { 0 }, _view.SkillSlots);
            Assert.AreEqual(1420f, HudSkillSlots.Left(_view.SkillSlots[0]));
        }

        [Test]
        public void EachSkillKeepsItsSlotWhateverOrderTheCastleListsThem()
        {
            _skills.Skills = new[] { CastleSkill.Rally, CastleSkill.Thunder };

            Start();

            CollectionAssert.AreEqual(new[] { "Cổ vũ", "Sấm sét" }, _view.SkillNames);
            CollectionAssert.AreEqual(new[] { 2, 0 }, _view.SkillSlots);
        }

        [Test]
        public void TappingAButtonUsesTheSkillInThatButtonNotTheSlotIndex()
        {
            _skills.Skills = new[] { CastleSkill.Frost };
            Start();

            _view.Press(0);

            CollectionAssert.AreEqual(new[] { CastleSkill.Frost }, _skills.Used);
            Assert.AreEqual(1, _view.SkillSlots[0]);
        }

        [Test]
        public void SlotsSitInTheBottomRightClusterAndTheTrayKeepsAllThree()
        {
            Assert.AreEqual(1420f, HudSkillSlots.Left(0));
            Assert.AreEqual(1580f, HudSkillSlots.Left(1));
            Assert.AreEqual(1740f, HudSkillSlots.Left(2));
            Assert.AreEqual(1880f, HudSkillSlots.Left(2) + HudSkillSlots.Size);
            Assert.AreEqual(460f, HudSkillSlots.ClusterWidth);
        }
    }
}
