using System.Linq;
using NUnit.Framework;
using M = Game.Balance.Tests.BalanceMath;
using N = Game.Balance.Tests.BalanceNumbers;

namespace Game.Balance.Tests
{
    // feature-balance/balance-difficulty-curve/break-forecast: dự đoán đợt gãy.
    public class BalanceBreakForecastTests
    {
        static double P(int w) => M.Pressure(BalanceSamples.Row(w).Build, w);

        [Test]
        public void PressureIsEnemyHpOverTeamDamageDuringTheSpawn()
        {
            foreach (var r in BalanceSamples.Rows())
            {
                string at = "đợt " + r.Wave;
                Assert.AreEqual(r.H, M.WaveHp(r.Wave), 1.0, at + " H");
                Assert.AreEqual(r.T, M.SpawnDuration(r.Wave), 1e-6, at + " T");
                Assert.AreEqual(M.WaveHp(r.Wave) / (M.TeamDps(r.Build, r.Wave) * M.SpawnDuration(r.Wave)),
                    M.Pressure(r.Build, r.Wave), 1e-9, at + " công thức");
                Assert.AreEqual(r.P, M.Pressure(r.Build, r.Wave), 0.05, at + " P");
                Assert.AreEqual(r.CastleMaxHp, M.CastleMaxHp(r.Build.CastleHp), 1e-9, at + " máu thành");
                Assert.AreEqual(r.HeroHp, M.TeamHeroHp(r.Build), r.HeroHp * 0.02, at + " máu 3 hero");
                Assert.AreEqual(r.E, M.WaveEnemyDps(r.Wave), 1.0, at + " E");
            }
        }

        [Test]
        public void TeamDpsIsHeroesWithAreaHittingTwoPlusCastleShotPlusThunderOverTwentySeconds()
        {
            var fresh = TeamBuild.Fresh(BalanceSamples.MixedTeam);
            Assert.AreEqual(52.0, M.TeamHeroDps(fresh), 1e-9, "12 + 17.5 + 11.25×2");
            Assert.AreEqual(10.0, M.CastleShotDps(1), 1e-9);
            Assert.AreEqual(16.0, M.ThunderDps(1, 4), 1e-9, "80 × 4 con ÷ 20 s");
            Assert.AreEqual(78.0, M.TeamDps(fresh, 1), 1e-9);

            Assert.AreEqual(4, M.ThunderHits(1)); Assert.AreEqual(4, M.ThunderHits(5));
            Assert.AreEqual(5, M.ThunderHits(6)); Assert.AreEqual(5, M.ThunderHits(15));
            Assert.AreEqual(6, M.ThunderHits(16)); Assert.AreEqual(6, M.ThunderHits(30));

            foreach (var r in BalanceSamples.Rows())
                Assert.AreEqual(r.D, M.TeamDps(r.Build, r.Wave), 1.0, "đợt " + r.Wave + " D");
        }

        [Test]
        public void EarlyWavesOneToNineStayUnderPressureOnePointOne()
        {
            Assert.AreEqual(0.6, P(1), 0.05);
            Assert.AreEqual(0.9, P(5), 0.05);
            Assert.AreEqual(1.1, P(9), 0.05);
            foreach (var r in BalanceSamples.Rows().Where(x => x.Wave <= 9))
                Assert.That(M.Pressure(r.Build, r.Wave), Is.LessThan(1.1), "đợt " + r.Wave);
        }

        [Test]
        public void PressureRisesFromAboutOnePointFiveToTwoPointFiveOverWavesElevenToFifteen()
        {
            Assert.AreEqual(1.5, P(11), 0.1);
            Assert.AreEqual(2.5, P(15), 0.1);
            Assert.That(P(15), Is.GreaterThan(P(11)));
            Assert.That(P(11), Is.GreaterThan(1.0), "thành bắt đầu mất máu");
            Assert.AreEqual(N.IntervalFloor, M.Interval(15), 1e-9, "nhịp thả chạm sàn ở đợt 15");
            Assert.That(M.Interval(14), Is.GreaterThan(N.IntervalFloor));
        }

        [Test]
        public void NormalPlayerBreaksAroundWavesFifteenToNineteen()
        {
            // Thước gãy: P từ khoảng 2.4 trở lên (thành không trụ được nếu kéo dài vài đợt).
            Assert.That(P(11), Is.LessThan(2.4));
            Assert.That(P(15), Is.InRange(2.4, 2.6));
            Assert.That(P(19), Is.InRange(2.4, 2.6));
            Assert.AreEqual(2.9, P(20), 0.1, "đợt 20 Trùm gần như chắc gãy");
            Assert.That(P(20), Is.GreaterThan(P(19)));
            int firstBreak = BalanceSamples.Rows().First(r => M.Pressure(r.Build, r.Wave) >= 2.4).Wave;
            Assert.That(firstBreak, Is.InRange(15, 19));
        }

