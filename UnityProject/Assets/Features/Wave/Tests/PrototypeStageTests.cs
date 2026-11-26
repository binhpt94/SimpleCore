using System;
using System.Collections.Generic;
using System.Linq;
using Game.Enemy;
using NUnit.Framework;
using UnityEditor;

namespace Game.Wave.Tests
{
    /// <summary>
    /// The PROTOTYPE column of the "Thông số theo giai đoạn" tables (#stage-prototype of each Wave node),
    /// checked on the real WaveConfig asset: what PROTOTYPE asks that is new or different from IDEA.
    /// </summary>
    public class PrototypeStageTests
    {
        private const string ConfigPath = "Assets/Features/Wave/FeatureAssets/Private/WaveConfig.asset";

        private static WaveRules RealRules()
        {
            var config = AssetDatabase.LoadAssetAtPath<WaveConfig>(ConfigPath);
            Assert.That(config, Is.Not.Null, "missing " + ConfigPath);
            return config.ToRules();
        }

        // wave-formula#stage-prototype: "Loại quái dùng: đủ 5 loại" (IDEA had no Boss and no Archer).
        [Test]
        public void WaveFormulaStagePrototype_RealConfigUsesAllFiveKinds()
        {
            var formula = new WaveFormula(RealRules());

            var kinds = new HashSet<EnemyType>();
            for (var wave = 1; wave <= 20; wave++)
            {
                var plan = formula.Plan(wave);
                if (plan.Soldiers > 0) kinds.Add(EnemyType.Soldier);
                if (plan.Wolves > 0) kinds.Add(EnemyType.Wolf);
                if (plan.Giants > 0) kinds.Add(EnemyType.Giant);
                if (plan.Archers > 0) kinds.Add(EnemyType.Archer);
                if (plan.Bosses > 0) kinds.Add(EnemyType.Boss);
            }

            Assert.That(kinds, Is.EquivalentTo(new[]
                { EnemyType.Soldier, EnemyType.Wolf, EnemyType.Giant, EnemyType.Archer, EnemyType.Boss }));
            Assert.That(formula.Plan(7).Archers, Is.GreaterThan(0), "Archer from wave 7");
            Assert.That(formula.Plan(10).Bosses, Is.EqualTo(1), "one Boss in wave 10");
            Assert.That(formula.Plan(9).Bosses, Is.EqualTo(0));
        }

        // spawn-order#stage-prototype: "Vị trí Trùm: con cuối (N)" (IDEA had no Boss).
        [Test]
        public void SpawnOrderStagePrototype_RealConfigWaveTen_EndsWithTheBoss()
        {
            var rules = RealRules();
            var plan = new WaveFormula(rules).Plan(10);

            for (var seed = 0; seed < 30; seed++)
            {
                var list = new SpawnOrder(rules, new SystemWaveRandom(seed)).Build(plan);

                Assert.That(list.Count, Is.EqualTo(26), "wave 10 has 26 enemies");
                Assert.That(list.Last().Type, Is.EqualTo(EnemyType.Boss), $"seed {seed}: the Boss is enemy 26");
                Assert.That(list.Count(e => e.Type == EnemyType.Boss), Is.EqualTo(1));
            }
        }

        // clear-rules#stage-prototype: "Báo dọn sạch cho feature-hud: có".
        // gap-timer#stage-prototype: "Báo đợt mới cho feature-hud: có".
        [Test]
        public void ClearRulesStagePrototype_And_GapTimerStagePrototype_HudHearsTheClearAndTheNewWave()
        {
            var log = new List<string>();
            var run = new FakeRun(log);
            var enemies = new FakeEnemies();
            var clock = new FakeSimClock();
            var director = new WaveDirector(RealRules(), run, enemies, new FakeIncome(log), new FirstRandom());
            director.Attach();
            IWaveProgress hud = director; // what Hud listens to
            hud.WaveCleared += w => log.Add("hud cleared " + w);
            hud.WaveStarted += w => log.Add("hud started " + w);
            hud.CurrentWaveChanged += w => log.Add("hud wave " + w);

            using (clock.OnStep(WaveParticipant.Owner, director.Step))
            {
                run.StartBattle();
                clock.Steps(1);
                clock.Seconds(7.01);
                enemies.DefeatAll();
                log.Clear();
                clock.Steps(1);

                Assert.That(log, Is.EqualTo(new[] { "coins 25", "score 1", "hud cleared 1", "hud wave 2" }),
                    "Hud is told of the clear (after the reward) and of the upcoming wave number");

                log.Clear();
                clock.Seconds(3.01);
                Assert.That(log, Is.EqualTo(new[] { "hud started 2" }), "Hud is told when the new wave starts");
            }
            director.Detach();
        }

        // gap-timer#stage-prototype: "Độ dài lúc thở: 3 s" kept from IDEA on the real config.
        [Test]
        public void GapTimerStagePrototype_RealConfigBreakIsThreeSeconds()
        {
            Assert.That(RealRules().GapSeconds, Is.EqualTo(3f).Within(1e-5));
        }
    }
}
