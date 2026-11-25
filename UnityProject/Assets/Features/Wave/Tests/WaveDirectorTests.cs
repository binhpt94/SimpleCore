using System.Collections.Generic;
using Game.Enemy;
using NUnit.Framework;

namespace Game.Wave.Tests
{
    public class WaveDirectorTests
    {
        private List<string> _log;
        private FakeRun _run;
        private FakeEnemies _enemies;
        private WaveDirector _director;

        [SetUp]
        public void SetUp()
        {
            _log = new List<string>();
            _run = new FakeRun(_log);
            _enemies = new FakeEnemies();
            _director = new WaveDirector(new WaveRules(), _run, _enemies, new FakeIncome(_log), new FirstRandom());
            _director.Attach();
            _director.WaveStarted += w => _log.Add("started " + w);
            _director.WaveCleared += w => _log.Add("cleared " + w);
            _director.CurrentWaveChanged += w => _log.Add("current " + w);
        }

        [TearDown]
        public void TearDown() => _director.Detach();

        /// <summary>Lets wave 1 send all 8 enemies (7 s) and defeats them, then settles the clear.</summary>
        private void ClearWaveOne()
        {
            _director.Tick(7.01f);
            _enemies.DefeatAll();
            _director.Tick(0.01f);
        }

        [Test]
        public void BattleStart_StartsWaveOne_WithTheFirstEnemyAtOnce()
        {
            _run.StartBattle();

            Assert.That(_log, Is.EqualTo(new[] { "current 1", "started 1" }));
            Assert.That(_director.CurrentWave, Is.EqualTo(1));
            Assert.That(_enemies.Spawned.Count, Is.EqualTo(1));
        }

        [Test]
        public void Enemies_ComeOneByOne_OnePerSecondInWaveOne()
        {
            _run.StartBattle();

            _director.Tick(0.99f);
            Assert.That(_enemies.Spawned.Count, Is.EqualTo(1));
            _director.Tick(0.02f);
            Assert.That(_enemies.Spawned.Count, Is.EqualTo(2));
            _director.Tick(6f);
            Assert.That(_enemies.Spawned.Count, Is.EqualTo(8));
            _director.Tick(5f);
            Assert.That(_enemies.Spawned.Count, Is.EqualTo(8), "wave 1 has 8 enemies");
        }

        [Test]
        public void SpawnedEnemies_CarryTheWaveStatScale()
        {
            _run.StartBattle();
            ClearWaveOne();
            _director.Tick(3.01f);

            Assert.That(_enemies.Spawned[_enemies.Spawned.Count - 1].StatScale, Is.EqualTo(1.1f).Within(1e-4));
        }

        [Test]
        public void ClearingAWave_PaysThenScoresThenTells_ThenMovesToTheNextWave()
        {
            _run.StartBattle();
            _log.Clear();

            ClearWaveOne();

            Assert.That(_log, Is.EqualTo(new[] { "coins 25", "score 1", "cleared 1", "current 2" }));
            Assert.That(_director.CurrentWave, Is.EqualTo(2));
            Assert.That(_director.InBreak, Is.True);
        }

        [Test]
        public void LastEnemyDefeatedBeforeAllCame_DoesNotClearTheWave()
        {
            _run.StartBattle();
            _director.Tick(2.5f);
            _enemies.DefeatAll();
            _director.Tick(0.01f);

            Assert.That(_log, Has.No.Member("cleared 1"));
        }

        [Test]
        public void AfterThreeSecondsOfBreak_TheNextWaveStartsWithAnEnemyAtOnce()
        {
            _run.StartBattle();
            ClearWaveOne();
            var before = _enemies.Spawned.Count;

            _director.Tick(2.9f);
            Assert.That(_log, Has.No.Member("started 2"));
            Assert.That(_enemies.Spawned.Count, Is.EqualTo(before), "no enemy during the break");

            _director.Tick(0.2f);
            Assert.That(_log, Has.Member("started 2"));
            Assert.That(_enemies.Spawned.Count, Is.EqualTo(before + 1));
        }

        [Test]
        public void CastleFallingInTheSameStepAsTheLastKill_LosesTheClear()
        {
            _run.StartBattle();
            _director.Tick(7.01f);
            _enemies.DefeatAll();
            _run.Lose();
            _director.Tick(0.01f);

            Assert.That(_log, Has.No.Member("coins 25"));
            Assert.That(_log, Has.No.Member("score 1"));
            Assert.That(_log, Has.No.Member("cleared 1"));
        }

        [Test]
        public void BattleStopped_StopsSendingEnemies()
        {
            _run.StartBattle();
            _director.Tick(2.5f);
            var sent = _enemies.Spawned.Count;

            _run.StopBattle();
            _director.Tick(10f);

            Assert.That(_enemies.Spawned.Count, Is.EqualTo(sent));
        }

