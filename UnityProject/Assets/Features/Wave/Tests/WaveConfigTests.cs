using NUnit.Framework;
using UnityEditor;

namespace Game.Wave.Tests
{
    public class WaveConfigTests
    {
        private const string ConfigPath = "Assets/Features/Wave/FeatureAssets/Private/WaveConfig.asset";

        [Test]
        public void RealConfig_HoldsTheDesignedIdeaNumbers()
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
            Assert.That(rules.UseArcher, Is.False, "IDEA: no Archer, its places go to Soldiers");
            Assert.That(rules.UseBoss, Is.False, "IDEA: no Boss");
            Assert.That(rules.ClearRewardBase, Is.EqualTo(20), "clear reward = 20 + 5w");
            Assert.That(rules.ClearRewardPerWave, Is.EqualTo(5));
            Assert.That(rules.GapSeconds, Is.EqualTo(3f).Within(1e-5));
            Assert.That(rules.AliveCap, Is.EqualTo(40));
            Assert.That(rules.MaxSameSideRun, Is.EqualTo(3));

            var formula = new WaveFormula(rules);
            for (var wave = 1; wave <= 200; wave++)
            {
                var plan = formula.Plan(wave);
                Assert.That(plan.Archers, Is.EqualTo(0), $"IDEA: no Archer in wave {wave}");
                Assert.That(plan.Bosses, Is.EqualTo(0), $"IDEA: no Boss in wave {wave}");
                Assert.That(plan.Soldiers + plan.Wolves + plan.Giants, Is.EqualTo(plan.Count),
                    $"wave {wave}: only Soldier, Wolf and Giant; Archer and Boss places go to Soldiers");
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
