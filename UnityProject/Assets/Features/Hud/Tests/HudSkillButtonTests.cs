using Game.Castle;
using Game.Run;
using NUnit.Framework;

namespace Game.Hud.Tests
{
    public sealed class HudSkillButtonTests
    {
        private FakeView _view;
        private FakeSkills _skills;
        private FakeRun _run;
        private HudSkillPresenter _buttons;

        [SetUp]
        public void SetUp()
        {
            _view = new FakeView();
            _skills = new FakeSkills();
            _run = new FakeRun();
            _buttons = new HudSkillPresenter(_view, _skills, _run, HudTestTimings.Default);
            _buttons.Start();
        }

        [Test]
        public void ThreeButtonsInCastleOrderAllReadyAtBattleStart()
        {
            CollectionAssert.AreEqual(new[] { "Sấm sét", "Băng giá", "Cổ vũ" }, _view.SkillNames);
            foreach (var shown in _view.Skills)
            {
                Assert.IsTrue(shown.Ready);
                Assert.AreEqual(string.Empty, shown.Seconds);
                Assert.AreEqual(0f, shown.Cover);
                Assert.AreEqual(1f, shown.Scale);
            }
        }

        [Test]
        public void TappingAReadyButtonUsesThatSkillOnce()
        {
            _view.Press(0);
            _view.Press(0);

            CollectionAssert.AreEqual(new[] { CastleSkill.Thunder }, _skills.Used);
            Assert.IsFalse(_view.Skills[0].Ready);
            Assert.AreEqual("20", _view.Skills[0].Seconds);
        }

        [Test]
        public void TappingACoolingButtonDoesNotAskTheCastle()
        {
            _skills.SetCooling(CastleSkill.Frost, 12f, 35f);
            _buttons.Tick(0.016f);

            _view.Press(1);

            CollectionAssert.IsEmpty(_skills.Used);
        }

        [Test]
        public void CoolingButtonShowsSecondsRoundedUpAndCoverForTheShareLeft()
        {
            _skills.SetCooling(CastleSkill.Frost, 12f, 35f);
            _skills.SetCooling(CastleSkill.Rally, 0.3f, 25f);
            _buttons.Tick(0.016f);

            Assert.AreEqual("12", _view.Skills[1].Seconds);
            Assert.AreEqual(12f / 35f, _view.Skills[1].Cover, 1e-5f);
            Assert.AreEqual("1", _view.Skills[2].Seconds);
        }

        [Test]
        public void HalfTheCooldownLeftCoversHalfTheButton()
        {
            _skills.SetCooling(CastleSkill.Thunder, 10f, 20f);
            _buttons.Tick(0.016f);

            Assert.AreEqual(0.5f, _view.Skills[0].Cover, 1e-5f);
        }

        [Test]
        public void NoButtonTakesTapsWhilePausedOrAfterTheRunEnded()
        {
            _run.SetStep(RunStep.Paused);
            _view.Press(0);
            _run.SetStep(RunStep.Ending);
            _view.Press(1);
            _run.SetStep(RunStep.Lost);
            _view.Press(2);

            CollectionAssert.IsEmpty(_skills.Used);
        }

        [Test]
        public void ButtonBouncesOnceWhenItsCooldownEnds()
        {
            _skills.SetCooling(CastleSkill.Thunder, 0.1f, 20f);
            _buttons.Tick(0.016f);
            _skills.SetReady(CastleSkill.Thunder);

            _buttons.Tick(0.016f);
            _buttons.Tick(0.15f);
            Assert.Greater(_view.Skills[0].Scale, 1.15f);

            _buttons.Tick(0.2f);
            Assert.AreEqual(1f, _view.Skills[0].Scale);

            _buttons.Tick(0.2f);
            Assert.AreEqual(1f, _view.Skills[0].Scale);
        }

        [Test]
        public void BounceStandsStillWhilePaused()
        {
            _skills.SetCooling(CastleSkill.Thunder, 0.1f, 20f);
            _buttons.Tick(0.016f);
            _skills.SetReady(CastleSkill.Thunder);
            _buttons.Tick(0.016f);
            _buttons.Tick(0.1f);
            var before = _view.Skills[0].Scale;

            _run.SetStep(RunStep.Paused);
            _buttons.Tick(1f);

            Assert.AreEqual(before, _view.Skills[0].Scale);
        }