        [Test]
        public void Pause_FreezesSendingAndTheBreak()
        {
            _run.StartBattle();
            _run.IsPaused = true;
            _director.Tick(10f);
            Assert.That(_enemies.Spawned.Count, Is.EqualTo(1));

            _run.IsPaused = false;
            ClearWaveOne();
            _run.IsPaused = true;
            _director.Tick(10f);
            Assert.That(_log, Has.No.Member("started 2"));

            _run.IsPaused = false;
            _director.Tick(3.01f);
            Assert.That(_log, Has.Member("started 2"));
        }

        [Test]
        public void FortyEnemiesAlive_HoldsTheNextOne_UntilOneFalls()
        {
            var rules = new WaveRules { AliveCap = 3 };
            _director.Detach();
            _director = new WaveDirector(rules, _run, _enemies, new FakeIncome(_log), new FirstRandom());
            _director.Attach();
            _run.StartBattle();

            _director.Tick(10f);
            Assert.That(_enemies.Spawned.Count, Is.EqualTo(3));

            _enemies.Defeat(1);
            _director.Tick(0.01f);
            Assert.That(_enemies.Spawned.Count, Is.EqualTo(4), "the held enemy comes at once");

            _director.Tick(0.98f);
            Assert.That(_enemies.Spawned.Count, Is.EqualTo(4), "the clock counts a full interval again");
        }

        [Test]
        public void CheatMidWave_StartsTheChosenWaveAtOnce_WithoutRewardOrScore()
        {
            _run.StartBattle();
            _director.Tick(2.5f);
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
            _run.StartBattle();
            _director.Tick(2.5f);
            var leftOver = _enemies.AliveCount;
            _director.StartWave(2);
            _director.Tick(20f);
            var sentInWaveTwo = _enemies.Spawned.Count - leftOver;

            _enemies.Defeat(sentInWaveTwo);
            _director.Tick(0.01f);
            Assert.That(_log, Has.No.Member("cleared 2"), "the left-over enemies are still on the field");

            _enemies.Defeat(leftOver);
            _director.Tick(0.01f);
            Assert.That(_log, Has.Member("cleared 2"));
            Assert.That(_log, Has.Member("coins 30"));
            Assert.That(_log, Has.No.Member("cleared 1"));
        }

        [Test]
        public void CheatDuringTheBreak_StartsTheChosenWaveAtOnce_AndEndsTheBreak()
        {
            _run.StartBattle();
            ClearWaveOne();
            _director.Tick(1f);
            var before = _enemies.Spawned.Count;
            _log.Clear();

            _director.StartWave(5);
            _director.Tick(3f);

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

            _run.StartBattle();
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
            _run.StartBattle();
            _director.Tick(7.01f);

            Assert.That(_enemies.Spawned.Count, Is.EqualTo(8));
            Assert.That(_enemies.Spawned.FindAll(s => s.Side == EnemySide.Left).Count, Is.EqualTo(4));
            Assert.That(_enemies.Spawned.FindAll(s => s.Side == EnemySide.Right).Count, Is.EqualTo(4));
        }

        [Test]
        public void PauseMidInterval_ResumesTheRestOfTheInterval_WithoutCatchingUp()
        {
            _run.StartBattle();
            _director.Tick(0.4f);
            _run.IsPaused = true;
            _director.Tick(10f);
            _run.IsPaused = false;

            _director.Tick(0.59f);
            Assert.That(_enemies.Spawned.Count, Is.EqualTo(1), "0.6 s of the interval were still left");
            _director.Tick(0.02f);
            Assert.That(_enemies.Spawned.Count, Is.EqualTo(2), "one enemy, no make-up for the paused time");
        }

        [Test]
        public void PauseDuringTheBreak_KeepsTheRestOfTheThreeSeconds()
        {
            _run.StartBattle();
            ClearWaveOne();
            _director.Tick(1.2f);
            _run.IsPaused = true;
            _director.Tick(10f);
            _run.IsPaused = false;

            _director.Tick(1.75f);
            Assert.That(_log, Has.No.Member("started 2"), "1.8 s were left when paused at 1.2 s");
            _director.Tick(0.1f);
            Assert.That(_log, Has.Member("started 2"));
        }

        [Test]
        public void PauseRightAtTheClear_PaysAndScoresAtOnce_TheBreakCountsAfterResume()
        {
            _run.StartBattle();
            _director.Tick(7.01f);
            _enemies.DefeatAll();
            _run.IsPaused = true;
            _director.Tick(0.01f);

            Assert.That(_log, Has.Member("coins 25"));
            Assert.That(_log, Has.Member("score 1"));
            _director.Tick(10f);
            Assert.That(_log, Has.No.Member("started 2"), "the break does not count while paused");

            _run.IsPaused = false;
            _director.Tick(3.01f);
            Assert.That(_log, Has.Member("started 2"));
        }

