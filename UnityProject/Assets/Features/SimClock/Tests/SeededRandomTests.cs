using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using static Game.SimClock.Tests.SimClockTestKit;

namespace Game.SimClock.Tests
{
    /// <summary>live: feature-sim-clock/seeded-random/random-values (ideas, edge cases, SeedParams and Streams rows).</summary>
    public class SeededRandomTests
    {
        private SimClockConfig _config;

        [SetUp]
        public void SetUp() => _config = NewConfig();

        [TearDown]
        public void TearDown() => Destroy(_config);

        private static List<int> Draw(ISimRandomStream stream, int count) =>
            Enumerable.Range(0, count).Select(_ => stream.Next(1000)).ToList();

        // ---------------------------------------------------------------- ideas

        [Test]
        public void Release_PicksANewRandomSeedEachRun()
        {
            var clock = NewClock(_config, false, out var seeds, out _);
            seeds.Next.Enqueue(111);
            seeds.Next.Enqueue(222);
            clock.SetUpRun();
            var first = clock.Seed;
            clock.ClearRun();
            clock.SetUpRun();
            Assert.That(first, Is.EqualTo(111));
            Assert.That(clock.Seed, Is.EqualTo(222));
            Assert.That(seeds.Asked, Has.All.EqualTo((0, int.MaxValue)));
        }

        [Test]
        public void Release_WritesTheSeedToTheLogWhenTheRunStarts()
        {
            var clock = NewClock(_config, false, out var seeds, out var log);
            seeds.Next.Enqueue(98765);
            clock.SetUpRun();
            Assert.That(log.Lines, Is.Empty, "not while setting up");
            clock.StartRun();
            Assert.That(log.Lines, Has.Count.EqualTo(1));
            Assert.That(log.Lines[0], Does.Contain("98765"));
        }

        [Test]
        public void Dev_UsesTheConfigSeed_AndPrintsItOnStart()
        {
            _config.devSeed = 4242;
            var clock = NewClock(_config, true, out var seeds, out var log);
            clock.SetUpRun();
            clock.StartRun();
            Assert.That(clock.Seed, Is.EqualTo(4242));
            Assert.That(seeds.Asked, Is.Empty, "the dev build never draws a seed");
            Assert.That(log.Lines.Single(), Does.Contain("4242").And.Contain("dev"));
        }

        [Test]
        public void Dev_EveryRunOfOneSession_UsesTheConfigSeed()
        {
            var clock = NewClock(_config, true, out _, out _);
            var seen = new List<int>();
            for (var i = 0; i < 3; i++)
            {
                clock.SetUpRun();
                clock.StartRun();
                seen.Add(clock.Seed);
                clock.ClearRun();
            }
            Assert.That(seen, Is.EqualTo(new[] { 12345, 12345, 12345 }));
        }

        [Test]
        public void Seed_IsAWholeNumberFrom0To2147483647()
        {
            var source = new SystemSeedSource();
            for (var i = 0; i < 2000; i++)
                Assert.That(source.Next(0, int.MaxValue), Is.InRange(0, int.MaxValue));
            Assert.That(source.Next(int.MaxValue, int.MaxValue), Is.EqualTo(int.MaxValue));
            _config.devSeed = -1;
            Assert.Throws<InvalidOperationException>(_config.Validate);
        }

        [Test]
        public void Stream_ComesFromSeedAndName_NotFromWhoAskedFirst()
        {
            var a = NewClock(_config, true, out _, out _);
            a.SetUpRun();
            var aAlpha = a.Stream("alpha");
            var aBeta = a.Stream("beta");

            var b = NewClock(_config, true, out _, out _);
            b.SetUpRun();
            var bBeta = b.Stream("beta");
            var bAlpha = b.Stream("alpha");

            Assert.That(Draw(bAlpha, 20), Is.EqualTo(Draw(aAlpha, 20)));
            Assert.That(Draw(bBeta, 20), Is.EqualTo(Draw(aBeta, 20)));
        }

        [Test]
        public void Stream_ExtraDrawsOnOneStream_DoNotShiftAnother()
        {
            var a = NewClock(_config, true, out _, out _);
            a.SetUpRun();
            Draw(a.Stream("alpha"), 37);
            var shifted = Draw(a.Stream("wave"), 20);

            var b = NewClock(_config, true, out _, out _);
            b.SetUpRun();
            Assert.That(Draw(b.Stream("wave"), 20), Is.EqualTo(shifted));
        }

        [Test]
        public void WaveStream_IsOneStream_ForShuffleAndSide()
        {
            var clock = NewClock(_config, true, out _, out _);
            clock.SetUpRun();
            var byName = clock.Stream("Wave");
            var byId = clock.Stream("wave");
            Assert.That(byName, Is.SameAs(byId));
            var draws = Draw(byName, 10);
            Assert.That(draws, Has.All.InRange(0, 999));
        }

