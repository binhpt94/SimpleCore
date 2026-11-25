using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace Game.Run.Tests
{
    public class RunFlowTests
    {
        private FakeDelay _delay;
        private FakeStore _store;
        private RunScore _score;
        private RunFlow _flow;
        private List<string> _log;

        private static readonly RunTeam Team = new RunTeam(new[] { "knight", "axe", "archer" });

        [SetUp]
        public void SetUp()
        {
            _delay = new FakeDelay();
            _store = new FakeStore();
            _score = new RunScore(_store);
            _flow = new RunFlow(_score, _delay, 1f, 3);
            _log = new List<string>();
            _flow.StepChanged += s => _log.Add("step:" + s);
            _flow.RunStarted += _ => _log.Add("RunStarted");
            _flow.BattleStarted += () => _log.Add("BattleStarted");
            _flow.BattleStopped += () => _log.Add("BattleStopped");
            _flow.BattleCleared += () => _log.Add("BattleCleared");
        }

        private void EnterBattle()
        {
            _flow.Play();
            _flow.EnterBattle(Team);
            _log.Clear();
        }

        /// <summary>A wave is reported cleared and the frame ends.</summary>
        private void ClearWave(int wave)
        {
            _flow.CountClearedWave(wave);
            _delay.NextFrameArrives();
        }

        [Test]
        public void GameSceneOpensOnTheStartScreen()
        {
            Assert.AreEqual(RunStep.Home, _flow.Step);
        }

        [Test]
        public void PressingPlayGoesToTeamSelect()
        {
            _flow.Play();
            Assert.AreEqual(RunStep.TeamSelect, _flow.Step);
        }

        [Test]
        public void EnteringBattleAnnouncesTheNewRunBeforeTheBattleStarts()
        {
            _flow.Play();
            _log.Clear();
            _flow.EnterBattle(Team);

            CollectionAssert.AreEqual(new[] { "step:Playing", "RunStarted", "BattleStarted" }, _log);
            Assert.AreEqual(3, _flow.Team.Count);
        }

        [Test]
        public void EnterBattleIsIgnoredOutsideTeamSelect()
        {
            _flow.EnterBattle(Team);
            Assert.AreEqual(RunStep.Home, _flow.Step);
        }

        [Test]
        public void TeamWithoutThreeHeroesCannotEnterBattle()
        {
            _flow.Play();
            Assert.Throws<ArgumentException>(() => _flow.EnterBattle(new RunTeam(new[] { "knight", "axe" })));
            Assert.AreEqual(RunStep.TeamSelect, _flow.Step);
        }

        [Test]
        public void TeamWithTheSameHeroTwiceCannotEnterBattle()
        {
            _flow.Play();
            Assert.Throws<ArgumentException>(() => _flow.EnterBattle(new RunTeam(new[] { "knight", "knight", "archer" })));
        }

        [Test]
        public void OnlyTheFirstPressCountsWhileTheScreenIsChanging()
        {
            _flow.Play();
            var battles = 0;
            _flow.RunStarted += _ =>
            {
                battles++;
                _flow.EnterBattle(Team);
            };
            _flow.EnterBattle(Team);
            Assert.AreEqual(1, battles);
        }

        [Test]
        public void PauseStandsTheBattleStillAndResumeRunsItAgain()
        {
            EnterBattle();
            _flow.Pause();
            Assert.IsTrue(_flow.IsPaused);
            Assert.AreEqual(RunStep.Paused, _flow.Step);

            _flow.Resume();
            Assert.IsFalse(_flow.IsPaused);
            Assert.AreEqual(RunStep.Playing, _flow.Step);
        }

        [Test]
        public void PausingTwiceDoesNothingMore()
        {
            EnterBattle();
            _flow.Pause();
            _log.Clear();
            _flow.Pause();
            CollectionAssert.IsEmpty(_log);
        }

        [Test]
        public void CastleDestroyedStopsTheBattleAtOnceAndShowsLosePopupAfterTheDelay()
        {
            EnterBattle();
            _flow.Lose();

            Assert.AreEqual(RunStep.Ending, _flow.Step);
            CollectionAssert.Contains(_log, "BattleStopped");
            Assert.AreEqual(1f, _delay.LastSeconds);

            _delay.Fire();
            Assert.AreEqual(RunStep.Lost, _flow.Step);
        }

        [Test]
        public void SecondCastleDestroyedReportIsIgnored()
        {
            EnterBattle();
            _flow.Lose();
            _log.Clear();
            _flow.Lose();
            CollectionAssert.IsEmpty(_log);
            Assert.AreEqual(1, _delay.Scheduled);
        }

        [Test]
        public void NothingCanBePressedDuringTheWaitBeforeTheLosePopup()
        {
            EnterBattle();
            _flow.Lose();
            _flow.Pause();
            _flow.Replay();
            _flow.GoHome();
            Assert.AreEqual(RunStep.Ending, _flow.Step);
        }

        [Test]
        public void PlayAgainClearsTheBattleAndGoesToTeamSelect()
        {
            EnterBattle();
            _flow.Lose();
            _delay.Fire();
            _log.Clear();

            _flow.Replay();
            CollectionAssert.AreEqual(new[] { "BattleCleared", "step:TeamSelect" }, _log);
        }

        [Test]
        public void BackToStartClearsTheBattleAndGoesHome()
        {
            EnterBattle();
            _flow.Lose();
            _delay.Fire();
            _log.Clear();

            _flow.GoHome();
            CollectionAssert.AreEqual(new[] { "BattleCleared", "step:Home" }, _log);
        }

        [Test]
        public void GivingUpStopsAndClearsTheBattleAndGoesHomeWithoutLosePopup()
        {
            EnterBattle();
            _flow.Pause();
            _log.Clear();

            _flow.Abandon();
            CollectionAssert.AreEqual(new[] { "BattleStopped", "BattleCleared", "step:Home" }, _log);
            Assert.AreEqual(0, _delay.Scheduled);
        }

        [Test]
        public void GivingUpIsOnlyPossibleWhilePaused()
        {
            EnterBattle();
            _flow.Abandon();
            Assert.AreEqual(RunStep.Playing, _flow.Step);
        }

        [Test]
        public void EachClearedWaveAddsOnePoint()
        {
            EnterBattle();
            ClearWave(1);
            ClearWave(2);
            ClearWave(3);
            _flow.Lose();
            Assert.AreEqual(3, _score.Score);
        }

        [Test]
        public void SameWaveReportedTwiceCountsOnce()
        {
            EnterBattle();
            ClearWave(1);
            ClearWave(1);
            Assert.AreEqual(1, _score.Score);
        }

        [Test]
        public void WaveClearedAfterTheCastleFellDoesNotCount()
        {
            EnterBattle();
            ClearWave(1);
            _flow.Lose();
            ClearWave(2);
            Assert.AreEqual(1, _score.Score);
        }

        [Test]
        public void WaveClearedInTheSameFrameJustBeforeTheCastleFallsDoesNotCount()
        {
            EnterBattle();
            ClearWave(1);

            _flow.CountClearedWave(2);
            _flow.Lose();
            _delay.NextFrameArrives();

            Assert.AreEqual(1, _score.Score);
            Assert.AreEqual(1, _score.Best);
        }

        [Test]
        public void WaveClearedCountsOnceTheFrameEndsWithoutTheCastleFalling()
        {
            EnterBattle();
            _flow.CountClearedWave(1);
            Assert.AreEqual(0, _score.Score);

            _delay.NextFrameArrives();
            Assert.AreEqual(1, _score.Score);
        }

        [Test]
        public void SessionEndDropsAWaveNotCountedYet()
        {
            EnterBattle();
            _flow.CountClearedWave(1);
            _flow.Shutdown();
            _delay.NextFrameArrives();
            Assert.AreEqual(0, _score.Score);
        }

        [Test]
        public void NewRunStartsTheScoreAtZero()
        {
            EnterBattle();
            ClearWave(1);
            _flow.Lose();
            _delay.Fire();
            _flow.Replay();
            _flow.EnterBattle(Team);
            Assert.AreEqual(0, _score.Score);
            Assert.IsFalse(_score.IsNewBest);
        }

        [Test]
        public void HigherScoreBecomesTheBestAndIsSavedBeforeTheLosePopup()
        {
            EnterBattle();
            ClearWave(1);
            ClearWave(2);
            _flow.Lose();

            Assert.AreEqual(2, _score.Best);
            Assert.IsTrue(_score.IsNewBest);
            Assert.AreEqual(2, _store.Saved);
            Assert.AreEqual(RunStep.Ending, _flow.Step);
        }

        [Test]
        public void EqualScoreIsNotANewBest()
        {
            _store.Value = 2;
            _score.LoadBest();
            EnterBattle();
            ClearWave(1);
            ClearWave(2);
            _flow.Lose();

            Assert.AreEqual(2, _score.Best);
            Assert.IsFalse(_score.IsNewBest);
            Assert.IsNull(_store.Saved);
        }

        [Test]
        public void LosingInTheFirstWaveScoresZeroAndKeepsTheBest()
        {
            _store.Value = 4;
            _score.LoadBest();
            EnterBattle();
            _flow.Lose();
            Assert.AreEqual(0, _score.Score);
            Assert.AreEqual(4, _score.Best);
            Assert.IsFalse(_score.IsNewBest);
        }

        [Test]
        public void GivingUpSettlesTheScoreLikeLosing()
        {
            EnterBattle();
            ClearWave(1);
            _flow.Pause();
            _flow.Abandon();
            Assert.AreEqual(1, _score.Best);
            Assert.IsTrue(_score.IsNewBest);
        }

        [Test]
        public void BrokenSavedBestCountsAsZero()
        {
            _store.Value = -5;
            _score.LoadBest();
            Assert.AreEqual(0, _score.Best);

            _store.ThrowOnLoad = true;
            _score.LoadBest();
            Assert.AreEqual(0, _score.Best);
        }

        [Test]
        public void FailedSaveKeepsTheBestForThisSession()
        {
            _store.ThrowOnSave = true;
            EnterBattle();
            ClearWave(1);
            Assert.DoesNotThrow(() => _flow.Lose());
            Assert.AreEqual(1, _score.Best);
        }

        [Test]
        public void NoSavedBestCountsAsZero()
        {
            _store.Value = 0;
            _score.LoadBest();
            Assert.AreEqual(0, _score.Best);
        }

        [Test]
        public void ScoreBestAndNewBestCanBeReadAtAnyTime()
        {
            Assert.AreEqual(0, _score.Score);
            Assert.AreEqual(0, _score.Best);
            Assert.IsFalse(_score.IsNewBest);

            _store.Value = 3;
            _score.LoadBest();
            Assert.AreEqual(3, _score.Best);

            EnterBattle();
            ClearWave(1);
            Assert.AreEqual(1, _score.Score);
        }

        [Test]
        public void GivingUpRightAfterEnteringBattleScoresZeroAndKeepsTheBest()
        {
            _store.Value = 2;
            _score.LoadBest();
            EnterBattle();
            _flow.Pause();
            _flow.Abandon();

            Assert.AreEqual(0, _score.Score);
            Assert.AreEqual(2, _score.Best);
            Assert.IsFalse(_score.IsNewBest);
            Assert.IsNull(_store.Saved);
        }

        [Test]
        public void PressingPlayAgainAndBackToStartTogetherOnlyTheFirstCounts()
        {
            EnterBattle();
            _flow.Lose();
            _delay.Fire();
            _log.Clear();

            _flow.Replay();
            _flow.GoHome();

            CollectionAssert.AreEqual(new[] { "BattleCleared", "step:TeamSelect" }, _log);
            Assert.AreEqual(RunStep.TeamSelect, _flow.Step);
        }

        [Test]
        public void SessionEndDropsThePendingLosePopup()
        {
            EnterBattle();
            _flow.Lose();
            _flow.Shutdown();
            Assert.IsTrue(_delay.Cancelled);
        }

        private sealed class FakeDelay : IRunDelay
        {
            private Action _then;

            public int Scheduled { get; private set; }
            public float LastSeconds { get; private set; }
            public bool Cancelled { get; private set; }

            public IDisposable Schedule(float seconds, Action then)
            {
                Scheduled++;
                LastSeconds = seconds;
                _then = then;
                return new Cancel(this);
            }

            private Action _nextFrame;

            public IDisposable NextFrame(Action then)
            {
                _nextFrame = then;
                return new CancelNextFrame(this);
            }

            public void NextFrameArrives()
            {
                var then = _nextFrame;
                _nextFrame = null;
                then?.Invoke();
            }

            private sealed class CancelNextFrame : IDisposable
            {
                private readonly FakeDelay _owner;
                public CancelNextFrame(FakeDelay owner) => _owner = owner;
                public void Dispose() => _owner._nextFrame = null;
            }

            public void Fire()
            {
                var then = _then;
                _then = null;
                then?.Invoke();
            }

            private sealed class Cancel : IDisposable
            {
                private readonly FakeDelay _owner;
                public Cancel(FakeDelay owner) => _owner = owner;

                public void Dispose()
                {
                    _owner.Cancelled = true;
                    _owner._then = null;
                }
            }
        }

        private sealed class FakeStore : IBestScoreStore
        {
            public int Value;
            public int? Saved;
            public bool ThrowOnLoad;
            public bool ThrowOnSave;

            public int Load()
            {
                if (ThrowOnLoad) throw new InvalidOperationException("unreadable");
                return Value;
            }

            public void Save(int best)
            {
                if (ThrowOnSave) throw new InvalidOperationException("disk full");
                Saved = best;
            }
        }
    }
}
