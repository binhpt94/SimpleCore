using System.Collections.Generic;
using NUnit.Framework;
using static Game.TeamSelect.Tests.Fakes;

namespace Game.TeamSelect.Tests
{
    /// <summary>The screen as the screen flow (GameFlow) sees it, through the <see cref="ITeamSelectScreen"/> contract.</summary>
    public class TeamSelectScreenTests
    {
        private sealed class Wired
        {
            public readonly FakeView View = new FakeView();
            public readonly TeamMemory Memory = new TeamMemory();
            public readonly TeamSelectPresenter Presenter;
            public readonly ITeamSelectScreen Screen;
            public readonly List<string[]> Entered = new List<string[]>();
            public int Back;

            public Wired()
            {
                Presenter = new TeamSelectPresenter(new FakeRoster(), Rules(), Memory);
                Presenter.Attach(View, new FakePortraits());
                Screen = new TeamSelectScreen(Presenter);
                Screen.EnterBattlePressed += team => Entered.Add(new List<string>(team).ToArray());
                Screen.BackPressed += () => Back++;
            }
        }

        [Test]
        public void FlowShowsAndHidesTheScreen()
        {
            var w = new Wired();
            Assert.IsFalse(w.View.Visible);
            w.Screen.Show();
            Assert.IsTrue(w.View.Visible);
            CollectionAssert.AreEqual(DefaultTeam, w.Presenter.Slots.ToArray());
            w.Screen.Hide();
            Assert.IsFalse(w.View.Visible);
        }

        [Test]
        public void EnterBattleReportsTeamInSlotOrderThroughTheContract()
        {
            var w = new Wired();
            w.Screen.Show();
            w.View.Input.TapSlot(0);
            w.View.Input.TapCard(Mage);
            w.View.Input.TapStart();
            Assert.AreEqual(1, w.Entered.Count);
            CollectionAssert.AreEqual(new[] { Mage, Axeman, Archer }, w.Entered[0]);
        }

        [Test]
        public void BackButtonIsReportedThroughTheContract()
        {
            var w = new Wired();
            w.Screen.Show();
            w.View.Input.TapBack();
            Assert.AreEqual(1, w.Back);
        }

        [Test]
        public void FlowLocksUnlocksAndShowsBuildErrorThroughTheContract()
        {
            var w = new Wired();
            w.Screen.Show();
            w.Screen.SetEnterBattleEnabled(false);
            Assert.IsFalse(w.View.StartEnabled);
            w.Screen.ShowBuildError("Không vào được trận, thử lại");
            Assert.AreEqual("Không vào được trận, thử lại", w.View.BuildError);
            w.Screen.SetEnterBattleEnabled(true);
            Assert.IsTrue(w.View.StartEnabled);
            w.Screen.HideBuildError();
            Assert.IsNull(w.View.BuildError);
        }

        [Test]
        public void RememberTeamThroughTheContractFillsSlotsOnNextShow()
        {
            var w = new Wired();
            w.Screen.RememberTeam(new[] { Swordsman, Mage, Knight });
            w.Screen.Show();
            CollectionAssert.AreEqual(new[] { Swordsman, Mage, Knight }, w.Presenter.Slots.ToArray());
        }

        [Test]
        public void ListenersRemovedFromTheContractNoLongerHear()
        {
            var w = new Wired();
            var heard = 0;
            System.Action back = () => heard++;
            w.Screen.BackPressed += back;
            w.Screen.BackPressed -= back;
            w.Screen.Show();
            w.View.Input.TapBack();
            Assert.AreEqual(0, heard);
            Assert.AreEqual(1, w.Back);
        }
    }
}
