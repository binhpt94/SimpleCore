using System.Collections.Generic;
using NUnit.Framework;
using M = Game.Balance.Tests.BalanceMath;
using N = Game.Balance.Tests.BalanceNumbers;

namespace Game.Balance.Tests
{
    // feature-balance/balance-economy/income-and-prices: thu theo đợt và giá nâng cấp.
    public class BalanceEconomyTests
    {
        // Bảng "Thu theo đợt": K tiền quái, B thưởng, I thu, M đầu đợt (đợt 1..20).
        static readonly int[] K = { 40, 50, 57, 66, 96, 105, 128, 138, 147, 272, 187, 207, 216, 227, 248, 258, 278, 288, 297, 413 };
        static readonly int[] B = { 25, 30, 35, 40, 45, 50, 55, 60, 65, 70, 75, 80, 85, 90, 95, 100, 105, 110, 115, 120 };
        static readonly int[] I = { 65, 80, 92, 106, 141, 155, 183, 198, 212, 342, 262, 287, 301, 317, 343, 358, 383, 398, 412, 533 };
        static readonly int[] Mo = { 50, 115, 195, 287, 393, 534, 689, 872, 1070, 1282, 1624, 1886, 2173, 2474, 2791, 3134, 3492, 3875, 4273, 4685 };

        [Test]
        public void WaveIncomeIsEnemyGoldPlusClearBonusOfTheTable()
        {
            for (int w = 1; w <= 20; w++)
            {
                Assert.AreEqual(K[w - 1], M.KillGold(w), "đợt " + w + " tiền quái");
                Assert.AreEqual(B[w - 1], M.ClearBonus(w), "đợt " + w + " thưởng");
                Assert.AreEqual(I[w - 1], M.Income(w), "đợt " + w + " thu");
            }
        }

        [Test]
        public void EnemyGoldDoesNotScaleWithWave()
        {
            // Lính luôn 5 xu: đợt 1 có 8 Lính, 40 xu.
            Assert.AreEqual(8 * N.Enemy(EnemyKind.Soldier).Gold, M.KillGold(1));
            Assert.AreEqual(5.0, M.KillGold(1) / (double)M.EnemyCount(1), 1e-9);
            Assert.AreEqual(9.0, M.KillGold(20) / (double)M.EnemyCount(20), 0.05);
        }

        [Test]
        public void MoneyAtWaveStartIsStartMoneyPlusEarlierIncome()
        {
            for (int w = 1; w <= 20; w++) Assert.AreEqual(Mo[w - 1], M.MoneyAtStart(w), "đầu đợt " + w);
            Assert.AreEqual(393, M.MoneyAtStart(5));
            Assert.AreEqual(1282, M.MoneyAtStart(10));
            Assert.AreEqual(4685, M.MoneyAtStart(20));
        }

        [Test]
        public void EnemyGoldBeatsClearBonusEveryWave()
        {
            for (int w = 1; w <= 60; w++)
                Assert.That(M.KillGold(w), Is.GreaterThan(M.ClearBonus(w)), "đợt " + w);
            Assert.AreEqual(40, M.KillGold(1)); Assert.AreEqual(25, M.ClearBonus(1));
            Assert.AreEqual(413, M.KillGold(20)); Assert.AreEqual(120, M.ClearBonus(20));
        }

