using System.Collections.Generic;
using System.Linq;
using Game.Enemy;
using NUnit.Framework;

namespace Game.Wave.Tests
{
    public class SpawnOrderTests
    {
        [Test]
        public void EverySide_GetsExactlyHalf_AndNeverMoreThanThreeInARow()
        {
            var rules = new WaveRules();
            var formula = new WaveFormula(rules);
            foreach (var wave in new[] { 1, 2, 5, 10, 20, 37 })
            {
                for (var seed = 0; seed < 200; seed++)
                {
                    var list = new SpawnOrder(rules, new SystemWaveRandom(seed)).Build(formula.Plan(wave));

                    Assert.That(list.Count(e => e.Side == EnemySide.Left), Is.EqualTo(list.Count / 2), $"wave {wave} seed {seed}");
                    Assert.That(LongestRun(list), Is.LessThanOrEqualTo(3), $"wave {wave} seed {seed}");
                }
            }
        }

        [Test]
        public void Boss_IsAlwaysTheLastEnemyOfTheWave()
        {
            var rules = new WaveRules();
            var plan = new WaveFormula(rules).Plan(10);
            for (var seed = 0; seed < 50; seed++)
            {
                var list = new SpawnOrder(rules, new SystemWaveRandom(seed)).Build(plan);

                Assert.That(list.Last().Type, Is.EqualTo(EnemyType.Boss));
                Assert.That(list.Count(e => e.Type == EnemyType.Boss), Is.EqualTo(1));
            }
        }

        [Test]
        public void SendList_HoldsExactlyTheKindsOfThePlan()
        {
            var rules = new WaveRules();
            var plan = new WaveFormula(rules).Plan(7);

            var list = new SpawnOrder(rules, new SystemWaveRandom(3)).Build(plan);

            Assert.That(list.Count(e => e.Type == EnemyType.Soldier), Is.EqualTo(plan.Soldiers));
            Assert.That(list.Count(e => e.Type == EnemyType.Wolf), Is.EqualTo(plan.Wolves));
            Assert.That(list.Count(e => e.Type == EnemyType.Giant), Is.EqualTo(plan.Giants));
            Assert.That(list.Count(e => e.Type == EnemyType.Archer), Is.EqualTo(plan.Archers));
        }

        private static int LongestRun(List<SpawnEntry> list)
        {
            var best = 0;
            var run = 0;
            for (var i = 0; i < list.Count; i++)
            {
                run = i > 0 && list[i].Side == list[i - 1].Side ? run + 1 : 1;
                if (run > best) best = run;
            }
            return best;
        }
    }
}
