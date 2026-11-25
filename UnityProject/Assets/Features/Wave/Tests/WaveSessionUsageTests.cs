using System.Collections.Generic;
using Game.Core;
using NUnit.Framework;

namespace Game.Wave.Tests
{
    /// <summary>
    /// Proposal 2026-11-25-session-usage-tracking: Wave reports its running jobs to the session
    /// (step subscription, send or break clock, the dev cheat's pending wait) and every count is back to 0 after clean-up.
    /// </summary>
    public class WaveSessionUsageTests
    {
        private List<string> _log;
        private FakeRun _run;
        private FakeEnemies _enemies;
        private FakeSimClock _clock;
        private WaveDirector _director;

        [SetUp]
        public void SetUp()
        {
            _log = new List<string>();
            _run = new FakeRun(_log);
            _enemies = new FakeEnemies();
            _clock = new FakeSimClock { Paused = () => _run.IsPaused };
            _director = new WaveDirector(new WaveRules(), _run, _enemies, new FakeIncome(_log), new FirstRandom());
        }

        private static ISessionParticipantUsage Usage(ISessionParticipant participant) => (ISessionParticipantUsage)participant;

        [Test]
        public void Wave_CountsItsStepLoopAndTheSendClock_AndIsBackToZeroAfterCleanUp()
        {
            var wave = new WaveParticipant(_director, _clock);

            wave.Run();
            Assert.That(Usage(wave).RunningJobs, Is.EqualTo(1), "the step subscription, no wave yet");
            Assert.That(Usage(wave).HeldViews, Is.EqualTo(0), "Wave has no view");

            _run.StartBattle();
            _clock.Steps(1);
            Assert.That(Usage(wave).RunningJobs, Is.EqualTo(2), "the send clock of wave 1 waits on the steps");

            wave.DisposeAsync().GetAwaiter().GetResult();
            Assert.That(Usage(wave).RunningJobs, Is.EqualTo(0));
            Assert.That(_clock.Subscribers, Is.EqualTo(0), "no step subscription left");
        }

        [Test]
        public void BreakBetweenWaves_StaysCountedAsAJob_UntilTheBattleStops()
        {
            var wave = new WaveParticipant(_director, _clock);
            wave.Run();
            _run.StartBattle();
            _clock.Steps(1); // wave 1 begins on the first gameplay step
            _clock.Seconds(7.01);
            Assert.That(_director.Waiting, Is.EqualTo(0), "all 8 enemies of wave 1 came");
            _enemies.DefeatAll();
            _clock.Steps(1);
            Assert.That(_director.InBreak, Is.True);
            Assert.That(Usage(wave).RunningJobs, Is.EqualTo(2), "the break clock");

            _run.StopBattle();
            Assert.That(Usage(wave).RunningJobs, Is.EqualTo(1), "the break is dropped at once, only the step loop is left");

            wave.DisposeAsync().GetAwaiter().GetResult();
            Assert.That(Usage(wave).RunningJobs, Is.EqualTo(0));
        }

        [Test]
        public void CleanUpInTheMiddleOfAWave_ReleasesEveryJob()
        {
            var wave = new WaveParticipant(_director, _clock);
            wave.Run();
            _run.StartBattle();
            _clock.Seconds(2.5);

            wave.DisposeAsync().GetAwaiter().GetResult();

            Assert.That(Usage(wave).RunningJobs, Is.EqualTo(0));
        }

        [Test]
        public void DevCheat_CountsItsPendingWait_UntilItFires_OrTheSessionEnds()
        {
            var wave = new WaveParticipant(_director, _clock);
            wave.Run();
            var cheat = new WaveStartCheat(_director, _run, 5);
            cheat.Run();
            Assert.That(Usage(cheat).RunningJobs, Is.EqualTo(1), "waiting for the battle");

            _run.StartBattle();
            _clock.Steps(1);
            Assert.That(Usage(cheat).RunningJobs, Is.EqualTo(0), "fired once, nothing left waiting");

            var unused = new WaveStartCheat(_director, _run, 5);
            unused.Run();
            unused.DisposeAsync().GetAwaiter().GetResult();
            Assert.That(Usage(unused).RunningJobs, Is.EqualTo(0), "a cheat that never fired is released at clean-up");

            cheat.DisposeAsync().GetAwaiter().GetResult();
            wave.DisposeAsync().GetAwaiter().GetResult();
            Assert.That(Usage(wave).RunningJobs, Is.EqualTo(0));
        }
    }
}