        [Test]
        public void NextLevelPriceGrowsByItsRateRoundedUp()
        {
            long[] hero = { 30, 41, 55, 74, 100, 135, 182, 246, 331 };
            long[] castle = { 40, 56, 79, 110, 154, 216, 302, 422, 591 };
            long[] skill = { 60, 90, 135, 203, 304, 456, 684, 1026, 1538 };
            for (int c = 1; c <= 9; c++)
            {
                Assert.AreEqual(hero[c - 1], M.Price(UpgradeLine.Hero, c), "hero từ cấp " + c);
                Assert.AreEqual(castle[c - 1], M.Price(UpgradeLine.CastleHp, c), "máu thành từ cấp " + c);
                Assert.AreEqual(castle[c - 1], M.Price(UpgradeLine.CastleAttack, c), "sức đánh thành từ cấp " + c);
                Assert.AreEqual(skill[c - 1], M.Price(UpgradeLine.Skill, c), "kĩ năng từ cấp " + c);
            }
            Assert.AreEqual(1194, M.CostToLevel(UpgradeLine.Hero, 10));
            Assert.AreEqual(1970, M.CostToLevel(UpgradeLine.CastleHp, 10));
            Assert.AreEqual(4496, M.CostToLevel(UpgradeLine.Skill, 10));
            Assert.AreEqual(863, M.CostToLevel(UpgradeLine.Hero, 9));
        }

        [Test]
        public void EveryWaveEarnsEnoughForTheCheapestUpgradeOnOffer()
        {
            for (int w = 1; w <= 20; w++)
                Assert.That(M.Income(w), Is.GreaterThanOrEqualTo(M.Price(UpgradeLine.Hero, 1)), "đợt " + w);
            foreach (var row in BalanceSamples.Rows())
                Assert.That(M.Income(row.Wave), Is.GreaterThanOrEqualTo(M.CheapestNext(row.Build)), "đợt " + row.Wave);
            // Đợt 20: thu 533 xu, hero cấp 9 lên 10 giá 331 xu.
            Assert.That(M.Income(20), Is.GreaterThanOrEqualTo(M.Price(UpgradeLine.Hero, 9)));
        }

        [Test]
        public void SampleBuildsSpendAlmostAllMoneyAtWaveStart()
        {
            foreach (var row in BalanceSamples.Rows())
            {
                Assert.AreEqual(row.Money, M.MoneyAtStart(row.Wave), "đầu đợt " + row.Wave);
                long cost = M.BuildCost(row.Build);
                Assert.That(cost, Is.LessThanOrEqualTo(row.Money), "đợt " + row.Wave + " mua quá tiền");
                if (row.Wave >= 5) Assert.That(cost, Is.GreaterThanOrEqualTo(0.85 * row.Money), "đợt " + row.Wave);
            }
        }

        [Test]
        public void MoneyUpToWaveTwentyNeverBuysEverything()
        {
            int total = M.MoneyAtStart(21);
            Assert.That(total, Is.InRange(5100, 5300), "khoảng 5200 xu");

            long everythingTo10 = 3 * M.CostToLevel(UpgradeLine.Hero, 10)
                + 2 * M.CostToLevel(UpgradeLine.CastleHp, 10) + 3 * M.CostToLevel(UpgradeLine.Skill, 10);
            Assert.That(everythingTo10, Is.InRange(20500, 21500), "khoảng 21 000 xu");
            Assert.That(everythingTo10 / (double)total, Is.InRange(3.5, 4.5), "gấp khoảng 4 lần");

            // Đủ cho 3 hero cấp 9 và thành cấp 5; không đủ cho 3 hero cấp 10 và thành cấp 8.
            long enough = 3 * M.CostToLevel(UpgradeLine.Hero, 9) + 2 * M.CostToLevel(UpgradeLine.CastleHp, 5);
            long tooMuch = 3 * M.CostToLevel(UpgradeLine.Hero, 10) + 2 * M.CostToLevel(UpgradeLine.CastleHp, 8);
            Assert.That(enough, Is.LessThanOrEqualTo(total));
            Assert.That(tooMuch, Is.GreaterThan(total));
        }

        static double HeroGainPerCoin(HeroCode code, int from) =>
            N.Hero(code).Dps * (M.HeroDpsMul(from + 1) - M.HeroDpsMul(from)) / M.Price(UpgradeLine.Hero, from);

        static double CastleAttackGainPerCoin(int from) =>
            (M.CastleShotDps(from + 1) - M.CastleShotDps(from)) / M.Price(UpgradeLine.CastleAttack, from);

