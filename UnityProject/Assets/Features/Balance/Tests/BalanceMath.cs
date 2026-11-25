using System;
using System.Collections.Generic;
using N = Game.Balance.Tests.BalanceNumbers;

namespace Game.Balance.Tests
{
    // Đội đã mua của một đợt mẫu (cấp từng mục). Thứ tự hero theo Heroes của đội.
    internal sealed class TeamBuild
    {
        public TeamBuild(HeroCode[] heroes, int[] heroLevels, int castleHp, int castleAttack,
            int thunder, int ice, int cheer)
        {
            Heroes = heroes; HeroLevels = heroLevels; CastleHp = castleHp; CastleAttack = castleAttack;
            Thunder = thunder; Ice = ice; Cheer = cheer;
        }

        public HeroCode[] Heroes { get; }
        public int[] HeroLevels { get; }
        public int CastleHp { get; }
        public int CastleAttack { get; }
        public int Thunder { get; }
        public int Ice { get; }
        public int Cheer { get; }

        public static TeamBuild Fresh(params HeroCode[] heroes) =>
            new TeamBuild(heroes, new[] { 1, 1, 1 }, 1, 1, 1, 1, 1);
    }

    // Các công thức của ba node vòng 4: income-and-prices, hero-compare, break-forecast.
    internal static class BalanceMath
    {
        const double Eps = 1e-9;

        // ---------- Đợt (feature-wave/difficulty-scaling/wave-formula) ----------
        public static int EnemyCount(int w) => N.WaveBaseCount + N.WaveCountPerWave * w;

        public static double Interval(int w) =>
            Math.Max(N.IntervalFloor, N.IntervalStart - N.IntervalStep * (w - 1));

        public static double Scale(int w) => 1 + N.WaveScalePerWave * (w - 1);

        public static bool IsBossWave(int w) => w % N.BossEvery == 0;

        static int[] MixTier(int w)
        {
            int[] tier = N.MixTiers[0];
            foreach (var t in N.MixTiers) if (w >= t[0]) tier = t;
            return tier;
        }

        // Số con từng loại của đợt w.
        public static Dictionary<EnemyKind, int> Composition(int w)
        {
            var c = new Dictionary<EnemyKind, int>();
            int slots = EnemyCount(w);
            if (IsBossWave(w)) { c[EnemyKind.Boss] = 1; slots -= 1; }
            int[] tier = MixTier(w);
            var others = new[] { EnemyKind.Wolf, EnemyKind.Giant, EnemyKind.EnemyArcher };
            int rest = slots;
            for (int i = 0; i < others.Length; i++)
            {
                int pct = tier[i + 1];
                if (pct == 0) continue;
                int n = Math.Max(1, pct * slots / 100);
                c[others[i]] = n;
                rest -= n;
            }
            c[EnemyKind.Soldier] = rest;
            return c;
        }

        static int Count(Dictionary<EnemyKind, int> c, EnemyKind k) => c.TryGetValue(k, out var n) ? n : 0;

        // ---------- Thu (income-and-prices) ----------
        public static int KillGold(int w)
        {
            int sum = 0;
            foreach (var kv in Composition(w)) sum += N.Enemy(kv.Key).Gold * kv.Value;
            return sum;
        }

        public static int ClearBonus(int w) => N.ClearBonusBase + N.ClearBonusPerWave * w;

        public static int Income(int w) => KillGold(w) + ClearBonus(w);

        // Thua giữa đợt: chỉ tính tiền các con đã hạ, không có thưởng dọn đợt.
        public static int IncomeIfNotCleared(Dictionary<EnemyKind, int> killed)
        {
            int sum = 0;
            foreach (var kv in killed) sum += N.Enemy(kv.Key).Gold * kv.Value;
            return sum;
        }

        // Tiền có ở đầu đợt w khi hạ hết quái mọi đợt trước.
        public static int MoneyAtStart(int w)
        {
            int m = N.StartMoney;
            for (int i = 1; i < w; i++) m += Income(i);
            return m;
        }

        // ---------- Giá (income-and-prices) ----------
        public static long Price(UpgradeLine line, int fromLevel)
        {
            double b, r;
            switch (line)
            {
                case UpgradeLine.Hero: b = N.HeroPriceBase; r = N.HeroPriceRate; break;
                case UpgradeLine.CastleHp: b = N.CastleHpPriceBase; r = N.CastleHpPriceRate; break;
                case UpgradeLine.CastleAttack: b = N.CastleAttackPriceBase; r = N.CastleAttackPriceRate; break;
                default: b = N.SkillPriceBase; r = N.SkillPriceRate; break;
            }
            return (long)Math.Ceiling(b * Math.Pow(r, fromLevel - 1) - Eps);
        }

        // Tổng tiền đưa một mục từ cấp 1 lên cấp target.
        public static long CostToLevel(UpgradeLine line, int target)
        {
            long sum = 0;
            for (int c = 1; c < target; c++) sum += Price(line, c);
            return sum;
        }

        public static long BuildCost(TeamBuild b)
        {
            long sum = 0;
            foreach (var l in b.HeroLevels) sum += CostToLevel(UpgradeLine.Hero, l);
            sum += CostToLevel(UpgradeLine.CastleHp, b.CastleHp);
            sum += CostToLevel(UpgradeLine.CastleAttack, b.CastleAttack);
            sum += CostToLevel(UpgradeLine.Skill, b.Thunder);
            sum += CostToLevel(UpgradeLine.Skill, b.Ice);
            sum += CostToLevel(UpgradeLine.Skill, b.Cheer);
            return sum;
        }