        // P của đợt w: đợt mẫu tính bằng công thức; đợt nằm giữa hai đợt mẫu lấy nội suy tuyến tính.
        static double InterpolatedP(int w)
        {
            var rows = BalanceSamples.Rows();
            for (int i = 0; i < rows.Length - 1; i++)
            {
                var a = rows[i];
                var b = rows[i + 1];
                if (w < a.Wave || w > b.Wave) continue;
                double pa = M.Pressure(a.Build, a.Wave), pb = M.Pressure(b.Build, b.Wave);
                return pa + (pb - pa) * (w - a.Wave) / (b.Wave - a.Wave);
            }
            throw new System.ArgumentOutOfRangeException(nameof(w));
        }

        // Một đợt mất T × (1 + P) (thả cộng dọn quái tồn) cộng lúc thở.
        static double WaveSeconds(int w) => M.SpawnDuration(w) * (1 + InterpolatedP(w)) + N.GapSeconds;

        static double MinutesToEndOf(int lastWave)
        {
            double s = 0;
            for (int w = 1; w <= lastWave; w++) s += WaveSeconds(w);
            return s / 60;
        }

        [Test]
        public void NormalRunThatBreaksInWavesFifteenToNineteenLastsSevenToTenAndAHalfMinutes()
        {
            Assert.AreEqual(14, WaveSeconds(1), 1.0, "đợt 1");
            Assert.AreEqual(26, WaveSeconds(5), 1.0, "đợt 5");
            Assert.AreEqual(41, WaveSeconds(10), 1.0, "đợt 10");
            for (int w = 15; w <= 19; w++) Assert.That(WaveSeconds(w), Is.InRange(39.0, 48.0), "đợt " + w);

            Assert.AreEqual(3.7, MinutesToEndOf(9), 0.15, "hết đợt 9");
            Assert.AreEqual(7.0, MinutesToEndOf(14), 0.15, "hết đợt 14");
            Assert.AreEqual(9.8, MinutesToEndOf(18), 0.15, "hết đợt 18");
            Assert.AreEqual(10.6, MinutesToEndOf(19), 0.15, "hết đợt 19");

            // Thua giữa đợt 15 tới giữa đợt 19.
            double early = MinutesToEndOf(14) + WaveSeconds(15) / 2 / 60;
            double late = MinutesToEndOf(18) + WaveSeconds(19) / 2 / 60;
            Assert.That(early, Is.InRange(7.0 - 0.15, 10.5));
            Assert.That(late, Is.InRange(7.0, 10.5 + 0.15));
        }

        [Test]
        public void PressureIgnoresIceAndCheer()
        {
            var r = BalanceSamples.Row(15);
            var b = r.Build;
            var moreSupport = new TeamBuild(b.Heroes, b.HeroLevels, b.CastleHp, b.CastleAttack, b.Thunder, b.Ice + 5, b.Cheer + 5);
            Assert.AreEqual(M.Pressure(b, 15), M.Pressure(moreSupport, 15), 1e-12);
            // Máu thành cũng không vào P.
            var moreHp = new TeamBuild(b.Heroes, b.HeroLevels, b.CastleHp + 5, b.CastleAttack, b.Thunder, b.Ice, b.Cheer);
            Assert.AreEqual(M.Pressure(b, 15), M.Pressure(moreHp, 15), 1e-12);
        }

        [Test]
        public void AliveCapFromWaveSeventeenStretchesSpawnAndLowersRealPressure()
        {
            Assert.That(M.EnemyCount(16), Is.LessThan(N.AliveCap));
            Assert.That(M.EnemyCount(17), Is.GreaterThanOrEqualTo(N.AliveCap));
            var r = BalanceSamples.Row(19);
            double table = M.Pressure(r.Build, 19);
            double delayed = M.Pressure(r.Build, 19, M.SpawnDuration(19) * 1.2);
            Assert.That(delayed, Is.LessThan(table));
        }

