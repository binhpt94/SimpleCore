using System.Collections.Generic;
using NUnit.Framework;
using M = Game.Balance.Tests.BalanceMath;
using N = Game.Balance.Tests.BalanceNumbers;

namespace Game.Balance.Tests
{
    // feature-balance/balance-config/number-table: bảng đối chiếu bộ số chuẩn.
    public class BalanceNumberTableTests
    {
        const double Tol = 1e-6;

        static void AssertHero(HeroCode code, double hp, double dmg, double sec, double range, double speed,
            bool area, double dps)
        {
            var h = N.Hero(code);
            Assert.AreEqual(hp, h.Hp, Tol, h.Name + " máu");
            Assert.AreEqual(dmg, h.Damage, Tol, h.Name + " sát thương");
            Assert.AreEqual(sec, h.AttackSeconds, Tol, h.Name + " s mỗi đòn");
            Assert.AreEqual(range, h.Range, Tol, h.Name + " tầm");
            Assert.AreEqual(speed, h.Speed, Tol, h.Name + " đi");
            Assert.AreEqual(area, h.Area, h.Name + " đòn vùng");
            Assert.AreEqual(dps, h.Dps, 0.05, h.Name + " sát thương mỗi giây");
        }

        [Test]
        public void FiveHeroesHaveTheLevelOneStatsOfTheTable()
        {
            Assert.AreEqual(5, N.Heroes.Length);
            AssertHero(HeroCode.Knight, 300, 12, 1.0, 1.2, 2.0, false, 12.0);
            AssertHero(HeroCode.Axeman, 240, 14, 1.4, 1.3, 2.4, true, 10.0);
            AssertHero(HeroCode.Swordsman, 180, 16, 0.7, 1.2, 3.5, false, 22.9);
            AssertHero(HeroCode.Archer, 120, 14, 0.8, 7, 3.0, false, 17.5);
            AssertHero(HeroCode.Mage, 100, 18, 1.6, 6, 2.6, true, 11.25);
            Assert.AreEqual(1.5, N.Hero(HeroCode.Axeman).AreaRadius, Tol);
            Assert.AreEqual(1.5, N.Hero(HeroCode.Mage).AreaRadius, Tol);
        }

        [Test]
        public void HeroCommonRulesMatchTheTable()
        {
            Assert.AreEqual(3, N.TeamSize);
            Assert.AreEqual(8, N.HeroRespawn, Tol);
            Assert.AreEqual(15, N.HeroReachX, Tol);
            // Mỗi cấp +15% máu và sát thương lv1; tốc độ đánh +3% mỗi cấp, tối đa +60% ở cấp 21.
            Assert.AreEqual(1.15, M.HeroStatMul(2), Tol);
            Assert.AreEqual(2.35, M.HeroStatMul(10), Tol);
            Assert.AreEqual(1.03, M.HeroAttackSpeedMul(2), Tol);
            Assert.AreEqual(1.60, M.HeroAttackSpeedMul(21), Tol);
            Assert.AreEqual(1.60, M.HeroAttackSpeedMul(30), Tol);
            foreach (var h in N.Heroes) Assert.That(h.Speed, Is.InRange(2.0, 3.5), h.Name);
        }

        [Test]
        public void CastleHasThousandHpAndShootsTenEverySecondWithinSixUnits()
        {
            Assert.AreEqual(1000, N.CastleHp, Tol);
            Assert.AreEqual(10, N.CastleShotDamage, Tol);
            Assert.AreEqual(1.0, N.CastleShotSeconds, Tol);
            Assert.AreEqual(6, N.CastleShotRange, Tol);
            Assert.AreEqual(1, N.CastleShotTargets);
            Assert.AreEqual(0, N.CastleX, Tol);
            Assert.AreEqual(3, N.CastleWidth, Tol);
            Assert.AreEqual(4, N.CastleHeight, Tol);
            Assert.AreEqual(N.CastleWidth / 2, N.CastleEdge, Tol);
        }

