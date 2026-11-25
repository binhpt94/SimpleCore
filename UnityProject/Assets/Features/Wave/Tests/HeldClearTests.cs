using System;
using System.Collections.Generic;
using Game.Run;
using NUnit.Framework;

namespace Game.Wave.Tests
{
    /// <summary>
    /// feature-popup-revive/revive-order/order-values#held-clear: a wave cleared in the step the castle fell for the
    /// first time is held through the revive flow. Revived: the clear pays, scores and the 3 s break starts then.
    /// Run lost: the clear is dropped, no clear reward, no score.
    /// </summary>
    public class HeldClearTests
    {
        private List<string> _log;
        private FakeRun _run;
        private FakeEnemies _enemies;
        private FakeSimClock _clock;
        private IDisposable _steps;
        private WaveDirector _director;

        [SetUp]
        public void SetUp()
        {
            _log = new List<string>();
            _run = new FakeRun(_log);
            _enemies = new FakeEnemies();
            _clock = new FakeSimClock { Paused = () => _run.IsPaused };
            _director = new WaveDirector(new WaveRules(), _run, _enemies, new FakeIncome(_log), new FirstRandom());
            _director.Attach();
            _steps = _clock.OnStep(WaveParticipant.Owner, _director.Step);
            _director.WaveStarted += w => _log.Add("started " + w);
            _director.WaveCleared += w => _log.Add("cleared " + w);
        }

        [TearDown]
        public void TearDown()
        {
            _steps.Dispose();
            _director.Detach();
        }

        /// <summary>Wave 1 is all sent; its last enemy falls in the same step as the castle's first fall.</summary>
        private void LastKillInTheStepTheCastleFalls()
        {
            _run.StartBattle();
            _clock.Steps(1);
            _clock.Seconds(7.01);
            _enemies.DefeatAll();
            // The run's verdict in that step: first fall with a revive to offer, the battle stands still.
            _run.Step = RunStep.ReviveWait;
            _run.IsPaused = true;
            _log.Clear();
        }

        private void StandStillThrough(RunStep step, double seconds)
        {
            _run.Step = step;
            _clock.Seconds(seconds);
        }

        [Test]
        public void HeldClear_ClearInTheStepTheCastleFell_PaysNothingWhileTheReviveIsOffered()
        {
            LastKillInTheStepTheCastleFalls();

            StandStillThrough(RunStep.ReviveWait, 1);
            StandStillThrough(RunStep.ReviveOffer, 20);
            StandStillThrough(RunStep.Reviving, 0.5);

            Assert.That(_log, Is.Empty, "no clear reward, no score while the revive flow runs");
            Assert.That(_director.CurrentWave, Is.EqualTo(1));
            Assert.That(_director.InBreak, Is.False);
        }

        [Test]
        public void HeldClear_Revived_ThenTheClearPaysAndScores_AndTheThreeSecondBreakStarts()
        {
            LastKillInTheStepTheCastleFalls();
            StandStillThrough(RunStep.ReviveOffer, 20);
            StandStillThrough(RunStep.Reviving, 0.5);

            _run.Step = RunStep.Playing;
            _run.IsPaused = false;
            _clock.Steps(1);

            Assert.That(_log, Is.EqualTo(new[] { "coins 25", "score 1", "cleared 1" }), "the held clear counts once the battle runs again");
            Assert.That(_director.CurrentWave, Is.EqualTo(2));
            Assert.That(_director.InBreak, Is.True);

            _clock.Seconds(2.9);
            Assert.That(_log, Has.No.Member("started 2"), "the 3 s break counts from the revive");
            _clock.Seconds(0.2);
            Assert.That(_log, Has.Member("started 2"));
        }

        [Test]
        public void HeldClear_RunLostAfterTheOffer_DropsTheClear_NoClearRewardNoScore()
        {
            LastKillInTheStepTheCastleFalls();
            StandStillThrough(RunStep.ReviveOffer, 5);

            // End run or failed ad: the run is lost and the battle stops.
            _run.Step = RunStep.Lost;
            _run.StopBattle();
            _clock.Seconds(10);

            Assert.That(_log, Has.No.Member("coins 25"));
            Assert.That(_log, Has.No.Member("score 1"));
            Assert.That(_log, Has.No.Member("cleared 1"));
            Assert.That(_log, Has.No.Member("started 2"));
        }

        [Test]
        public void HeldClear_OrdinaryPauseAtTheClear_StillPaysAtOnce()
        {
            _run.StartBattle();
            _clock.Steps(1);
            _clock.Seconds(7.01);
            _enemies.DefeatAll();
            _run.Step = RunStep.Paused;
            _run.IsPaused = true;
            _clock.Steps(1);

            Assert.That(_log, Has.Member("coins 25"), "only the revive flow holds a clear, not the pause popup");
        }
    }
}