        [Test]
        public void MeleeOnlyTeamMatchesTheSampleEarlyButMustChaseEnemyArchersFromWaveSeven()
        {
            var mono = TeamBuild.Fresh(BalanceSamples.MeleeTeam);
            Assert.AreEqual(54.9, M.TeamHeroDps(mono), 0.05);
            Assert.That(M.Pressure(mono, 1), Is.LessThan(1.0));
            Assert.That(M.Pressure(mono, 1), Is.LessThanOrEqualTo(P(1)));
            var archerFoe = N.Enemy(EnemyKind.EnemyArcher);
            foreach (var code in BalanceSamples.MeleeTeam)
                Assert.That(N.Hero(code).Range, Is.LessThan(archerFoe.Range), N.Hero(code).Name);
            Assert.IsFalse(M.Composition(6).ContainsKey(EnemyKind.EnemyArcher));
            Assert.That(M.Composition(7)[EnemyKind.EnemyArcher], Is.GreaterThanOrEqualTo(1));
        }

        // ---------- Khoảng cách khéo và vụng, ba cách chơi để chơi thử ----------
        static ShopLevels L(int k, int a, int m, int hp = 1, int atk = 1, int th = 1, int ice = 1, int cheer = 1) =>
            new ShopLevels(k, a, m, hp, atk, th, ice, cheer);

        [Test]
        public void SkillGapTargetIsClumsyTenToTwelveSkilledSixteenToNineteenAtLeastFourApart()
        {
            Assert.AreEqual(10, BalancePlayStyles.ClumsyBreakMin);
            Assert.AreEqual(12, BalancePlayStyles.ClumsyBreakMax);
            Assert.AreEqual(16, BalancePlayStyles.SkilledBreakMin);
            Assert.AreEqual(19, BalancePlayStyles.SkilledBreakMax);
            Assert.AreEqual(4, BalancePlayStyles.PrototypeMinGap);
            Assert.AreEqual(6, BalancePlayStyles.FullMinGap);
            Assert.AreEqual(5, BalancePlayStyles.RunsPerStyle);
            Assert.That(BalancePlayStyles.SkilledBreakMin - BalancePlayStyles.ClumsyBreakMax,
                Is.GreaterThanOrEqualTo(BalancePlayStyles.PrototypeMinGap), "hai khoảng cách nhau đủ 4 đợt");

            // Vụng gãy quanh đợt Trùm đầu tiên, chỗ P vọt từ 1.1 lên 1.8.
            Assert.IsTrue(M.IsBossWave(10));
            Assert.That(10, Is.InRange(BalancePlayStyles.ClumsyBreakMin, BalancePlayStyles.ClumsyBreakMax));
            // Khéo gãy trước đợt Trùm 20 (P ≈ 2.9 cả với cách chơi thường), mục tiêu cũ 20–25 đã bỏ.
            Assert.That(BalancePlayStyles.SkilledBreakMax, Is.LessThan(20));
            Assert.AreEqual(2.9, P(20), 0.1);
            // Khoảng khéo nằm trong khoảng gãy của người chơi bình thường (15–19), vụng nằm dưới hẳn.
            Assert.That(BalancePlayStyles.SkilledBreakMin, Is.GreaterThanOrEqualTo(15));
            Assert.That(BalancePlayStyles.ClumsyBreakMax, Is.LessThan(15));
        }

        [Test]
        public void SkillGapIsMeasuredByMedianBreakWaveOverAtLeastFiveRunsPerStyle()
        {
            // Đợt chơi thử 20261126-010423 (bot khéo cũ mua món rẻ nhất): vụng 12, 10; khéo 12, 13, 13.
            int[] oldClumsy = { 12, 10 }, oldSkilled = { 12, 13, 13 };
            Assert.AreEqual(11, BalancePlayStyles.Median(oldClumsy), 1e-9);
            Assert.AreEqual(13, BalancePlayStyles.Median(oldSkilled), 1e-9);
            Assert.IsFalse(BalancePlayStyles.GapReached(oldClumsy, oldSkilled, BalancePlayStyles.PrototypeMinGap),
                "cách 2 đợt và thiếu lượt: không đạt");

            int[] clumsy = { 10, 11, 10, 12, 11 }, skilled = { 16, 15, 17, 18, 16 };
            Assert.AreEqual(11, BalancePlayStyles.Median(clumsy), 1e-9);
            Assert.AreEqual(16, BalancePlayStyles.Median(skilled), 1e-9);
            Assert.IsTrue(BalancePlayStyles.GapReached(clumsy, skilled, BalancePlayStyles.PrototypeMinGap));
            Assert.IsFalse(BalancePlayStyles.GapReached(clumsy, skilled, BalancePlayStyles.FullMinGap),
                "FULL đòi ít nhất 6 đợt");
            Assert.IsFalse(BalancePlayStyles.GapReached(clumsy.Take(4).ToArray(), skilled,
                BalancePlayStyles.PrototypeMinGap), "4 lượt là thiếu");
            Assert.AreEqual(10.5, BalancePlayStyles.Median(10, 11), 1e-9);
        }