        [Test]
        public void CastleSkillsAndLevelGainsMatchTheTable()
        {
            Assert.AreEqual(80, N.ThunderDamage, Tol);
            Assert.AreEqual(6, N.ThunderRange, Tol);
            Assert.AreEqual(20, N.ThunderCooldown, Tol);
            Assert.AreEqual(0.5, N.IceSlow, Tol);
            Assert.AreEqual(5, N.IceSeconds, Tol);
            Assert.AreEqual(35, N.IceCooldown, Tol);
            Assert.AreEqual(0.30, N.CheerHeal, Tol);
            Assert.AreEqual(25, N.CheerCooldown, Tol);

            // Máu thành +150 mỗi cấp; sức đánh +2 (20% của 10) mỗi cấp.
            Assert.AreEqual(1150, M.CastleMaxHp(2), Tol);
            Assert.AreEqual(1900, M.CastleMaxHp(7), Tol);
            Assert.AreEqual(12, M.CastleShotDamage(2), Tol);
            Assert.AreEqual(18, M.CastleShotDamage(5), Tol);

            // Hiệu lực kĩ năng +20% lv1 mỗi cấp: Sấm 80→96→112, Băng 5→6→7 s (chậm giữ 50%), Cổ vũ 30→36→42%.
            Assert.AreEqual(96, M.SkillEffect(N.ThunderDamage, 2), Tol);
            Assert.AreEqual(112, M.SkillEffect(N.ThunderDamage, 3), Tol);
            Assert.AreEqual(6, M.SkillEffect(N.IceSeconds, 2), Tol);
            Assert.AreEqual(7, M.SkillEffect(N.IceSeconds, 3), Tol);
            Assert.AreEqual(0.36, M.SkillEffect(N.CheerHeal, 2), Tol);
            Assert.AreEqual(0.42, M.SkillEffect(N.CheerHeal, 3), Tol);
        }

        [Test]
        public void FiveEnemyTypesHaveBaseStatsAndGoldOfTheTable()
        {
            void Check(EnemyKind k, double hp, double dmg, double sec, double range, double speed, int gold, double dps)
            {
                var e = N.Enemy(k);
                Assert.AreEqual(hp, e.Hp, Tol, e.Name + " máu");
                Assert.AreEqual(dmg, e.Damage, Tol, e.Name + " sát thương");
                Assert.AreEqual(sec, e.AttackSeconds, Tol, e.Name + " s mỗi đòn");
                Assert.AreEqual(range, e.Range, Tol, e.Name + " tầm");
                Assert.AreEqual(speed, e.Speed, Tol, e.Name + " đi");
                Assert.AreEqual(gold, e.Gold, e.Name + " tiền");
                Assert.AreEqual(dps, e.Dps, 0.01, e.Name + " sát thương mỗi giây");
            }

            Assert.AreEqual(5, N.Enemies.Length);
            Check(EnemyKind.Soldier, 40, 5, 1.0, 1.0, 1.5, 5, 5.0);
            Check(EnemyKind.Wolf, 20, 3, 0.8, 1.0, 3.0, 4, 3.75);
            Check(EnemyKind.Giant, 150, 15, 1.5, 1.2, 0.8, 15, 10.0);
            Check(EnemyKind.EnemyArcher, 25, 6, 1.2, 5, 1.5, 6, 5.0);
            Check(EnemyKind.Boss, 400, 40, 2.0, 1.5, 0.6, 100, 20.0);
        }

        [Test]
        public void WaveParametersMatchTheTable()
        {
            Assert.AreEqual(8, M.EnemyCount(1));
            Assert.AreEqual(26, M.EnemyCount(10));
            Assert.AreEqual(46, M.EnemyCount(20));
            Assert.AreEqual(1.0, M.Interval(1), Tol);
            Assert.AreEqual(0.8, M.Interval(5), Tol);
            Assert.AreEqual(0.55, M.Interval(10), Tol);
            Assert.AreEqual(0.3, M.Interval(15), Tol);
            Assert.AreEqual(0.3, M.Interval(40), Tol);
            Assert.AreEqual(1.9, M.Scale(10), Tol);
            Assert.AreEqual(2.9, M.Scale(20), Tol);

            Assert.AreEqual(1, N.UnlockWave(EnemyKind.Soldier));
            Assert.AreEqual(3, N.UnlockWave(EnemyKind.Wolf));
            Assert.AreEqual(5, N.UnlockWave(EnemyKind.Giant));
            Assert.AreEqual(7, N.UnlockWave(EnemyKind.EnemyArcher));
            Assert.AreEqual(10, N.BossEvery);
            Assert.AreEqual(40, N.AliveCap);
            Assert.AreEqual(3, N.GapSeconds, Tol);
            Assert.AreEqual(25, M.ClearBonus(1));
            Assert.AreEqual(120, M.ClearBonus(20));
        }

        [Test]
        public void EachWaveHasTheRightCountMixAndOneBossEveryTenWaves()
        {
            for (int w = 1; w <= 40; w++)
            {
                var c = M.Composition(w);
                int total = 0;
                foreach (var kv in c)
                {
                    Assert.That(kv.Value, Is.GreaterThanOrEqualTo(0), "đợt " + w + " " + kv.Key);
                    total += kv.Value;
                }
                Assert.AreEqual(M.EnemyCount(w), total, "đợt " + w + " tổng số con");
                int boss = c.TryGetValue(EnemyKind.Boss, out var b) ? b : 0;
                Assert.AreEqual(w % 10 == 0 ? 1 : 0, boss, "đợt " + w + " số Trùm");
                foreach (EnemyKind k in new[] { EnemyKind.Wolf, EnemyKind.Giant, EnemyKind.EnemyArcher })
                {
                    bool open = w >= N.UnlockWave(k);
                    int n = c.TryGetValue(k, out var x) ? x : 0;
                    if (open) Assert.That(n, Is.GreaterThanOrEqualTo(1), "đợt " + w + " " + k + " đã mở");
                    else Assert.AreEqual(0, n, "đợt " + w + " " + k + " chưa mở");
                }
            }
            foreach (var t in N.MixTiers) Assert.That(t[1] + t[2] + t[3], Is.LessThan(100));
        }