        [Test]
        public void CastleFallingDuringTheBreak_KeepsTheClearedWave_AndStartsNoNewWave()
        {
            _run.StartBattle();
            ClearWaveOne();
            _director.Tick(1f);
            var sent = _enemies.Spawned.Count;

            _run.Lose();
            _director.Tick(10f);

            Assert.That(_log, Has.Member("coins 25"));
            Assert.That(_log, Has.Member("score 1"));
            Assert.That(_log, Has.No.Member("started 2"));
            Assert.That(_enemies.Spawned.Count, Is.EqualTo(sent));
        }

        [Test]
        public void CastleFallingWhileHeldAtTheCap_DropsTheWaitingEnemies_AndNeverClears()
        {
            var rules = new WaveRules { AliveCap = 3 };
            _director.Detach();
            _director = new WaveDirector(rules, _run, _enemies, new FakeIncome(_log), new FirstRandom());
            _director.Attach();
            _director.WaveCleared += w => _log.Add("cleared " + w);
            _run.StartBattle();
            _director.Tick(10f);
            Assert.That(_enemies.Spawned.Count, Is.EqualTo(3));

            _run.Lose();
            _enemies.DefeatAll();
            _director.Tick(10f);

            Assert.That(_enemies.Spawned.Count, Is.EqualTo(3), "the 5 waiting enemies are dropped");
            Assert.That(_log, Has.No.Member("cleared 1"));
        }

        [Test]
        public void Boss_WaitsUnderTheAliveCapLikeEveryEnemy_AndStillComesLast()
        {
            // Wave 10 has 25 enemies then the Boss; with the wave-1 enemy left on the field a cap of 26 is reached
            // just before the Boss.
            var rules = new WaveRules { AliveCap = 26 };
            _director.Detach();
            _director = new WaveDirector(rules, _run, _enemies, new FakeIncome(_log), new FirstRandom());
            _director.Attach();
            _run.StartBattle();
            _director.StartWave(10);

            _director.Tick(100f);
            Assert.That(_enemies.Spawned.Count, Is.EqualTo(26));
            Assert.That(_enemies.Spawned.Exists(s => s.Type == EnemyType.Boss), Is.False, "the Boss waits at the cap");

            _enemies.Defeat(1);
            _director.Tick(0.01f);
            Assert.That(_enemies.Spawned.Count, Is.EqualTo(27));
            Assert.That(_enemies.Spawned[26].Type, Is.EqualTo(EnemyType.Boss));
        }

        [Test]
        public void ClearingWaveOne_MakesTheWaveNumberTwoAtOnce_AndKeepsItThroughTheBreak()
        {
            _run.StartBattle();
            _director.Tick(7.01f);
            _enemies.DefeatAll();
            _log.Clear();

            _director.Tick(0.01f);
            Assert.That(_director.CurrentWave, Is.EqualTo(2), "the number moves on in the step of the clear");
            Assert.That(_log, Has.Member("current 2"));

            _log.Clear();
            _director.Tick(1.5f);
            _director.Tick(1.4f);
            Assert.That(_director.InBreak, Is.True);
            Assert.That(_director.CurrentWave, Is.EqualTo(2), "during the break the number is the upcoming wave");
            Assert.That(_log, Is.Empty, "no number change while breathing");
        }

        [Test]
        public void BreakEnd_StartsTheUpcomingWave_WithoutChangingTheWaveNumberAgain()
        {
            _run.StartBattle();
            ClearWaveOne();
            _log.Clear();

            _director.Tick(3.01f);

            Assert.That(_log, Is.EqualTo(new[] { "started 2" }), "only the start of wave 2, no second number change");
            Assert.That(_director.CurrentWave, Is.EqualTo(2));
            Assert.That(_enemies.Spawned[_enemies.Spawned.Count - 1].StatScale, Is.EqualTo(1.1f).Within(1e-4),
                "the first enemy of wave 2 comes at once with the wave-2 scale");
        }

        [Test]
        public void NextWave_StartsOnTime_WithoutWaitingForAnythingElse()
        {
            // Wave never waits for heroes coming back or upgrades: only the 3 s clock starts the next wave.
            _run.StartBattle();
            ClearWaveOne();

            _director.Tick(3.01f);

            Assert.That(_log, Has.Member("started 2"));
            Assert.That(_director.CurrentWave, Is.EqualTo(2));
        }

        [Test]
        public void NewRun_StartsAgainFromWaveOne()
        {
            _run.StartBattle();
            ClearWaveOne();
            _run.StopBattle();
            _log.Clear();

            _run.StartBattle();

            Assert.That(_director.CurrentWave, Is.EqualTo(1));
            Assert.That(_log, Is.EqualTo(new[] { "current 1", "started 1" }));
        }
    }
}