        [Test]
        public void ClumsyBuysTheCheapestItemWheneverItHasTheMoney()
        {
            Assert.IsTrue(BalancePlayStyles.BuysNow(PlayStyle.Clumsy, false, 30, 30), "giữa đợt vẫn mua");
            Assert.IsFalse(BalancePlayStyles.BuysNow(PlayStyle.Clumsy, false, 29, 30));
            // Lv1: hero 30 xu rẻ nhất, hoà thì Hiệp sĩ trước.
            Assert.AreEqual(BuyItem.Knight, BalancePlayStyles.ClumsyNext(ShopLevels.Fresh()));
            // Hiệp sĩ cấp 2 thành 41 xu: tới Cung thủ 30 xu.
            Assert.AreEqual(BuyItem.Archer, BalancePlayStyles.ClumsyNext(L(2, 1, 1)));
            // Ba hero cấp 2 (41 xu): máu thành 40 xu rẻ hơn.
            Assert.AreEqual(BuyItem.CastleHp, BalancePlayStyles.ClumsyNext(L(2, 2, 2)));
        }

        [Test]
        public void NormalBuysOnlyAtTheStartOfTheBreatherAndSplitsFiftyFiveFortyFive()
        {
            Assert.IsFalse(BalancePlayStyles.BuysNow(PlayStyle.Normal, false, 10000, 30), "giữa đợt không mua");
            Assert.IsTrue(BalancePlayStyles.BuysNow(PlayStyle.Normal, true, 100, 41));
            Assert.AreEqual(0.55, BalancePlayStyles.NormalHeroShare, 1e-9);
        }

        [Test]
        public void SkilledBuysByThePriorityOrderAndNeverIdlesMoreThanThreeSeconds()
        {
            Assert.AreEqual(3, BalancePlayStyles.SkilledMaxIdleSeconds, 1e-9);
            Assert.IsTrue(BalancePlayStyles.BuysNow(PlayStyle.Skilled, false, 41, 41));
            Assert.IsFalse(BalancePlayStyles.BuysNow(PlayStyle.Skilled, false, 40, 41));

            // 1. Máu thành dưới 50% thì sửa thành trước mọi thứ.
            Assert.AreEqual(BuyItem.CastleHp, BalancePlayStyles.SkilledNext(L(2, 2, 2), 0.49));
            // 2. Pháp sư khi cấp nó không cao hơn hero thấp nhất quá 1.
            Assert.AreEqual(BuyItem.Mage, BalancePlayStyles.SkilledNext(ShopLevels.Fresh(), 0.5));
            Assert.AreEqual(BuyItem.Mage, BalancePlayStyles.SkilledNext(L(2, 2, 3), 1.0));
            // 3. Sức đánh thành khi thấp hơn cấp Pháp sư trừ 2.
            Assert.AreEqual(BuyItem.CastleAttack, BalancePlayStyles.SkilledNext(L(2, 2, 4), 1.0));
            // 4. Sấm sét khi thấp hơn cấp Pháp sư chia 3 làm tròn lên (4 / 3 → 2).
            Assert.AreEqual(BuyItem.Thunder, BalancePlayStyles.SkilledNext(L(2, 2, 4, atk: 2), 1.0));
            // 5. Hero cấp thấp nhất, hoà thì Hiệp sĩ trước.
            Assert.AreEqual(BuyItem.Knight, BalancePlayStyles.SkilledNext(L(2, 2, 4, atk: 2, th: 2), 1.0));
            Assert.AreEqual(BuyItem.Archer, BalancePlayStyles.SkilledNext(L(3, 2, 4, atk: 2, th: 2), 1.0));
        }

        [Test]
        public void SkilledSavesForIceBeforeBossWavesWhileIceIsLevelOne()
        {
            Assert.AreEqual(60, BalancePlayStyles.SkilledReserve(9, ShopLevels.Fresh()), "giá Băng giá cấp 2");
            Assert.AreEqual(60, BalancePlayStyles.SkilledReserve(19, ShopLevels.Fresh()));
            Assert.AreEqual(0, BalancePlayStyles.SkilledReserve(8, ShopLevels.Fresh()));
            Assert.AreEqual(0, BalancePlayStyles.SkilledReserve(10, ShopLevels.Fresh()));
            Assert.AreEqual(0, BalancePlayStyles.SkilledReserve(9, L(1, 1, 1, ice: 2)), "Băng giá đã cấp 2");
        }