        [Test]
        public void SameSeed_GivesSameNumbers_DifferentSeed_Differs()
        {
            var a = NewClock(_config, true, out _, out _);
            a.SetUpRun();
            var b = NewClock(_config, true, out _, out _);
            b.SetUpRun();
            Assert.That(Draw(a.Stream("wave"), 30), Is.EqualTo(Draw(b.Stream("wave"), 30)));

            a.SetUpRun();
            var seed12345 = Draw(a.Stream("wave"), 30);
            _config.devSeed = 777;
            var c = NewClock(_config, true, out _, out _);
            c.SetUpRun();
            Assert.That(Draw(c.Stream("wave"), 30), Is.Not.EqualTo(seed12345));
        }

        // ---------------------------------------------------------------- edge cases

        [Test]
        public void Edge_ReplayAfterLosing_DevGivesTheSameRun()
        {
            var clock = NewClock(_config, true, out _, out _);
            clock.SetUpRun();
            clock.StartRun();
            var first = Draw(clock.Stream("wave"), 25);
            clock.ClearRun();
            clock.SetUpRun();
            clock.StartRun();
            Assert.That(Draw(clock.Stream("wave"), 25), Is.EqualTo(first));
        }

        [Test]
        public void Edge_ReplayAfterLosing_ReleasePicksANewSeed()
        {
            var clock = NewClock(_config, false, out var seeds, out _);
            seeds.Next.Enqueue(5);
            seeds.Next.Enqueue(6);
            clock.SetUpRun();
            clock.ClearRun();
            clock.SetUpRun();
            Assert.That(clock.Seed, Is.EqualTo(6));
        }

        [Test]
        public void Edge_CastleRevivedByAd_KeepsSeedStepsAndStreamPosition()
        {
            var straight = NewClock(_config, true, out _, out _);
            straight.SetUpRun();
            straight.StartRun();
            var expected = Draw(straight.Stream("wave"), 20);

            var revived = NewClock(_config, true, out _, out var log);
            revived.SetUpRun();
            revived.StartRun();
            var wave = revived.Stream("wave");
            var got = Draw(wave, 10);
            revived.Advance(0.1);
            revived.SetGameplay(false);
            revived.Advance(0.1);
            revived.SetGameplay(true);
            revived.Advance(0.025);
            got.AddRange(Draw(wave, 10));

            Assert.That(got, Is.EqualTo(expected));
            Assert.That(revived.StepCount, Is.EqualTo(9));
            Assert.That(revived.GameplayStepCount, Is.EqualTo(5));
            Assert.That(log.Lines, Has.Count.EqualTo(1), "seed written once per run");
        }

        [Test]
        public void Edge_SetUpFailedBeforeStart_WritesNoSeed_AndNextSetUpPicksAgain()
        {
            var clock = NewClock(_config, false, out var seeds, out var log);
            seeds.Next.Enqueue(10);
            seeds.Next.Enqueue(20);
            clock.SetUpRun();
            clock.ClearRun();
            Assert.That(log.Lines, Is.Empty);
            clock.SetUpRun();
            clock.StartRun();
            Assert.That(clock.Seed, Is.EqualTo(20));
            Assert.That(log.Lines.Single(), Does.Contain("20"));
        }

        [Test]
        public void Edge_TesterReplaysAnyRun_BySettingTheConfigSeed()
        {
            var release = NewClock(_config, false, out var seeds, out var log);
            seeds.Next.Enqueue(31337);
            release.SetUpRun();
            release.StartRun();
            var reported = Draw(release.Stream("wave"), 15);
            Assert.That(log.Lines.Single(), Does.Contain("31337"));

            _config.devSeed = 31337;
            var dev = NewClock(_config, true, out _, out _);
            dev.SetUpRun();
            dev.StartRun();
            Assert.That(Draw(dev.Stream("wave"), 15), Is.EqualTo(reported));
        }

        [Test]
        public void Edge_StreamNameNotInStreams_IsAClearError()
        {
            var clock = NewClock(_config, true, out _, out _);
            clock.SetUpRun();
            var ex = Assert.Throws<InvalidOperationException>(() => clock.Stream("loot"));
            Assert.That(ex.Message, Does.Contain("loot").And.Contain("Streams"));
        }

        [Test]
        public void Edge_VisualEffectRandom_DoesNotShiftGameplayStreams()
        {
            var a = NewClock(_config, true, out _, out _);
            a.SetUpRun();
            var plain = Draw(a.Stream("wave"), 20);

            var b = NewClock(_config, true, out _, out _);
            b.SetUpRun();
            var wave = b.Stream("wave");
            var got = new List<int>();
            for (var i = 0; i < 20; i++)
            {
                UnityEngine.Random.Range(0, 100);
                got.Add(wave.Next(1000));
            }
            Assert.That(got, Is.EqualTo(plain));
        }

