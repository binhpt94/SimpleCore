using System;
using System.Collections.Generic;
using Game.Core;
using Game.Enemy;
using Game.Run;
using Game.SimClock;
using Game.Wallet;
using NUnit.Framework;
using UnityEditor;
using Object = UnityEngine.Object;

namespace Game.Wave.Tests
{
    public class WaveStartCheatTests
    {
        private const string InstallerPath = "Assets/Features/Wave/FeatureAssets/Public/WaveInstaller.asset";

        private List<string> _log;
        private FakeRun _run;
        private FakeEnemies _enemies;
        private FakeSimClock _clock;
        private IDisposable _steps;
        private WaveDirector _director;
        private WaveStartCheat _cheat;

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
            _director.CurrentWaveChanged += w => _log.Add("current " + w);
            _cheat = new WaveStartCheat(_director, _run, 5);
            _cheat.Run();
        }

        [TearDown]
        public void TearDown()
        {
            _cheat.DisposeAsync().GetAwaiter().GetResult();
            _steps.Dispose();
            _director.Detach();
        }

        [Test]
        public void OneFrameAfterBattleStart_TheBattleStandsAtWaveFive_Paused()
        {
            _run.StartBattle();

            Assert.That(_director.CurrentWave, Is.EqualTo(1), "nothing happens before the first step of the battle");
            Assert.That(_run.IsPaused, Is.False);

            _log.Clear();
            _clock.Steps(1);

            Assert.That(_log, Is.EqualTo(new[] { "started 1", "current 5", "started 5", "pause" }),
                "wave 1 begins on the first step, then wave 5 starts through the normal path and the run is paused");
            Assert.That(_director.CurrentWave, Is.EqualTo(5));
            Assert.That(_run.IsPaused, Is.True);
            Assert.That(_enemies.Spawned[_enemies.Spawned.Count - 1].StatScale, Is.EqualTo(1.4f).Within(1e-5),
                "the enemy that came at once is a wave 5 enemy");
        }

        [Test]
        public void WhilePaused_TheBattleBehindStandsStill()
        {
            _run.StartBattle();
            _clock.Steps(1);
            var spawned = _enemies.Spawned.Count;

            _clock.Seconds(10);

            Assert.That(_enemies.Spawned.Count, Is.EqualTo(spawned));
            Assert.That(_director.CurrentWave, Is.EqualTo(5));
        }

        [Test]
        public void FramesBeforeTheBattle_DoNothing()
        {
            _clock.Steps(2);

            Assert.That(_log, Is.Empty);
            Assert.That(_run.IsPaused, Is.False);
        }

        [Test]
        public void Cheat_FiresOnce_TheNextBattleStartsAtWaveOneUnpaused()
        {
            _run.StartBattle();
            _clock.Steps(1);
            _run.IsPaused = false;

            _run.StartBattle();
            _clock.Steps(1);

            Assert.That(_director.CurrentWave, Is.EqualTo(1));
            Assert.That(_run.IsPaused, Is.False);
        }

        [Test]
        public void RealInstaller_HasTheCheatOff_OnlyWaveJoinsTheSession()
        {
            var installer = AssetDatabase.LoadAssetAtPath<InstallerAsset>(InstallerPath);
            var session = new FakeSession();

            installer.Install(Registry(session));

            Assert.That(session.Joined.Count, Is.EqualTo(1));
        }

        [Test]
        public void InstallerWithCheatPausedAtWaveSet_AlsoJoinsTheCheat()
        {
            var installer = Object.Instantiate(AssetDatabase.LoadAssetAtPath<InstallerAsset>(InstallerPath));
            try
            {
                var serialized = new SerializedObject(installer);
                var field = serialized.FindProperty("cheatPausedAtWave");
                Assert.That(field, Is.Not.Null, "WaveInstaller.cheatPausedAtWave");
                field.intValue = 5;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                var session = new FakeSession();

                installer.Install(Registry(session));

                Assert.That(session.Joined.Count, Is.EqualTo(2));
                Assert.That(session.Joined[1], Is.TypeOf<WaveStartCheat>());
            }
            finally
            {
                Object.DestroyImmediate(installer);
            }
        }

        private static ServiceRegistry Registry(FakeSession session)
        {
            var log = new List<string>();
            var registry = new ServiceRegistry();
            registry.Register<ISessionScene>(session);
            registry.Register<IRunFlow>(new FakeRun(log));
            registry.Register<IEnemyField>(new FakeEnemies());
            registry.Register<IWalletIncome>(new FakeIncome(log));
            registry.Register<ISimClock>(new FakeSimClock());
            registry.Register<ISimRandom>(new FakeSimRandom(12345));
            return registry;
        }
    }
}
