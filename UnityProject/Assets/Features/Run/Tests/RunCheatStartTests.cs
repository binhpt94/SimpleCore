#if UNITY_EDITOR || DEVELOPMENT_BUILD
using NUnit.Framework;

namespace Game.Run.Tests
{
    // Bug b20261127-ab7a: a cheat state shown in battle asks the run's IRunCheatStart for a battle, so the scene opens
    // straight in it instead of on the start screen.
    public class RunCheatStartTests
    {
        private static readonly RunTeam Default = new RunTeam(new[] { "knight", "axeman", "archer" });

        [Test]
        public void NoCheatAskedMeansNoBattleRequested()
        {
            var start = new RunCheatStart(Default);

            Assert.IsFalse(start.BattleRequested);
        }

        [Test]
        public void CheatAskingWithoutATeamGetsTheRunsDefaultTeam()
        {
            var start = new RunCheatStart(Default);

            start.RequestBattle(default);

            Assert.IsTrue(start.BattleRequested);
            Assert.That(start.Team.HeroCodes, Is.EqualTo(Default.HeroCodes));
        }

        [Test]
        public void CheatAskingWithATeamGetsThatTeamEvenIfAnotherAsksWithoutOne()
        {
            var start = new RunCheatStart(Default);
            var wanted = new RunTeam(new[] { "archer", "knight", "axeman" });

            start.RequestBattle(wanted);
            start.RequestBattle(default);

            Assert.That(start.Team.HeroCodes, Is.EqualTo(wanted.HeroCodes));
        }
    }
}
#endif
