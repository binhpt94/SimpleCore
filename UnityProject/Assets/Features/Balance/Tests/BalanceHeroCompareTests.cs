using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using M = Game.Balance.Tests.BalanceMath;
using N = Game.Balance.Tests.BalanceNumbers;

namespace Game.Balance.Tests
{
    // feature-balance/balance-team/hero-compare: so sánh sức hero.
    public class BalanceHeroCompareTests
    {
        static double S(HeroCode c, int level = 1, int areaHits = 2, bool reached = false) =>
            M.HeroPower(N.Hero(c), level, areaHits, reached);

        static double TeamDpsLv1(params HeroCode[] team) =>
            M.TeamHeroDps(TeamBuild.Fresh(team));

        // Bảng hợp khó (dự kiến): mỗi hero các loại quái nó hợp và các loại nó khó.
        static readonly Dictionary<HeroCode, EnemyKind[][]> Counters = new Dictionary<HeroCode, EnemyKind[][]>
        {
            { HeroCode.Knight,    new[] { new[] { EnemyKind.Giant, EnemyKind.Boss },        new[] { EnemyKind.EnemyArcher } } },
            { HeroCode.Axeman,    new[] { new[] { EnemyKind.Soldier, EnemyKind.Wolf },      new[] { EnemyKind.EnemyArcher, EnemyKind.Boss } } },
            { HeroCode.Swordsman, new[] { new[] { EnemyKind.EnemyArcher, EnemyKind.Giant }, new[] { EnemyKind.Wolf, EnemyKind.Soldier } } },
            { HeroCode.Archer,    new[] { new[] { EnemyKind.EnemyArcher, EnemyKind.Boss },  new[] { EnemyKind.Wolf } } },
            { HeroCode.Mage,      new[] { new[] { EnemyKind.Soldier, EnemyKind.Wolf },      new[] { EnemyKind.Giant, EnemyKind.Boss } } },
        };

        [Test]
        public void PowerIsRootOfEffectiveHpTimesEffectiveDps()
        {
            // Hiệp sĩ: √(300 × 12) = 60. Đấu sĩ rìu đòn vùng ×2: √(240 × 20). Cung thủ tầm 7u máu ×2: √(240 × 17.5).
            Assert.AreEqual(60.0, S(HeroCode.Knight), 0.05);
            Assert.AreEqual(Math.Sqrt(240 * 20.0), S(HeroCode.Axeman), 1e-9);
            Assert.AreEqual(Math.Sqrt(240 * 17.5), S(HeroCode.Archer), 1e-9);
            Assert.AreEqual(Math.Sqrt(200 * 22.5), S(HeroCode.Mage), 1e-9);
            Assert.AreEqual(240, M.HeroEffectiveHp(N.Hero(HeroCode.Archer)), 1e-9);
            Assert.AreEqual(180, M.HeroEffectiveHp(N.Hero(HeroCode.Swordsman)), 1e-9);
            Assert.AreEqual(22.5, M.HeroEffectiveDps(N.Hero(HeroCode.Mage)), 1e-9);
        }

        [Test]
        public void AllHeroPowersAreWithinTenPercentOfTheAverage()
        {
            Assert.AreEqual(60.0, S(HeroCode.Knight), 0.1);
            Assert.AreEqual(69.3, S(HeroCode.Axeman), 0.1);
            Assert.AreEqual(64.1, S(HeroCode.Swordsman), 0.1);
            Assert.AreEqual(64.8, S(HeroCode.Archer), 0.1);
            Assert.AreEqual(67.1, S(HeroCode.Mage), 0.1);

            double avg = N.Heroes.Average(h => M.HeroPower(h));
            Assert.AreEqual(65, avg, 0.5);
            foreach (var h in N.Heroes)
                Assert.That(Math.Abs(M.HeroPower(h) / avg - 1), Is.LessThanOrEqualTo(0.10), h.Name);
            Assert.AreEqual(HeroCode.Knight, N.Heroes.OrderBy(h => M.HeroPower(h)).First().Code, "thấp nhất");
            Assert.AreEqual(HeroCode.Axeman, N.Heroes.OrderBy(h => M.HeroPower(h)).Last().Code, "cao nhất");
        }

