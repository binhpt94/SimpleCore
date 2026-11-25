using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using Game.Core;
using UnityEngine;
using static Game.TeamSelect.Tests.Fakes;

namespace Game.TeamSelect.Tests
{
    public class TeamSelectSessionTests
    {
        private sealed class FakeViewFactory : ITeamSelectViewFactory
        {
            public FakeView Last;
            public int Created;
            public int Destroyed;

            public ITeamSelectView Create()
            {
                Created++;
                return Last = new FakeView();
            }

            public void Destroy(ITeamSelectView view) => Destroyed++;

            public Object HolderOf(ITeamSelectView view) => null;
        }

        [Test]
        public void RunCreatesScreenThatOpensWhenFlowShowsItAndEndDestroysIt()
        {
            var views = new FakeViewFactory();
            var session = new TeamSelectSession(new FakeRoster(), Rules(), views);
            var entered = new List<string[]>();
            session.Presenter.EnterBattlePressed += team => entered.Add(new List<string>(team).ToArray());
            session.LoadAsync(CancellationToken.None).GetAwaiter().GetResult();
            session.Run();

            Assert.AreEqual(1, views.Created);
            Assert.IsTrue(session.HasView);
            Assert.IsFalse(views.Last.Visible);
            session.Presenter.Show();
            Assert.IsTrue(views.Last.Visible);
            CollectionAssert.AreEqual(DefaultTeam, session.Presenter.Slots.ToArray());

            views.Last.Input.TapCard(Axeman);
            views.Last.Input.TapCard(Mage);
            Assert.IsTrue(views.Last.Input.TapStart());
            CollectionAssert.AreEqual(new[] { Knight, Mage, Archer }, entered[0]);

            session.DisposeAsync().GetAwaiter().GetResult();
            Assert.AreEqual(1, views.Destroyed);
            Assert.IsFalse(session.HasView);
            session.Presenter.SetEnterBattleEnabled(true);
            views.Last.Input.TapStart();
            Assert.AreEqual(1, entered.Count, "an ended screen reports nothing");
        }

        [Test]
        public void ShownBeforeTheViewExistsAppearsOnceTheViewIsCreated()
        {
            var views = new FakeViewFactory();
            var session = new TeamSelectSession(new FakeRoster(), Rules(), views);
            session.Presenter.Show();
            session.Presenter.ShowBuildError("Không vào được trận, thử lại");
            session.Run();
            Assert.IsTrue(views.Last.Visible);
            Assert.AreEqual("Không vào được trận, thử lại", views.Last.BuildError);
            Assert.AreEqual(3, views.Last.Slots.Count);
        }

        /// <summary>Pairs each fake view with a plain GameObject standing in for the prefab instance on screen.</summary>
        private sealed class UnityViewFactory : ITeamSelectViewFactory
        {
            public GameObject Last;
            private FakeView _view;

            public ITeamSelectView Create()
            {
                Last = new GameObject("TeamSelectView");
                return _view = new FakeView();
            }

            public void Destroy(ITeamSelectView view)
            {
                if (view == _view && Last != null) Object.DestroyImmediate(Last);
            }

            public Object HolderOf(ITeamSelectView view) => view == _view ? Last : null;
        }

        [Test]
        public void ScreenIsCountedAsHeldWhileOpenAndCountsReturnToZeroAfterTheSessionEnds()
        {
            var views = new UnityViewFactory();
            var session = new TeamSelectSession(new FakeRoster(), Rules(), views);
            ISessionParticipantUsage usage = session;
            Assert.IsTrue(usage.OpensViewsOnRun);

            session.Run();
            Assert.AreEqual(1, usage.HeldViews, "the open team select screen is held");
            Assert.AreEqual(0, usage.RunningJobs, "team select runs no timed jobs");

            session.DisposeAsync().GetAwaiter().GetResult();
            Assert.AreEqual(0, usage.HeldViews);
            Assert.AreEqual(0, usage.RunningJobs);
            Assert.IsTrue(views.Last == null, "the view object is destroyed");
        }

        [Test]
        public void RememberedTeamLastsAcrossRunsOfTheSameSession()
        {
            var views = new FakeViewFactory();
            var session = new TeamSelectSession(new FakeRoster(), Rules(), views);
            var screen = session.Presenter;
            screen.EnterBattlePressed += team =>
            {
                screen.RememberTeam(team);
                screen.Hide();
            };
            session.Run();
            screen.Show();
            views.Last.Input.TapSlot(0);
            views.Last.Input.TapCard(Swordsman);
            views.Last.Input.TapStart();
            screen.Show(); // lost, then Play again
            CollectionAssert.AreEqual(new[] { Swordsman, Axeman, Archer }, screen.Slots.ToArray());
        }
    }
}