        [Test]
        public void Edge_RunCancelled_ClearsStreams_NextRunStartsFresh()
        {
            var clock = NewClock(_config, true, out _, out _);
            clock.SetUpRun();
            clock.StartRun();
            var wave = clock.Stream("wave");
            var first = Draw(wave, 5);
            clock.ClearRun();
            Assert.Throws<InvalidOperationException>(() => wave.Next(10));
            clock.SetUpRun();
            Assert.That(Draw(wave, 5), Is.EqualTo(first));
        }

        // ---------------------------------------------------------------- tables

        [Test]
        public void Params_RealConfigHoldsTheSeedNumbers()
        {
            var config = LoadConfig();
            Assert.That(config.devSeed, Is.EqualTo(12345));
            Assert.That(config.seedMin, Is.EqualTo(0));
            Assert.That(config.seedMax, Is.EqualTo(int.MaxValue));
            Assert.That(config.FindStream("wave"), Is.Not.Null);
        }

        [Test]
        public void release_seed_IsRandomEachRun_WithinTheRange()
        {
            var config = LoadConfig();
            var clock = new SimClockService(config, false, new SystemSeedSource(), new FakeLog());
            var seen = new HashSet<int>();
            for (var i = 0; i < 20; i++)
            {
                clock.SetUpRun();
                Assert.That(clock.Seed, Is.InRange(config.seedMin, config.seedMax), "release_seed");
                seen.Add(clock.Seed);
                clock.ClearRun();
            }
            Assert.That(seen.Count, Is.GreaterThan(1));
        }

        [Test]
        public void release_seed_log_WritesOnStart_Once()
        {
            var clock = NewClock(LoadConfig(), false, out var seeds, out var log);
            seeds.Next.Enqueue(77);
            clock.SetUpRun();
            clock.StartRun();
            clock.StartRun();
            Assert.That(log.Lines, Is.EqualTo(new[] { "[SimClock] Seed của lượt: 77" }), "release_seed_log");
        }

        [Test]
        public void dev_seed_ComesFromConfig()
        {
            var clock = NewClock(LoadConfig(), true, out var seeds, out _);
            clock.SetUpRun();
            Assert.That(clock.Seed, Is.EqualTo(LoadConfig().devSeed), "dev_seed");
            Assert.That(seeds.Asked, Is.Empty);
        }

        [Test]
        public void dev_seed_default_Is12345()
        {
            Assert.That(LoadConfig().devSeed, Is.EqualTo(12345), "dev_seed_default");
        }

        [Test]
        public void dev_seed_print_PrintsToConsoleOnStart()
        {
            var clock = NewClock(LoadConfig(), true, out _, out var log);
            clock.SetUpRun();
            Assert.That(log.Lines, Is.Empty);
            clock.StartRun();
            Assert.That(log.Lines, Is.EqualTo(new[] { "[SimClock] Seed của lượt (bản dev, từ config): 12345" }),
                "dev_seed_print");
        }

        [Test]
        public void dev_seed_step_EveryRunUsesTheConfigSeed()
        {
            var clock = NewClock(LoadConfig(), true, out _, out _);
            for (var i = 0; i < 4; i++)
            {
                clock.SetUpRun();
                Assert.That(clock.Seed, Is.EqualTo(12345), "dev_seed_step");
                clock.ClearRun();
            }
        }

        [Test]
        public void seed_min_Is0()
        {
            var config = LoadConfig();
            Assert.That(config.seedMin, Is.EqualTo(0), "seed_min");
            var clock = NewClock(config, false, out var seeds, out _);
            clock.SetUpRun();
            Assert.That(seeds.Asked.Single().min, Is.EqualTo(0));
        }

        [Test]
        public void seed_max_Is2147483647()
        {
            var config = LoadConfig();
            Assert.That(config.seedMax, Is.EqualTo(2147483647), "seed_max");
            var clock = NewClock(config, false, out var seeds, out _);
            seeds.Next.Enqueue(2147483647);
            clock.SetUpRun();
            Assert.That(clock.Seed, Is.EqualTo(2147483647));
            Assert.That(seeds.Asked.Single().max, Is.EqualTo(2147483647));
        }

        [Test]
        public void Streams_wave_RowExists_AndGivesTheWaveStream()
        {
            var config = LoadConfig();
            var row = config.FindStream("wave");
            Assert.That(row, Is.Not.Null, "Streams row wave");
            Assert.That(row.id, Is.EqualTo("wave"));
            Assert.That(row.name, Is.EqualTo("Wave"));
            Assert.That(row.stage, Is.EqualTo("idea"));
            Assert.That(row.owner, Is.EqualTo("feature-wave"));
            var clock = NewClock(config, true, out _, out _);
            clock.SetUpRun();
            var wave = clock.Stream("Wave");
            Assert.That(wave.Next(2), Is.InRange(0, 1));
        }
    }
}
