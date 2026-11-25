using System.Threading;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using Game.Run;
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
        }

        [Test]
        public void RunCreatesScreenThatOpensOnPlayAndEndDestroysIt()
        {
            var run = new FakeRun();
            var views = new FakeViewFactory();
            var session = new TeamSelectSession(run, new FakeRoster(), Rules(), views);
            session.LoadAsync(CancellationToken.None).GetAwaiter().GetResult();
            session.Run();

            Assert.AreEqual(1, views.Created);
            Assert.IsFalse(views.Last.Visible);
            run.Play();
            Assert.IsTrue(views.Last.Visible);
            CollectionAssert.AreEqual(DefaultTeam, session.Screen.Slots.ToArray());

            views.Last.Input.TapCard(Axeman);
            views.Last.Input.TapCard(Mage);
            Assert.IsTrue(views.Last.Input.TapStart());
            CollectionAssert.AreEqual(new[] { Knight, Mage, Archer }, run.Entered[0].HeroCodes);

            session.DisposeAsync().GetAwaiter().GetResult();
            Assert.AreEqual(1, views.Destroyed);
            Assert.IsNull(session.Screen);
            run.SetStep(RunStep.TeamSelect);
            Assert.IsFalse(views.Last.Visible, "disposed screen no longer follows the run");
        }

        [Test]
        public void RememberedTeamLastsAcrossRunsOfTheSameSession()
        {
            var run = new FakeRun();
            var views = new FakeViewFactory();
            var session = new TeamSelectSession(run, new FakeRoster(), Rules(), views);
            session.Run();
            run.Play();
            views.Last.Input.TapSlot(0);
            views.Last.Input.TapCard(Swordsman);
            views.Last.Input.TapStart();
            run.Lose();
            run.Replay();
            CollectionAssert.AreEqual(new[] { Swordsman, Axeman, Archer }, session.Screen.Slots.ToArray());
        }
    }
}