        [Test]
        public void ZeroCooldownSkillTheCastleReportsReadyStaysReadyAndATapUsesIt()
        {
            _skills.SetRaw(CastleSkill.Rally, true, 0f, 0f);
            _buttons.Tick(0.016f);

            Assert.IsTrue(_view.Skills[2].Ready);
            Assert.AreEqual(string.Empty, _view.Skills[2].Seconds);
            Assert.AreEqual(0f, _view.Skills[2].Cover);

            _view.Press(2);

            CollectionAssert.AreEqual(new[] { CastleSkill.Rally }, _skills.Used);
        }

        [Test]
        public void ZeroCooldownWithMismatchedDataShowsNoSecondsAndNoCover()
        {
            _skills.SetRaw(CastleSkill.Rally, false, 5f, 0f);
            _buttons.Tick(0.016f);

            Assert.AreEqual(string.Empty, _view.Skills[2].Seconds);
            Assert.AreEqual(0f, _view.Skills[2].Cover);
            Assert.IsFalse(float.IsNaN(_view.Skills[2].Cover));
        }

        [Test]
        public void TwoButtonsTappedTogetherEachUseTheirOwnSkill()
        {
            _view.Press(0);
            _view.Press(2);

            CollectionAssert.AreEqual(new[] { CastleSkill.Thunder, CastleSkill.Rally }, _skills.Used);
            Assert.IsFalse(_view.Skills[0].Ready);
            Assert.IsFalse(_view.Skills[2].Ready);
            Assert.IsTrue(_view.Skills[1].Ready);
        }

        [Test]
        public void TapInTheFrameTheCastleTurnsReadyUsesTheSkill()
        {
            _skills.SetCooling(CastleSkill.Thunder, 0.01f, 20f);
            _buttons.Tick(0.016f);
            _skills.SetReady(CastleSkill.Thunder);

            _view.Press(0);

            CollectionAssert.AreEqual(new[] { CastleSkill.Thunder }, _skills.Used);
        }

        [Test]
        public void TapInTheFrameTheCastleStillCoolsIsIgnored()
        {
            _skills.SetCooling(CastleSkill.Thunder, 0.01f, 20f);
            _buttons.Tick(0.016f);

            _view.Press(0);

            CollectionAssert.IsEmpty(_skills.Used);
        }

        [Test]
        public void RefusedUseKeepsTheButtonAsTheCastleReportsIt()
        {
            _skills.Refuse = true;

            _view.Press(0);
            _buttons.Tick(0.016f);

            CollectionAssert.AreEqual(new[] { CastleSkill.Thunder }, _skills.Used);
            Assert.IsTrue(_view.Skills[0].Ready);
            Assert.AreEqual(string.Empty, _view.Skills[0].Seconds);
            Assert.AreEqual(0f, _view.Skills[0].Cover);
        }

        [Test]
        public void SkillUpgradesLeaveTheButtonsAndTheirCooldownAsTheyAre()
        {
            // Upgrades do not touch cooldowns (feature-castle/castle-skills): the buttons are made once per battle
            // and keep showing the castle's own cooldown however long the battle runs.
            for (var frame = 0; frame < 300; frame++) _buttons.Tick(0.016f);
            _view.Press(0);
            _buttons.Tick(0.016f);

            Assert.AreEqual(1, _view.SkillButtonsCreated);
            CollectionAssert.AreEqual(new[] { "Sấm sét", "Băng giá", "Cổ vũ" }, _view.SkillNames);
            CollectionAssert.AreEqual(new[] { 0, 1, 2 }, _view.SkillSlots);
            Assert.AreEqual("20", _view.Skills[0].Seconds);
        }

        [Test]
        public void StoppedButtonsIgnoreTaps()
        {
            _buttons.Stop();
            _view.Press(0);

            CollectionAssert.IsEmpty(_skills.Used);
            Assert.AreEqual(0, _view.SkillListeners);
        }
    }
}
