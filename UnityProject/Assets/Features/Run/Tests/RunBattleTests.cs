using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace Game.Run.Tests
{
    /// <summary>
    /// IRunBattle (PROTOTYPE screen routing): the screen flow asks for a battle, the run builds it and answers once,
    /// and tells how the player left after it has cleared the battle (feature-run/run-flow/flow-rules).
    /// </summary>
    public class RunBattleTests
    {
        private static readonly RunTeam Team = new RunTeam(new[] { "knight", "axe", "archer" });

        private FakeDelay _delay;
        private RunFlow _flow;
        private IRunBattle _battle;
        private List<string> _log;

        [SetUp]
        public void SetUp()
        {
            _delay = new FakeDelay();
            _flow = new RunFlow(new RunScore(new FakeStore()), _delay, 1f, 3, reviveOnce: true, pauseEnabled: true);
            _battle = _flow;
            _log = new List<string>();
            _flow.StepChanged += s => _log.Add("step:" + s);
            _flow.RunStarted += _ => _log.Add("RunStarted");
            _flow.BattleStarted += () => _log.Add("BattleStarted");
            _flow.BattleStopped += () => _log.Add("BattleStopped");
            _flow.BattleCleared += () => _log.Add("BattleCleared");
            _battle.BattleBuilt += () => _log.Add("BattleBuilt");
            _battle.BuildFailed += why => _log.Add("BuildFailed:" + why);
            _battle.BattleLeft += choice => _log.Add("BattleLeft:" + choice);
        }

        /// <summary>The castle falls twice (revive then loss), the lose popup shows.</summary>
        private void LoseTheRun()
        {
            _flow.Lose();
            _delay.Fire();
            _flow.DeclineRevive();
            Assert.AreEqual(RunStep.Lost, _flow.Step);
        }

        [Test]
        public void BuildingABattleStartsTheRunAndAnswersBattleBuiltOnceItIsReady()
        {
            _battle.BuildBattle(Team);

            CollectionAssert.AreEqual(new[] { "step:Playing", "RunStarted", "BattleStarted", "BattleBuilt" }, _log);
            Assert.AreEqual(RunStep.Playing, _flow.Step);
            CollectionAssert.AreEqual(Team.HeroCodes, _flow.Team.HeroCodes);
        }

        [Test]
        public void BuildingABattleWorksFromTeamSelectToo()
        {
            _flow.Play();
            _log.Clear();

            _battle.BuildBattle(Team);

            Assert.AreEqual("BattleBuilt", _log[_log.Count - 1]);
        }

        [Test]
        public void TeamWithoutThreeHeroesAnswersBuildFailedWithTheReasonAndBuildsNothing()
        {
            _battle.BuildBattle(new RunTeam(new[] { "knight", "axe" }));

            Assert.AreEqual(1, _log.Count);
            StringAssert.StartsWith("BuildFailed:", _log[0]);
            StringAssert.Contains("3 heroes", _log[0]);
            Assert.AreEqual(RunStep.Home, _flow.Step);
        }

        [Test]
        public void TeamWithTheSameHeroTwiceAnswersBuildFailed()
        {
            _battle.BuildBattle(new RunTeam(new[] { "knight", "knight", "archer" }));

            StringAssert.StartsWith("BuildFailed:", _log[0]);
            Assert.AreEqual(RunStep.Home, _flow.Step);
        }

        [Test]
        public void BattleFeatureFailingWhileBuildingAnswersBuildFailedAndLeavesNothingOfTheBattle()
        {
            _flow.RunStarted += _ => throw new InvalidOperationException("no hero prefab");

            _battle.BuildBattle(Team);

            CollectionAssert.DoesNotContain(_log, "BattleBuilt");
            Assert.AreEqual(1, _log.FindAll(l => l.StartsWith("BuildFailed:")).Count, "Exactly one answer.");
            StringAssert.Contains("no hero prefab", _log.Find(l => l.StartsWith("BuildFailed:")));
            CollectionAssert.Contains(_log, "BattleCleared", "Whatever was built is cleared.");
            Assert.AreEqual(RunStep.Home, _flow.Step);
            Assert.IsFalse(_flow.IsPaused);
        }

        [Test]
        public void BuildingIsIgnoredWhileABattleRuns()
        {
            _battle.BuildBattle(Team);
            _log.Clear();

            _battle.BuildBattle(Team);

            CollectionAssert.IsEmpty(_log);
        }

        [Test]
        public void CancellingTheBuildThrowsTheBattleAwayWithNoAnswerAndNoBattleLeft()
        {
            _battle.BuildBattle(Team);
            _log.Clear();

            _battle.CancelBuild();

            CollectionAssert.AreEqual(new[] { "BattleStopped", "BattleCleared", "step:Home" }, _log);
            _battle.BuildBattle(Team);
            Assert.AreEqual(RunStep.Playing, _flow.Step, "A new battle can be built after the cancel.");
        }

        [Test]
        public void CancellingWithNoBattleDoesNothing()
        {
            _battle.CancelBuild();

            CollectionAssert.IsEmpty(_log);
        }

        [Test]
        public void PlayAgainOnTheLosePopupClearsTheBattleThenTellsTheFlow()
        {
            _battle.BuildBattle(Team);
            LoseTheRun();
            _log.Clear();

            _flow.Replay();

            var cleared = _log.IndexOf("BattleCleared");
            var left = _log.IndexOf("BattleLeft:PlayAgain");
            Assert.That(cleared, Is.GreaterThanOrEqualTo(0));
            Assert.That(left, Is.GreaterThan(cleared), "The battle is already cleared when the flow hears of it.");
            Assert.AreEqual(1, _log.FindAll(l => l.StartsWith("BattleLeft")).Count);
        }

        [Test]
        public void BackToStartOnTheLosePopupClearsTheBattleThenTellsTheFlow()
        {
            _battle.BuildBattle(Team);
            LoseTheRun();
            _log.Clear();

            _flow.GoHome();

            CollectionAssert.AreEqual(new[] { "BattleCleared", "step:Home", "BattleLeft:BackToStart" }, _log);
        }

        [Test]
        public void GivingUpOnThePausePopupSettlesClearsThenTellsTheFlow()
        {
            _battle.BuildBattle(Team);
            _flow.Pause();
            _log.Clear();

            _flow.Abandon();

            CollectionAssert.AreEqual(new[] { "BattleStopped", "BattleCleared", "step:Home", "BattleLeft:Abandon" }, _log);
        }

        [Test]
        public void PressingPlayAgainAndBackToStartTogetherTellsTheFlowOnce()
        {
            _battle.BuildBattle(Team);
            LoseTheRun();
            _log.Clear();

            _flow.Replay();
            _flow.GoHome();

            Assert.AreEqual(1, _log.FindAll(l => l.StartsWith("BattleLeft")).Count);
            CollectionAssert.Contains(_log, "BattleLeft:PlayAgain");
        }

        [Test]
        public void ANewBattleCanBeBuiltAfterLeavingTheLastOne()
        {
            _battle.BuildBattle(Team);
            LoseTheRun();
            _flow.Replay();
            _log.Clear();

            _battle.BuildBattle(Team);

            Assert.AreEqual("BattleBuilt", _log[_log.Count - 1]);
            Assert.IsFalse(_flow.ReviveOffered, "The new run has its revive again.");
        }

        private sealed class FakeDelay : IRunDelay
        {
            private Action _then;

            public IDisposable Schedule(float seconds, Action then)
            {
                _then = then;
                return new Cancel(() => _then = null);
            }

            public IDisposable NextFrame(Action then) => new Cancel(() => { });

            public void Fire()
            {
                var then = _then;
                _then = null;
                then?.Invoke();
            }

            private sealed class Cancel : IDisposable
            {
                private readonly Action _cancel;
                public Cancel(Action cancel) => _cancel = cancel;
                public void Dispose() => _cancel();
            }
        }

        private sealed class FakeStore : IBestScoreStore
        {
            public int Load() => 0;
            public void Save(int best) { }
        }
    }
}