        [Test]
        public void ThunderIsPressedOnReadyWhenClumsyOnFourNearWhenNormalOnSixNearWhenSkilled()
        {
            Assert.IsTrue(BalancePlayStyles.PressThunder(PlayStyle.Clumsy, 8, 0, 1.0, false));
            Assert.IsTrue(BalancePlayStyles.PressThunder(PlayStyle.Normal, 8, 4, 1.0, false));
            Assert.IsFalse(BalancePlayStyles.PressThunder(PlayStyle.Normal, 8, 3, 1.0, false));
            Assert.IsTrue(BalancePlayStyles.PressThunder(PlayStyle.Skilled, 3, 4, 1.0, false), "đợt 1–5: từ 4");
            Assert.IsFalse(BalancePlayStyles.PressThunder(PlayStyle.Skilled, 8, 5, 1.0, false));
            Assert.IsTrue(BalancePlayStyles.PressThunder(PlayStyle.Skilled, 8, 6, 1.0, false));
            Assert.IsTrue(BalancePlayStyles.PressThunder(PlayStyle.Skilled, 8, 0, 0.25, true), "thành dưới 30% bị đánh");
            Assert.IsFalse(BalancePlayStyles.PressThunder(PlayStyle.Skilled, 8, 0, 0.25, false));
        }

        [Test]
        public void SkilledHoldsIceForTheBossUntilItIsFourUnitsFromTheCastleEdge()
        {
            Assert.IsTrue(BalancePlayStyles.PressIce(PlayStyle.Clumsy, 3, 0, 99));
            Assert.IsTrue(BalancePlayStyles.PressIce(PlayStyle.Normal, 3, 15, 99));
            Assert.IsFalse(BalancePlayStyles.PressIce(PlayStyle.Normal, 3, 14, 99));
            Assert.IsFalse(BalancePlayStyles.PressIce(PlayStyle.Skilled, 10, 40, 6), "đợt Trùm: giữ dù đông");
            Assert.IsTrue(BalancePlayStyles.PressIce(PlayStyle.Skilled, 10, 1, 4));
            Assert.IsFalse(BalancePlayStyles.PressIce(PlayStyle.Skilled, 11, 19, 99));
            Assert.IsTrue(BalancePlayStyles.PressIce(PlayStyle.Skilled, 11, 20, 99));
        }

        [Test]
        public void SkilledCheersWhenTwoHeroesAreLowOrTheKnightIsBelowThirtyPercent()
        {
            Assert.IsTrue(BalancePlayStyles.PressCheer(PlayStyle.Clumsy, 0, 1.0));
            Assert.IsTrue(BalancePlayStyles.PressCheer(PlayStyle.Normal, 1, 1.0));
            Assert.IsFalse(BalancePlayStyles.PressCheer(PlayStyle.Normal, 0, 1.0));
            Assert.IsFalse(BalancePlayStyles.PressCheer(PlayStyle.Skilled, 1, 0.5));
            Assert.IsTrue(BalancePlayStyles.PressCheer(PlayStyle.Skilled, 2, 1.0));
            Assert.IsTrue(BalancePlayStyles.PressCheer(PlayStyle.Skilled, 1, 0.25));
        }

        [Test]
        public void BossWavesAreTheOnlyIntendedPressureSpikes()
        {
            // Đợt 9 → 10 → 11: 1.1 → 1.8 → 1.5; bước vọt hơn 0.5 chỉ ở đợt Trùm.
            Assert.That(P(10) - P(9), Is.GreaterThan(0.5));
            Assert.IsTrue(M.IsBossWave(10));
            Assert.That(P(11), Is.LessThan(P(10)));
            Assert.That(System.Math.Abs(P(11) - P(10)), Is.LessThanOrEqualTo(0.5));
            Assert.AreEqual(1.8, P(10), 0.05);
            // Trùm đợt 10: 760 máu, khoảng 22% máu cả đợt; đợt 20: 1160 máu, khoảng 14%.
            Assert.AreEqual(760, N.Enemy(EnemyKind.Boss).Hp * M.Scale(10), 1e-6);
            Assert.AreEqual(0.22, 760 / M.WaveHp(10), 0.01);
            Assert.AreEqual(1160, N.Enemy(EnemyKind.Boss).Hp * M.Scale(20), 1e-6);
            Assert.AreEqual(0.14, 1160 / M.WaveHp(20), 0.01);
        }
    }
}
