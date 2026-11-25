using NUnit.Framework;
using UnityEditor;

namespace Game.Wave.Tests
{
    public class WaveConfigTests
    {
        private const string ConfigPath = "Assets/Features/Wave/FeatureAssets/Private/WaveConfig.asset";

        [Test]
        public void RealConfig_HoldsTheDesignedPrototypeNumbers()
        {
            var config = AssetDatabase.LoadAssetAtPath<WaveConfig>(ConfigPath);
            Assert.That(config, Is.Not.Null, "missing " + ConfigPath);

            var rules = config.ToRules();

            Assert.That(rules.BaseCount, Is.EqualTo(6), "count = 6 + 2w");
            Assert.That(rules.CountPerWave, Is.EqualTo(2));
            Assert.That(rules.BaseInterval, Is.EqualTo(1.0f).Within(1e-5), "interval = max(0.3, 1.0 - 0.05(w-1))");
            Assert.That(rules.IntervalStep, Is.EqualTo(0.05f).Within(1e-5));
            Assert.That(rules.MinInterval, Is.EqualTo(0.3f).Within(1e-5));
            Assert.That(rules.StatStepPerWave, Is.EqualTo(0.10f).Within(1e-5), "scale = 1 + 0.10(w-1)");
            Assert.That(rules.WolfFromWave, Is.EqualTo(3));
            Assert.That(rules.GiantFromWave, Is.EqualTo(5));
            Assert.That(rules.ArcherFromWave, Is.EqualTo(7));
            Assert.That(rules.BossEveryWaves, Is.EqualTo(10));
            Assert.That(rules.UseArcher, Is.True, "PROTOTYPE: all 5 kinds, Archer from wave 7");
            Assert.That(rules.UseBoss, Is.True, "PROTOTYPE: all 5 kinds, Boss every 10 waves");
            Assert.That(rules.ClearRewardBase, Is.EqualTo(20), "clear reward = 20 + 5w");
            Assert.That(rules.ClearRewardPerWave, Is.EqualTo(5));
            Assert.That(rules.GapSeconds, Is.EqualTo(3f).Within(1e-5));
            Assert.That(rules.AliveCap, Is.EqualTo(40));
            Assert.That(rules.MaxSameSideRun, Is.EqualTo(3));

            var formula = new WaveFormula(rules);
            for (var wave = 1; wave <= 200; wave++)
            {
                var plan = formula.Plan(wave);
                Assert.That(plan.Archers > 0, Is.EqualTo(wave >= 7), $"PROTOTYPE: Archer from wave 7 (wave {wave})");
                Assert.That(plan.Bosses, Is.EqualTo(wave % 10 == 0 ? 1 : 0), $"PROTOTYPE: one Boss in waves 10, 20... (wave {wave})");
                Assert.That(plan.Soldiers + plan.Wolves + plan.Giants + plan.Archers + plan.Bosses, Is.EqualTo(plan.Count),
                    $"wave {wave}: the 5 kinds fill every place");
            }

            var designed = WaveRules.DefaultTiers();
            Assert.That(rules.Tiers.Count, Is.EqualTo(designed.Count), "6 mix tiers");
            for (var i = 0; i < designed.Count; i++)
            {
                Assert.That(rules.Tiers[i].FromWave, Is.EqualTo(designed[i].FromWave));
                Assert.That(rules.Tiers[i].Soldier, Is.EqualTo(designed[i].Soldier), $"tier {i} soldier");
                Assert.That(rules.Tiers[i].Wolf, Is.EqualTo(designed[i].Wolf), $"tier {i} wolf");
                Assert.That(rules.Tiers[i].Giant, Is.EqualTo(designed[i].Giant), $"tier {i} giant");
                Assert.That(rules.Tiers[i].Archer, Is.EqualTo(designed[i].Archer), $"tier {i} archer");
            }
        }
    }
}