        static double ThunderGainPerCoin(int from, int hits) =>
            (M.ThunderDps(from + 1, hits) - M.ThunderDps(from, hits)) / M.Price(UpgradeLine.Skill, from);

        [Test]
        public void ValuePerCoinDropsAsLevelRises()
        {
            Assert.AreEqual(0.11, HeroGainPerCoin(HeroCode.Archer, 1), 0.01);
            Assert.AreEqual(0.07, HeroGainPerCoin(HeroCode.Knight, 1), 0.01);
            Assert.AreEqual(0.05, HeroGainPerCoin(HeroCode.Archer, 4), 0.005);
            Assert.AreEqual(0.05, CastleAttackGainPerCoin(1), 0.005);
            Assert.AreEqual(0.018, CastleAttackGainPerCoin(4), 0.002);
            Assert.AreEqual(0.053, ThunderGainPerCoin(1, 4), 0.003);
            Assert.AreEqual(3.75, N.CastleHpPerLevel / M.Price(UpgradeLine.CastleHp, 1), 1e-9);

            foreach (var h in N.Heroes)
                for (int c = 1; c < 30; c++)
                    Assert.That(HeroGainPerCoin(h.Code, c + 1), Is.LessThan(HeroGainPerCoin(h.Code, c)), h.Name + " cấp " + c);
            for (int c = 1; c < 30; c++)
            {
                Assert.That(CastleAttackGainPerCoin(c + 1), Is.LessThan(CastleAttackGainPerCoin(c)));
                Assert.That(ThunderGainPerCoin(c + 1, 5), Is.LessThan(ThunderGainPerCoin(c, 5)));
            }
        }

        [Test]
        public void LosingMidWaveGivesOnlyTheGoldOfKilledEnemiesWithoutClearBonus()
        {
            var killed = new Dictionary<EnemyKind, int>(M.Composition(5));
            killed[EnemyKind.Soldier] -= 1; // một Lính còn sống khi thành vỡ
            int got = M.IncomeIfNotCleared(killed);
            Assert.AreEqual(M.KillGold(5) - N.Enemy(EnemyKind.Soldier).Gold, got);
            Assert.That(got, Is.LessThan(M.Income(5) - M.ClearBonus(5) + 1));
            Assert.That(got, Is.LessThan(M.Income(5)), "bảng thu là mức trần");
        }

        [Test]
        public void IncomeDropsOnlyRightAfterABossWave()
        {
            Assert.AreEqual(342, M.Income(10));
            Assert.AreEqual(262, M.Income(11));
            for (int w = 1; w < 60; w++)
            {
                if (M.Income(w + 1) < M.Income(w))
                    Assert.IsTrue(M.IsBossWave(w), "thu giảm sau đợt " + w + " không phải đợt Trùm");
                else
                    Assert.IsFalse(M.IsBossWave(w), "đợt " + (w + 1) + " sau Trùm phải thu ít hơn");
            }
        }

        [Test]
        public void KeptMoneyEarnsNoInterestAndIsNotLost()
        {
            // Không tiêu gì: tiền đầu đợt sau đúng bằng tiền đầu đợt này cộng thu của đợt, không thêm không bớt.
            for (int w = 1; w < 30; w++)
                Assert.AreEqual(M.MoneyAtStart(w) + M.Income(w), M.MoneyAtStart(w + 1), "đợt " + w);
        }

        [Test]
        public void VeryHighLevelPricesFollowTheFormulaWithoutCap()
        {
            long p = M.Price(UpgradeLine.Hero, 29);
            Assert.That(p, Is.InRange(130000L, 137000L), "hero 29 lên 30 khoảng 133 000 xu");
            for (int c = 1; c < 60; c++)
            {
                Assert.That(M.Price(UpgradeLine.Hero, c + 1), Is.GreaterThan(M.Price(UpgradeLine.Hero, c)));
                Assert.That(M.Price(UpgradeLine.Skill, c + 1), Is.GreaterThan(M.Price(UpgradeLine.Skill, c)));
            }
        }
    }
}
