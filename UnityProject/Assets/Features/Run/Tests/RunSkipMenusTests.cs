using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using NUnit.Framework;

namespace Game.Run.Tests
{
    /// <summary>IDEA stage: no start screen, no team select (flow-rules "Thông số theo giai đoạn").</summary>
    public class RunSkipMenusTests
    {
        private static readonly RunTeam DefaultTeam = new RunTeam(new[] { "knight", "axeman", "archer" });

        private FakeDelay _delay;
        private RunScore _score;
        private RunFlow _flow;
        private List<string> _log;

        [SetUp]
        public void SetUp()
        {
            _delay = new FakeDelay();
            _score = new RunScore(new FakeStore());
            _flow = new RunFlow(_score, _delay, 1f, 3, skipMenus: true, defaultTeam: DefaultTeam);
            _log = new List<string>();
            _flow.StepChanged += s => _log.Add("step:" + s);
            _flow.RunStarted += t => _log.Add("RunStarted:" + string.Join(",", t.HeroCodes));
            _flow.BattleStarted += () => _log.Add("BattleStarted");
            _flow.BattleCleared += () => _log.Add("BattleCleared");
        }

        private void LoseAndWait()
        {
            _flow.Lose();
            _delay.Fire();
            _log.Clear();
        }

        [Test]
        public void GameGoesStraightIntoBattleWithTheDefaultTeam()
        {
            _flow.Begin();

            CollectionAssert.AreEqual(
                new[] { "step:Playing", "RunStarted:knight,axeman,archer", "BattleStarted" }, _log);
            Assert.AreEqual(RunStep.Playing, _flow.Step);
        }

        [Test]
        public void BeginningTwiceStartsOnlyOneBattle()
        {
            _flow.Begin();
            _log.Clear();
            _flow.Begin();
            CollectionAssert.IsEmpty(_log);
        }

        [Test]
        public void PlayAgainStartsANewBattleWithTheDefaultTeam()
        {
            _flow.Begin();
            _flow.CountClearedWave(1);
            LoseAndWait();

            _flow.Replay();

            CollectionAssert.AreEqual(
                new[] { "BattleCleared", "step:Playing", "RunStarted:knight,axeman,archer", "BattleStarted" }, _log);
            Assert.AreEqual(0, _score.Score);
        }

        [Test]
        public void ThereIsNoBackToStartOnTheLosePopup()
        {
            _flow.Begin();
            LoseAndWait();

            _flow.GoHome();
            Assert.AreEqual(RunStep.Lost, _flow.Step);
            CollectionAssert.IsEmpty(_log);
        }

        [Test]
        public void ThereIsNoGivingUp()
        {
            _flow.Begin();
            _flow.Pause();
            _flow.Abandon();
            Assert.AreEqual(RunStep.Paused, _flow.Step);
        }

        [Test]
        public void WithMenusTheGameStaysOnTheStartScreen()
        {
            var flow = new RunFlow(_score, _delay, 1f, 3);
            flow.Begin();
            Assert.AreEqual(RunStep.Home, flow.Step);
        }

        [Test]
        public void WrongDefaultTeamIsRejectedAtOnce()
        {
            var error = Assert.Throws<ArgumentException>(() =>
                new RunFlow(_score, _delay, 1f, 3, skipMenus: true, defaultTeam: new RunTeam(new[] { "knight", "knight", "archer" })));
            StringAssert.Contains("default team", error.Message);
        }

        [Test]
        public void BattleStartsOnTheFrameAfterTheSessionRuns()
        {
            var participant = new RunSession(_flow, _score, _delay);
            participant.LoadAsync(CancellationToken.None).Forget();
            participant.Run();

            // Features installed after the run get their Run call in this same frame: no RunStarted yet.
            Assert.AreEqual(RunStep.Home, _flow.Step);
            CollectionAssert.IsEmpty(_log);

            _delay.NextFrameArrives();
            Assert.AreEqual(RunStep.Playing, _flow.Step);
        }

        [Test]
        public void SessionEndingBeforeTheNextFrameStartsNoBattle()
        {
            var participant = new RunSession(_flow, _score, _delay);
            participant.LoadAsync(CancellationToken.None).Forget();
            participant.Run();
            participant.DisposeAsync().Forget();

            _delay.NextFrameArrives();
            Assert.AreEqual(RunStep.Home, _flow.Step);
        }

        private sealed class FakeDelay : IRunDelay
        {
            private Action _then;
            private Action _nextFrame;

            public IDisposable Schedule(float seconds, Action then)
            {
                _then = then;
                return new Cancel(() => _then = null);
            }

            public IDisposable NextFrame(Action then)
            {
                _nextFrame = then;
                return new Cancel(() => _nextFrame = null);
            }

            public void Fire()
            {
                var then = _then;
                _then = null;
                then?.Invoke();
            }

            public void NextFrameArrives()
            {
                var then = _nextFrame;
                _nextFrame = null;
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