        // Giá rẻ nhất của lần nâng kế tiếp trong đội đã mua.
        public static long CheapestNext(TeamBuild b)
        {
            long min = long.MaxValue;
            foreach (var l in b.HeroLevels) min = Math.Min(min, Price(UpgradeLine.Hero, l));
            min = Math.Min(min, Price(UpgradeLine.CastleHp, b.CastleHp));
            min = Math.Min(min, Price(UpgradeLine.CastleAttack, b.CastleAttack));
            min = Math.Min(min, Price(UpgradeLine.Skill, b.Thunder));
            min = Math.Min(min, Price(UpgradeLine.Skill, b.Ice));
            min = Math.Min(min, Price(UpgradeLine.Skill, b.Cheer));
            return min;
        }

        // ---------- Hero theo cấp ----------
        public static double HeroStatMul(int level) => 1 + N.HeroStatPerLevel * (level - 1);

        public static double HeroAttackSpeedMul(int level) =>
            1 + Math.Min(N.HeroAttackSpeedPerLevel * (level - 1), N.HeroAttackSpeedCap);

        // m(L): hệ số sát thương mỗi giây của hero cấp L.
        public static double HeroDpsMul(int level) => HeroStatMul(level) * HeroAttackSpeedMul(level);

        // Sát thương mỗi giây hiệu dụng: đòn vùng nhân số con trúng (mặc định 2).
        public static double HeroEffectiveDps(HeroStats h, int level = 1, int areaHits = 2) =>
            h.Dps * HeroDpsMul(level) * (h.Area ? areaHits : 1);

        // Máu hiệu dụng: hero tầm từ 5u máu ×2, trừ khi có quái đã tới sát (ví dụ Sói).
        public static double HeroEffectiveHp(HeroStats h, int level = 1, bool reachedInMelee = false) =>
            h.Hp * HeroStatMul(level) * (h.Ranged && !reachedInMelee ? 2 : 1);

        // Sức tổng S (feature-balance/balance-team/hero-compare).
        public static double HeroPower(HeroStats h, int level = 1, int areaHits = 2, bool reachedInMelee = false) =>
            Math.Sqrt(HeroEffectiveHp(h, level, reachedInMelee) * HeroEffectiveDps(h, level, areaHits));

        public static double TeamHeroDps(TeamBuild b, int areaHits = 2)
        {
            double sum = 0;
            for (int i = 0; i < b.Heroes.Length; i++)
                sum += HeroEffectiveDps(N.Hero(b.Heroes[i]), b.HeroLevels[i], areaHits);
            return sum;
        }

        public static double TeamHeroHp(TeamBuild b)
        {
            double sum = 0;
            for (int i = 0; i < b.Heroes.Length; i++) sum += N.Hero(b.Heroes[i]).Hp * HeroStatMul(b.HeroLevels[i]);
            return sum;
        }

        // ---------- Thành theo cấp ----------
        public static double CastleMaxHp(int level) => N.CastleHp + N.CastleHpPerLevel * (level - 1);

        public static double CastleShotDamage(int level) => N.CastleShotDamage * (1 + N.CastleAttackPerLevel * (level - 1));

        public static double SkillEffect(double lv1Effect, int level) => lv1Effect * (1 + N.SkillPerLevel * (level - 1));

        // ---------- Áp lực (break-forecast) ----------
        public static int ThunderHits(int w) => w <= 5 ? 4 : w <= 15 ? 5 : 6;

        public static double CastleShotDps(int attackLevel) => CastleShotDamage(attackLevel) / N.CastleShotSeconds;

        public static double ThunderDps(int thunderLevel, int hits) =>
            SkillEffect(N.ThunderDamage, thunderLevel) * hits / N.ThunderCooldown;

        // D: sát thương mỗi giây hiệu dụng của đội ở đợt w. Băng giá và Cổ vũ không vào công thức.
        public static double TeamDps(TeamBuild b, int w) =>
            TeamHeroDps(b) + CastleShotDps(b.CastleAttack) + ThunderDps(b.Thunder, ThunderHits(w));

        // H: tổng máu quái của đợt.
        public static double WaveHp(int w)
        {
            double sum = 0;
            foreach (var kv in Composition(w)) sum += N.Enemy(kv.Key).Hp * kv.Value;
            return sum * Scale(w);
        }

        // E: sát thương mỗi giây của quái nếu tất cả cùng đánh.
        public static double WaveEnemyDps(int w)
        {
            double sum = 0;
            foreach (var kv in Composition(w)) sum += N.Enemy(kv.Key).Dps * kv.Value;
            return sum * Scale(w);
        }

        // T: thời gian thả đợt nếu không bị hoãn.
        public static double SpawnDuration(int w) => (EnemyCount(w) - 1) * Interval(w);

        public static double Pressure(TeamBuild b, int w) => Pressure(b, w, SpawnDuration(w));

        public static double Pressure(TeamBuild b, int w, double spawnDuration) =>
            WaveHp(w) / (TeamDps(b, w) * spawnDuration);
    }
}