        // Hạng có tính hoà: số hero khác hơn hẳn + 1.
        static int TopRank(HeroStats h, Func<HeroStats, double> stat) => N.Heroes.Count(o => stat(o) > stat(h)) + 1;
        static int BottomRank(HeroStats h, Func<HeroStats, double> stat) => N.Heroes.Count(o => stat(o) < stat(h)) + 1;

        [Test]
        public void EveryHeroHasAStrongStatAndAWeakStat()
        {
            var stats = new Func<HeroStats, double>[]
            {
                h => h.Hp, h => h.Damage, h => -h.AttackSeconds, h => h.Range, h => h.Speed, h => h.Dps,
            };
            foreach (var h in N.Heroes)
            {
                Assert.IsTrue(stats.Any(s => TopRank(h, s) <= 2), h.Name + " không có chỉ số cao nhất hoặc nhì");
                Assert.IsTrue(stats.Any(s => BottomRank(h, s) <= 2), h.Name + " không có chỉ số thấp nhất hoặc nhì");
            }
            Assert.AreEqual(1, TopRank(N.Hero(HeroCode.Knight), h => h.Hp));
            Assert.AreEqual(1, BottomRank(N.Hero(HeroCode.Knight), h => h.Damage));
            Assert.AreEqual(1, BottomRank(N.Hero(HeroCode.Mage), h => h.Hp));
            Assert.AreEqual(1, BottomRank(N.Hero(HeroCode.Knight), h => h.Speed), "Hiệp sĩ đi chậm nhất");
            Assert.AreEqual(1, TopRank(N.Hero(HeroCode.Swordsman), h => h.Speed), "Kiếm khách đi nhanh nhất");
        }

        [Test]
        public void EveryHeroHasAnEnemyItFitsAndAnEnemyItStruggles()
        {
            foreach (var h in N.Heroes)
            {
                Assert.IsTrue(Counters.ContainsKey(h.Code), h.Name + " không có trong bảng hợp khó");
                var good = Counters[h.Code][0];
                var hard = Counters[h.Code][1];
                Assert.That(good.Length, Is.GreaterThan(0), h.Name + " không có loại hợp");
                Assert.That(hard.Length, Is.GreaterThan(0), h.Name + " không có loại khó");
                Assert.IsEmpty(good.Intersect(hard), h.Name + " vừa hợp vừa khó cùng một loại");
            }

            var giant = N.Enemy(EnemyKind.Giant);
            var archerFoe = N.Enemy(EnemyKind.EnemyArcher);
            var wolf = N.Enemy(EnemyKind.Wolf);
            // Hiệp sĩ chịu Khổng lồ 30 s; tầm 1.2u phải đi tới Cung thủ quái đứng cách 5u.
            Assert.AreEqual(30, N.Hero(HeroCode.Knight).Hp / giant.Dps, 1e-9);
            Assert.That(N.Hero(HeroCode.Knight).Range, Is.LessThan(archerFoe.Range));
            // Kiếm khách đuổi kịp Cung thủ quái, hạ Khổng lồ trong khoảng 6.3 s.
            Assert.That(N.Hero(HeroCode.Swordsman).Speed, Is.GreaterThan(archerFoe.Speed));
            Assert.AreEqual(6.3, giant.Hp / N.Hero(HeroCode.Swordsman).Dps, 0.63);
            // Cung thủ bắn trước tầm của Cung thủ quái; Sói đi nhanh bằng Cung thủ nên tới sát được.
            Assert.That(N.Hero(HeroCode.Archer).Range, Is.GreaterThan(archerFoe.Range));
            Assert.That(wolf.Speed, Is.GreaterThanOrEqualTo(N.Hero(HeroCode.Archer).Speed));
            // Pháp sư hạ Sói trong 2 đòn; cần khoảng 12.8 s cho một Khổng lồ.
            Assert.AreEqual(2, (int)Math.Ceiling(wolf.Hp / N.Hero(HeroCode.Mage).Damage));
            Assert.AreEqual(12.8, giant.Hp / N.Hero(HeroCode.Mage).Dps, 1.28);
            // Đấu sĩ rìu: sát thương mỗi mục tiêu thấp nhất.
            Assert.AreEqual(1, BottomRank(N.Hero(HeroCode.Axeman), h => h.Dps));
        }

