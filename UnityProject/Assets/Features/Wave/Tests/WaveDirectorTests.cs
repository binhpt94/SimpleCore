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
