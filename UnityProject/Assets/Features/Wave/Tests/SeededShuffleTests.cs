using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace Game.Wave.Tests
{
    /// <summary>
    /// spawn-order#seeded-shuffle: the kind order and the sides come from the run's stream named Wave,
    /// so the same seed gives the same waves (feature-sim-clock/seeded-random).
    /// </summary>
    public class SeededShuffleTests
    {
        /// <summary>Plays waves 7, 8 and 9 (all kinds unlocked) and returns what came, in order, as "Kind/Side".</summary>
        private static List<string> PlayThreeWaves(int seed, WaveRules rules = null, Action<FakeSimRandom, WaveDirector, FakeEnemies, FakeSimClock> during = null)
        {
            var log = new List<string>();
            var run = new FakeRun(log);
            var enemies = new FakeEnemies();
            var clock = new FakeSimClock { Paused = () => run.IsPaused };
            var random = new FakeSimRandom(seed);
            var director = new WaveDirector(rules ?? new WaveRules(), run, enemies, new FakeIncome(log),
                new StreamWaveRandom(random.Stream(WaveParticipant.Owner)));
            director.Attach();
            using (clock.OnStep(WaveParticipant.Owner, director.Step))
            {
                run.StartBattle();
                clock.Steps(1);
                director.StartWave(7);
                during?.Invoke(random, director, enemies, clock);
                for (var wave = 7; wave <= 9; wave++)
                {
                    while (director.Waiting > 0)
                    {
                        clock.Seconds(1);
                        if (enemies.AliveCount > 0) enemies.Defeat(1);
                    }
                    enemies.DefeatAll();
                    clock.Steps(1);
                    Assert.That(log, Has.Member("score " + wave), "wave " + wave + " was cleared");
                    clock.Seconds(3.01);
                }
            }
            director.Detach();
            // Skip the wave-1 enemy that came before the cheat.
            return enemies.Spawned.Skip(1).Select(s => s.Type + "/" + s.Side).ToList();
        }

        [Test]
        public void SameRunSeed_GivesTheSameKindOrderAndSides_InEveryWave()
        {
            var first = PlayThreeWaves(12345);
            var again = PlayThreeWaves(12345);

            Assert.That(first.Count, Is.GreaterThan(60), "waves 7 to 9 and the first of wave 10");
            Assert.That(again, Is.EqualTo(first));
        }

        [Test]
        public void DifferentRunSeeds_GiveDifferentWaves()
        {
            var orders = new HashSet<string>();
            for (var seed = 0; seed < 5; seed++) orders.Add(string.Join(",", PlayThreeWaves(seed)));

            Assert.That(orders.Count, Is.EqualTo(5));
        }

        [Test]
        public void EnemyHeldAtTheAliveCap_KeepsTheKindAndSideDrawnForIt_AndDrawsNothingWhileWaiting()
        {
            var free = PlayThreeWaves(777);

            var drawsWhileHeld = -1;
            var held = PlayThreeWaves(777, new WaveRules { AliveCap = 2 }, (random, director, enemies, clock) =>
            {
                clock.Seconds(5);
                Assert.That(director.Waiting, Is.GreaterThan(0), "wave 7 is held at the cap");
                var before = random.Draws(WaveParticipant.Owner);
                clock.Seconds(30);
                drawsWhileHeld = random.Draws(WaveParticipant.Owner) - before;
            });

            Assert.That(drawsWhileHeld, Is.EqualTo(0), "waiting for a free place draws no number");
            Assert.That(held, Is.EqualTo(free), "holding changes neither the enemy waiting nor the later draws");
        }
    }
}