        [Test]
        public void EveryHeroGrowsByTheSameRatioForTheSamePrice()
        {
            foreach (int level in new[] { 2, 5, 9, 15 })
            {
                double ratio = S(HeroCode.Knight, level) / S(HeroCode.Knight);
                foreach (var h in N.Heroes)
                    Assert.AreEqual(ratio, M.HeroPower(h, level) / M.HeroPower(h), 1e-9, h.Name + " cấp " + level);
            }
            Assert.AreEqual(1.69, S(HeroCode.Archer, 5) / S(HeroCode.Archer), 0.01);
            Assert.AreEqual(2.45, S(HeroCode.Archer, 9) / S(HeroCode.Archer), 0.01);
            Assert.AreEqual(1.38, M.HeroDpsMul(3), 0.005);
            Assert.AreEqual(1.79, M.HeroDpsMul(5), 0.005);
            Assert.AreEqual(2.24, M.HeroDpsMul(7), 0.005);
            Assert.AreEqual(2.73, M.HeroDpsMul(9), 0.005);
            // Giá nâng hero chỉ là hàm của cấp (Price(UpgradeLine.Hero, cấp)), không có tham số hero.
        }

        [Test]
        public void MeleeOnlyTeamDealsAboutAsMuchAsTheMixedTeam()
        {
            double mono = TeamDpsLv1(BalanceSamples.MeleeTeam);
            double mixed = TeamDpsLv1(BalanceSamples.MixedTeam);
            Assert.AreEqual(54.9, mono, 0.05);
            Assert.AreEqual(52.0, mixed, 0.05);
            Assert.That(mono, Is.GreaterThanOrEqualTo(mixed * 0.95));
            Assert.AreEqual(62.9, TeamDpsLv1(HeroCode.Swordsman, HeroCode.Archer, HeroCode.Mage), 0.05);
            Assert.AreEqual(54.5, TeamDpsLv1(HeroCode.Knight, HeroCode.Axeman, HeroCode.Mage), 0.05);
        }

        [Test]
        public void AreaHeroesAreWeakerWhenTheirBlowHitsOnlyOneEnemy()
        {
            Assert.That(S(HeroCode.Axeman, 1, 1), Is.LessThan(S(HeroCode.Axeman)));
            Assert.That(S(HeroCode.Mage, 1, 1), Is.LessThan(S(HeroCode.Mage)));
            Assert.AreEqual(S(HeroCode.Knight), S(HeroCode.Knight, 1, 1), 1e-9, "một mục tiêu không đổi");
        }

        [Test]
        public void AreaHeroesShineWhenTheirBlowHitsMoreThanTwoEnemies()
        {
            Assert.That(S(HeroCode.Axeman, 1, 4), Is.GreaterThan(S(HeroCode.Axeman)));
            Assert.That(S(HeroCode.Mage, 1, 4), Is.GreaterThan(S(HeroCode.Mage)));
            Assert.That(S(HeroCode.Mage, 1, 4), Is.GreaterThan(N.Heroes.Max(h => M.HeroPower(h))));
            Assert.AreEqual(S(HeroCode.Swordsman), S(HeroCode.Swordsman, 1, 4), 1e-9);
        }

        [Test]
        public void RangedHeroLosesItsHpBonusWhenAWolfReachesIt()
        {
            Assert.AreEqual(120, M.HeroEffectiveHp(N.Hero(HeroCode.Archer), 1, reachedInMelee: true), 1e-9);
            Assert.That(S(HeroCode.Archer, 1, 2, reached: true), Is.LessThan(S(HeroCode.Archer)));
            CollectionAssert.Contains(Counters[HeroCode.Archer][1], EnemyKind.Wolf);
        }

        [Test]
        public void AboveLevelTwentyAttackSpeedStopsButHeroesStayEven()
        {
            Assert.AreEqual(1.6, M.HeroAttackSpeedMul(21), 1e-9);
            Assert.AreEqual(1.6, M.HeroAttackSpeedMul(25), 1e-9);
            // Cấp 21→22 chỉ còn máu và sát thương tăng.
            double expected = M.HeroStatMul(22) / M.HeroStatMul(21);
            foreach (var h in N.Heroes)
            {
                Assert.AreEqual(expected, M.HeroPower(h, 22) / M.HeroPower(h, 21), 1e-9, h.Name);
                Assert.AreEqual(M.HeroPower(N.Hero(HeroCode.Knight), 25) / M.HeroPower(N.Hero(HeroCode.Knight)),
                    M.HeroPower(h, 25) / M.HeroPower(h), 1e-9, h.Name + " cấp 25");
            }
        }
    }
}