        [Test]
        public void UpgradePricesAndStartMoneyMatchTheTable()
        {
            Assert.AreEqual(30, M.Price(UpgradeLine.Hero, 1));
            Assert.AreEqual(41, M.Price(UpgradeLine.Hero, 2));
            Assert.AreEqual(40, M.Price(UpgradeLine.CastleHp, 1));
            Assert.AreEqual(56, M.Price(UpgradeLine.CastleHp, 2));
            Assert.AreEqual(40, M.Price(UpgradeLine.CastleAttack, 1));
            Assert.AreEqual(56, M.Price(UpgradeLine.CastleAttack, 2));
            Assert.AreEqual(60, M.Price(UpgradeLine.Skill, 1));
            Assert.AreEqual(90, M.Price(UpgradeLine.Skill, 2));
            Assert.AreEqual(0, N.MaxLevel, "không có cấp tối đa");
            Assert.AreEqual(50, N.StartMoney);
        }

        [Test]
        public void EveryNumberHasOneOwnerAUnitAndNumbersToReviewOnChange()
        {
            var keys = new HashSet<string>();
            foreach (var e in N.Table)
            {
                Assert.IsTrue(keys.Add(e.Key), "số " + e.Key + " ghi hai lần (hai nơi sở hữu)");
                Assert.IsFalse(string.IsNullOrEmpty(e.Unit), e.Key + " thiếu đơn vị");
                if (e.Key == "upgrade.max-level") continue; // bảng để trống cột này
                Assert.That(e.ReviewOnChange.Length, Is.GreaterThan(0), e.Key + " thiếu số phải xem lại");
            }
            foreach (var e in N.Table)
                foreach (var link in e.ReviewOnChange)
                    Assert.IsTrue(link.StartsWith("feature-") || N.Entry(link) != null,
                        e.Key + " trỏ tới số không có: " + link);
        }

        [Test]
        public void ChangingEnemyGoldMeansReviewingPricesAndTheBreakForecast()
        {
            var gold = N.Entry("enemy.gold");
            CollectionAssert.Contains(gold.ReviewOnChange, "upgrade.hero-price");
            CollectionAssert.Contains(gold.ReviewOnChange, N.BreakNode);
            CollectionAssert.Contains(N.Entry("hero.stats").ReviewOnChange, N.HeroCompareNode);
            CollectionAssert.Contains(N.Entry("wave.clear-bonus").ReviewOnChange, N.IncomeNode);
        }

        [Test]
        public void NumbersBelongToTheOwningFeature()
        {
            Assert.AreEqual(Owner.Hero, N.Entry("hero.stats").Owner);
            Assert.AreEqual(Owner.Castle, N.Entry("castle.hp").Owner);
            Assert.AreEqual(Owner.Enemy, N.Entry("enemy.stats").Owner);
            Assert.AreEqual(Owner.Enemy, N.Entry("enemy.gold").Owner);
            Assert.AreEqual(Owner.Wave, N.Entry("wave.scale").Owner, "hệ số đợt ở feature-wave, không ở feature-enemy");
            Assert.AreEqual(Owner.Wave, N.Entry("wave.clear-bonus").Owner);
            Assert.AreEqual(Owner.Upgrade, N.Entry("upgrade.skill-price").Owner);
            Assert.AreEqual(Owner.Wallet, N.Entry("wallet.start").Owner);
        }

        [Test]
        public void NumberWithoutClearOwnerLikeTheVisibleStripGoesToCastle()
        {
            Assert.AreEqual(Owner.Castle, N.Entry("map.strip").Owner);
            Assert.AreEqual(-16, N.StripLeft, Tol);
            Assert.AreEqual(16, N.StripRight, Tol);
        }

        [Test]
        public void CastleEdgeNeededByTwoFeaturesIsHeldOnlyByCastle()
        {
            // Mép thành x = ±1.5 dùng cho tầm bắn và cho quái dừng lại: chỉ feature-castle giữ.
            Assert.AreEqual(Owner.Castle, N.Entry("castle.body").Owner);
            int holders = 0;
            foreach (var e in N.Table) if (e.Key.Contains("edge") || e.Key == "castle.body") holders++;
            Assert.AreEqual(1, holders);
            Assert.AreEqual(1.5, N.CastleEdge, Tol);
        }
    }
}
