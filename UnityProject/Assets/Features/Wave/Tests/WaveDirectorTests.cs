using System;
using System.Collections.Generic;
using Game.Enemy;
using NUnit.Framework;

namespace Game.Wave.Tests
{
    /// <summary>
    /// The director driven by a hand-stepped shared clock (0.025 s steps); a step is gameplay unless the test says otherwise.
    /// </summary>
    public class WaveDirectorTests
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
            _clock = new FakeSimClock();
            Make(new WaveRules());
            _director.WaveStarted += w => _log.Add("started " + w);
            _director.WaveCleared += w => _log.Add("cleared " + w);
            _director.CurrentWaveChanged += w => _log.Add("current " + w);
        }

        [TearDown]
        public void TearDown()
        {
            _steps.Dispose();
            _director.Detach();
        }

        private void Make(WaveRules rules)
        {
            if (_director != null)
            {
                _steps.Dispose();
                _director.Detach();
            }
            _director = new WaveDirector(rules, _run, _enemies, new FakeIncome(_log), new FirstRandom());
            _director.Attach();
            _steps = _clock.OnStep(WaveParticipant.Owner, _director.Step);
        }

        /// <summary>The battle starts and the first gameplay step runs: wave 1 begins in it.</summary>
        private void Begin()
        {
            _run.StartBattle();
            _clock.Steps(1);
        }

        private void Wait(double seconds) => _clock.Seconds(seconds);

        private void Step() => _clock.Steps(1);

        /// <summary>Lets wave 1 send all 8 enemies (7 s) and defeats them, then settles the clear on the next step.</summary>
        private void ClearWaveOne()
        {
            Wait(7.01);
            _enemies.DefeatAll();
            Step();
        }

        [Test]
        public void BattleStart_StartsWaveOne_WithTheFirstEnemyAtOnce()
        {
            _run.StartBattle();
            Assert.That(_enemies.Spawned, Is.Empty, "nothing is sent outside a step of the shared clock");

            Step();

            Assert.That(_log, Is.EqualTo(new[] { "current 1", "started 1" }));
            Assert.That(_director.CurrentWave, Is.EqualTo(1));
            Assert.That(_enemies.Spawned.Count, Is.EqualTo(1), "the first enemy comes on the first step of the battle");
        }

        [Test]
        public void Enemies_ComeOneByOne_OnePerSecondInWaveOne()
        {
            Begin();

            Wait(0.99);
            Assert.That(_enemies.Spawned.Count, Is.EqualTo(1));
            Wait(0.02);
            Assert.That(_enemies.Spawned.Count, Is.EqualTo(2), "40 steps of 0.025 s after the first");
            Wait(6);
            Assert.That(_enemies.Spawned.Count, Is.EqualTo(8));
            Wait(5);
            Assert.That(_enemies.Spawned.Count, Is.EqualTo(8), "wave 1 has 8 enemies");
        }

        [Test]
        public void SpawnedEnemies_CarryTheWaveStatScale()
        {
            Begin();
            ClearWaveOne();
            Wait(3.01);

            Assert.That(_enemies.Spawned[_enemies.Spawned.Count - 1].StatScale, Is.EqualTo(1.1f).Within(1e-4));
        }

        [Test]
        public void ClearingAWave_PaysThenScoresThenTells_ThenMovesToTheNextWave()
        {
            Begin();
            _log.Clear();

            ClearWaveOne();

            Assert.That(_log, Is.EqualTo(new[] { "coins 25", "score 1", "cleared 1", "current 2" }));
            Assert.That(_director.CurrentWave, Is.EqualTo(2));
            Assert.That(_director.InBreak, Is.True);
        }

        [Test]
        public void LastEnemyDefeatedBeforeAllCame_DoesNotClearTheWave()
        {
            Begin();
            Wait(2.5);
            _enemies.DefeatAll();
            Step();

            Assert.That(_log, Has.No.Member("cleared 1"));
        }

        [Test]
        public void AfterThreeSecondsOfBreak_TheNextWaveStartsWithAnEnemyAtOnce()
        {
            Begin();
            ClearWaveOne();
            var before = _enemies.Spawned.Count;

            Wait(2.9);
            Assert.That(_log, Has.No.Member("started 2"));
            Assert.That(_enemies.Spawned.Count, Is.EqualTo(before), "no enemy during the break");

            Wait(0.2);
            Assert.That(_log, Has.Member("started 2"));
            Assert.That(_enemies.Spawned.Count, Is.EqualTo(before + 1));
        }

        [Test]
        public void CastleFallingInTheSameStepAsTheLastKill_LosesTheClear()
        {
            Begin();
            Wait(7.01);
            _enemies.DefeatAll();
            _run.Lose();
            Step();

            Assert.That(_log, Has.No.Member("coins 25"));
            Assert.That(_log, Has.No.Member("score 1"));
            Assert.That(_log, Has.No.Member("cleared 1"));
        }

        [Test]
        public void BattleStopped_StopsSendingEnemies()
        {
            Begin();
            Wait(2.5);
            var sent = _enemies.Spawned.Count;

            _run.StopBattle();
            Wait(10);

            Assert.That(_enemies.Spawned.Count, Is.EqualTo(sent));
        }

        [Test]
        public void FortyEnemiesAlive_HoldsTheNextOne_UntilOneFalls()
        {
            Make(new WaveRules { AliveCap = 3 });
            Begin();

            Wait(10);
            Assert.That(_enemies.Spawned.Count, Is.EqualTo(3));

            _enemies.Defeat(1);
            Step();
            Assert.That(_enemies.Spawned.Count, Is.EqualTo(4), "the held enemy comes on the next step");

            Wait(0.98);
            Assert.That(_enemies.Spawned.Count, Is.EqualTo(4), "the clock counts a full interval again");
        }

        [Test]
        public void CheatMidWave_StartsTheChosenWaveAtOnce_WithoutRewardOrScore()
        {
            Begin();
            Wait(2.5);
            _log.Clear();

            _director.StartWave(7);

            Assert.That(_log, Is.EqualTo(new[] { "current 7", "started 7" }));
            Assert.That(_director.CurrentWave, Is.EqualTo(7));
            Assert.That(_enemies.Spawned.Count, Is.EqualTo(4), "3 of wave 1, then the first of wave 7 at once");
            Assert.That(_enemies.Spawned[3].StatScale, Is.EqualTo(1.6f).Within(1e-4));
        }

        [Test]
        public void CheatMidWave_EnemiesLeftOnTheField_MustFallBeforeTheNewWaveClears()
        {
            Begin();
            Wait(2.5);
            var leftOver = _enemies.AliveCount;
            _director.StartWave(2);
            Wait(20);
            var sentInWaveTwo = _enemies.Spawned.Count - leftOver;

            _enemies.Defeat(sentInWaveTwo);
            Step();
            Assert.That(_log, Has.No.Member("cleared 2"), "the left-over enemies are still on the field");

            _enemies.Defeat(leftOver);
            Step();
            Assert.That(_log, Has.Member("cleared 2"));
            Assert.That(_log, Has.Member("coins 30"));
            Assert.That(_log, Has.No.Member("cleared 1"));
        }

        [Test]
        public void CheatDuringTheBreak_StartsTheChosenWaveAtOnce_AndEndsTheBreak()
        {
            Begin();
            ClearWaveOne();
            Wait(1);
            var before = _enemies.Spawned.Count;
            _log.Clear();

            _director.StartWave(5);
            Wait(3);

            Assert.That(_log, Is.EqualTo(new[] { "current 5", "started 5" }));
            Assert.That(_director.InBreak, Is.False);
            Assert.That(_enemies.Spawned.Count, Is.GreaterThan(before));
        }

        [Test]
        public void CheatOutsideARunningBattle_DoesNothing()
        {
            _director.StartWave(3);
            Assert.That(_enemies.Spawned, Is.Empty);
            Assert.That(_log, Is.Empty);

            Begin();
            _run.StopBattle();
            var sent = _enemies.Spawned.Count;
            _log.Clear();

            _director.StartWave(3);

            Assert.That(_enemies.Spawned.Count, Is.EqualTo(sent));
            Assert.That(_log, Is.Empty);
        }

        [Test]
        public void WaveOne_SendsHalfOfItsEnemiesToEachSide()
        {
            Begin();
            Wait(7.01);

            Assert.That(_enemies.Spawned.Count, Is.EqualTo(8));
            Assert.That(_enemies.Spawned.FindAll(s => s.Side == EnemySide.Left).Count, Is.EqualTo(4));
            Assert.That(_enemies.Spawned.FindAll(s => s.Side == EnemySide.Right).Count, Is.EqualTo(4));
        }

        [Test]
        public void CastleFallingDuringTheBreak_KeepsTheClearedWave_AndStartsNoNewWave()
        {
            Begin();
            ClearWaveOne();
            Wait(1);
            var sent = _enemies.Spawned.Count;

            _run.Lose();
            Wait(10);

            Assert.That(_log, Has.Member("coins 25"));
            Assert.That(_log, Has.Member("score 1"));
            Assert.That(_log, Has.No.Member("started 2"));
            Assert.That(_enemies.Spawned.Count, Is.EqualTo(sent));
        }

        [Test]
        public void CastleFallingWhileHeldAtTheCap_DropsTheWaitingEnemies_AndNeverClears()
        {
            Make(new WaveRules { AliveCap = 3 });
            _director.WaveCleared += w => _log.Add("cleared " + w);
            Begin();
            Wait(10);
            Assert.That(_enemies.Spawned.Count, Is.EqualTo(3));

            _run.Lose();
            _enemies.DefeatAll();
            Wait(10);

            Assert.That(_enemies.Spawned.Count, Is.EqualTo(3), "the 5 waiting enemies are dropped");
            Assert.That(_log, Has.No.Member("cleared 1"));
        }

        [Test]
        public void Boss_WaitsUnderTheAliveCapLikeEveryEnemy_AndStillComesLast()
        {
            // Wave 10 has 25 enemies then the Boss; with the wave-1 enemy left on the field a cap of 26 is reached
            // just before the Boss.
            Make(new WaveRules { AliveCap = 26 });
            Begin();
            _director.StartWave(10);

            Wait(100);
            Assert.That(_enemies.Spawned.Count, Is.EqualTo(26));
            Assert.That(_enemies.Spawned.Exists(s => s.Type == EnemyType.Boss), Is.False, "the Boss waits at the cap");

            _enemies.Defeat(1);
            Step();
            Assert.That(_enemies.Spawned.Count, Is.EqualTo(27));
            Assert.That(_enemies.Spawned[26].Type, Is.EqualTo(EnemyType.Boss));
        }

        [Test]
        public void ClearingWaveOne_MakesTheWaveNumberTwoAtOnce_AndKeepsItThroughTheBreak()
        {
            Begin();
            Wait(7.01);
            _enemies.DefeatAll();
            _log.Clear();

            Step();
            Assert.That(_director.CurrentWave, Is.EqualTo(2), "the number moves on in the step of the clear");
            Assert.That(_log, Has.Member("current 2"));

            _log.Clear();
            Wait(1.5);
            Wait(1.4);
            Assert.That(_director.InBreak, Is.True);
            Assert.That(_director.CurrentWave, Is.EqualTo(2), "during the break the number is the upcoming wave");
            Assert.That(_log, Is.Empty, "no number change while breathing");
        }

        [Test]
        public void BreakEnd_StartsTheUpcomingWave_WithoutChangingTheWaveNumberAgain()
        {
            Begin();
            ClearWaveOne();
            _log.Clear();

            Wait(3.01);

            Assert.That(_log, Is.EqualTo(new[] { "started 2" }), "only the start of wave 2, no second number change");
            Assert.That(_director.CurrentWave, Is.EqualTo(2));
            Assert.That(_enemies.Spawned[_enemies.Spawned.Count - 1].StatScale, Is.EqualTo(1.1f).Within(1e-4),
                "the first enemy of wave 2 comes at once with the wave-2 scale");
        }

        [Test]
        public void NextWave_StartsOnTime_WithoutWaitingForAnythingElse()
        {
            // Wave never waits for heroes coming back or upgrades: only the 3 s clock starts the next wave.
            Begin();
            ClearWaveOne();

            Wait(3.01);

            Assert.That(_log, Has.Member("started 2"));
            Assert.That(_director.CurrentWave, Is.EqualTo(2));
        }

        [Test]
        public void NewRun_StartsAgainFromWaveOne()
        {
            Begin();
            ClearWaveOne();
            _run.StopBattle();
            _log.Clear();

            Begin();

            Assert.That(_director.CurrentWave, Is.EqualTo(1));
            Assert.That(_log, Is.EqualTo(new[] { "current 1", "started 1" }));
        }

        [Test]
        public void WaitsCountGameplaySteps_TheIntervalIsExactlyFortyStepsInWaveOne()
        {
            Begin();

            _clock.Steps(39);
            Assert.That(_enemies.Spawned.Count, Is.EqualTo(1));
            _clock.Steps(1);
            Assert.That(_enemies.Spawned.Count, Is.EqualTo(2), "1 s at 40 steps a second");
        }

        [Test]
        public void StepsThatAreNotGameplay_DoNotStartTheFirstWave()
        {
            _clock.Gameplay = () => false;
            _run.StartBattle();
            _clock.Steps(100);
            Assert.That(_enemies.Spawned, Is.Empty, "the first wave waits for the first gameplay step");

            _clock.Gameplay = () => true;
            Step();
            Assert.That(_enemies.Spawned.Count, Is.EqualTo(1));
        }

        [Test]
        public void DefeatsOfAStep_AreSeenByTheNextStep_TheHeldEnemyComesThen()
        {
            // spawn-after-results: Wave runs first in a step, so a kill made later in step k frees a place in step k+1.
            Make(new WaveRules { AliveCap = 1 });
            Begin();
            Wait(2);
            Assert.That(_enemies.Spawned.Count, Is.EqualTo(1), "held at the cap");

            _enemies.Defeat(1);
            Assert.That(_enemies.Spawned.Count, Is.EqualTo(1), "nothing comes in the middle of the step");
            Step();
            Assert.That(_enemies.Spawned.Count, Is.EqualTo(2));
        }
    }
}
