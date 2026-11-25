using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Game.SimClock.Tests
{
    /// <summary>Shared fakes and configs of the SimClock tests.</summary>
    internal static class SimClockTestKit
    {
        internal const string ConfigPath = "Assets/Features/SimClock/FeatureAssets/Private/SimClockConfig.asset";
        internal const string InstallerPath = "Assets/Features/SimClock/FeatureAssets/Public/SimClockInstaller.asset";

        /// <summary>The real config asset of the feature (the design numbers).</summary>
        internal static SimClockConfig LoadConfig()
        {
            var config = AssetDatabase.LoadAssetAtPath<SimClockConfig>(ConfigPath);
            Assert(config != null, $"missing {ConfigPath}");
            return config;
        }

        /// <summary>An in-memory copy of the design numbers, with extra test streams; free to change in a test.</summary>
        internal static SimClockConfig NewConfig()
        {
            var config = ScriptableObject.CreateInstance<SimClockConfig>();
            config.name = "TestSimClockConfig";
            config.stepOrder = new[] { "Wave", "Enemy", "Hero", "Castle", "Combat", "Run" };
            config.streams = new[]
            {
                new SimClockConfig.StreamEntry { id = "wave", name = "Wave", stage = "idea", owner = "feature-wave" },
                new SimClockConfig.StreamEntry { id = "alpha", name = "Alpha", stage = "idea", owner = "test" },
                new SimClockConfig.StreamEntry { id = "beta", name = "Beta", stage = "idea", owner = "test" },
            };
            return config;
        }

        internal static SimClockService NewClock(SimClockConfig config, bool dev, out FakeSeeds seeds, out FakeLog log)
        {
            seeds = new FakeSeeds();
            log = new FakeLog();
            return new SimClockService(config, dev, seeds, log);
        }

        /// <summary>A clock of the test config whose run has started (dev build, seed 12345).</summary>
        internal static SimClockService StartedClock(SimClockConfig config = null, bool dev = true)
        {
            var clock = NewClock(config ? config : NewConfig(), dev, out _, out _);
            clock.SetUpRun();
            clock.StartRun();
            return clock;
        }

        internal static void Destroy(Object o)
        {
            if (o != null) Object.DestroyImmediate(o);
        }

        private static void Assert(bool ok, string why)
        {
            if (!ok) throw new System.InvalidOperationException(why);
        }

        internal sealed class FakeSeeds : ISeedSource
        {
            internal readonly Queue<int> Next = new Queue<int>();
            internal readonly List<(int min, int max)> Asked = new List<(int, int)>();

            int ISeedSource.Next(int min, int max)
            {
                Asked.Add((min, max));
                return Next.Count > 0 ? Next.Dequeue() : min;
            }
        }

        internal sealed class FakeLog : ISeedLog
        {
            internal readonly List<string> Lines = new List<string>();

            public void Write(string line) => Lines.Add(line);
        }
    }
}
