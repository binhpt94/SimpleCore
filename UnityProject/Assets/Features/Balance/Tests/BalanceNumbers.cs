using System.Collections.Generic;

namespace Game.Balance.Tests
{
    // Bản đối chiếu bộ số chuẩn (feature-balance/balance-config/number-table).
    // Balance không có code chạy trong game và không có cấu hình riêng: mỗi số dưới đây nằm
    // trong cấu hình của feature sở hữu. Lớp này chỉ là bảng đối chiếu cho test.

    internal enum Owner { Hero, Castle, Enemy, Wave, Upgrade, Wallet }

    internal enum HeroCode { Knight, Axeman, Swordsman, Archer, Mage }

    internal enum EnemyKind { Soldier, Wolf, Giant, EnemyArcher, Boss }

    internal enum UpgradeLine { Hero, CastleHp, CastleAttack, Skill }

    internal sealed class HeroStats
    {
        public HeroStats(HeroCode code, string name, double hp, double damage, double attackSeconds,
            double range, double speed, bool area, double areaRadius)
        {
            Code = code; Name = name; Hp = hp; Damage = damage; AttackSeconds = attackSeconds;
            Range = range; Speed = speed; Area = area; AreaRadius = areaRadius;
        }

        public HeroCode Code { get; }
        public string Name { get; }
        public double Hp { get; }
        public double Damage { get; }
        public double AttackSeconds { get; }
        public double Range { get; }
        public double Speed { get; }
        public bool Area { get; }
        public double AreaRadius { get; }

        // Số suy ra, không phải số cấu hình.
        public double Dps => Damage / AttackSeconds;
        public bool Ranged => Range >= BalanceNumbers.RangedFrom;
        public bool Melee => !Ranged;
    }

    internal sealed class EnemyStats
    {
        public EnemyStats(EnemyKind kind, string name, double hp, double damage, double attackSeconds,
            double range, double speed, int gold)
        {
            Kind = kind; Name = name; Hp = hp; Damage = damage; AttackSeconds = attackSeconds;
            Range = range; Speed = speed; Gold = gold;
        }

        public EnemyKind Kind { get; }
        public string Name { get; }
        public double Hp { get; }
        public double Damage { get; }
        public double AttackSeconds { get; }
        public double Range { get; }
        public double Speed { get; }
        public int Gold { get; }
        public double Dps => Damage / AttackSeconds;
    }

    // Một dòng của bảng: số nào, đơn vị, feature sở hữu, các số phải xem lại khi đổi.
    // Một liên kết là key của dòng khác trong bảng hoặc đường dẫn node thiết kế (feature-...).
    internal sealed class TableEntry
    {
        public TableEntry(string key, string unit, Owner owner, params string[] reviewOnChange)
        {
            Key = key; Unit = unit; Owner = owner; ReviewOnChange = reviewOnChange;
        }

        public string Key { get; }
        public string Unit { get; }
        public Owner Owner { get; }
        public string[] ReviewOnChange { get; }
    }

    internal static class BalanceNumbers
    {
        // ---- Bản đồ và thành (feature-castle) ----
        public const double StripLeft = -16, StripRight = 16;
        public const double CastleX = 0, CastleWidth = 3, CastleHeight = 4;
        public const double CastleEdge = 1.5;
        public const double CastleHp = 1000;
        public const double CastleShotDamage = 10, CastleShotSeconds = 1.0, CastleShotRange = 6;
        public const int CastleShotTargets = 1;
        public const double ThunderDamage = 80, ThunderRange = 6, ThunderCooldown = 20;
        public const double IceSlow = 0.5, IceSeconds = 5, IceCooldown = 35;
        public const double CheerHeal = 0.30, CheerCooldown = 25;
        public const double CastleHpPerLevel = 150;
        public const double CastleAttackPerLevel = 0.20; // phần của sát thương lv1
        public const double SkillPerLevel = 0.20;        // phần của hiệu lực lv1, hồi chiêu không đổi

        // ---- Hero (feature-hero), lv1 ----
        public const int TeamSize = 3;
        public const double RangedFrom = 5;              // tầm từ 5u tính là bắn xa
        public const double HeroReachX = 15;
        public const double HeroRespawn = 8;
        public const double HeroStatPerLevel = 0.15;     // máu và sát thương, theo lv1
        public const double HeroAttackSpeedPerLevel = 0.03;
        public const double HeroAttackSpeedCap = 0.60;   // chạm ở cấp 21

        public static readonly HeroStats[] Heroes =
        {
            new HeroStats(HeroCode.Knight,    "Hiệp sĩ",    300, 12, 1.0, 1.2, 2.0, false, 0),
            new HeroStats(HeroCode.Axeman,    "Đấu sĩ rìu", 240, 14, 1.4, 1.3, 2.4, true, 1.5),
            new HeroStats(HeroCode.Swordsman, "Kiếm khách", 180, 16, 0.7, 1.2, 3.5, false, 0),
            new HeroStats(HeroCode.Archer,    "Cung thủ",   120, 14, 0.8, 7,   3.0, false, 0),
            new HeroStats(HeroCode.Mage,      "Pháp sư",    100, 18, 1.6, 6,   2.6, true, 1.5),
        };

        // ---- Quái (feature-enemy), gốc ở đợt 1 ----
        public static readonly EnemyStats[] Enemies =
        {
            new EnemyStats(EnemyKind.Soldier,     "Lính",          40,  5, 1.0, 1.0, 1.5,   5),
            new EnemyStats(EnemyKind.Wolf,        "Sói",           20,  3, 0.8, 1.0, 3.0,   4),
            new EnemyStats(EnemyKind.Giant,       "Khổng lồ",     150, 15, 1.5, 1.2, 0.8,  15),
            new EnemyStats(EnemyKind.EnemyArcher, "Cung thủ quái", 25,  6, 1.2, 5,   1.5,   6),
            new EnemyStats(EnemyKind.Boss,        "Trùm",         400, 40, 2.0, 1.5, 0.6, 100),
        };

        // ---- Đợt (feature-wave) ----
        public const int WaveBaseCount = 6, WaveCountPerWave = 2;
        public const double IntervalStart = 1.0, IntervalStep = 0.05, IntervalFloor = 0.3;
        public const double WaveScalePerWave = 0.10;     // máu và sát thương; tiền không nhân
        public const int BossEvery = 10;
        public const int AliveCap = 40;
        public const double GapSeconds = 3;
        public const int ClearBonusBase = 20, ClearBonusPerWave = 5;

        // Đợt mở từng loại; Trùm không mở theo mốc mà ra ở đợt chia hết cho BossEvery (trả 0).
        public static int UnlockWave(EnemyKind kind)
        {
            switch (kind)
            {
                case EnemyKind.Soldier: return 1;
                case EnemyKind.Wolf: return 3;
                case EnemyKind.Giant: return 5;
                case EnemyKind.EnemyArcher: return 7;
                default: return 0;
            }
        }

        // Bảng phần trăm loại theo bậc đợt (feature-wave/difficulty-scaling/wave-formula), tính bằng %.
        // Mỗi bậc: đợt đầu bậc, rồi phần của Sói, Khổng lồ, Cung thủ quái; Lính lấy phần còn lại.
        public static readonly int[][] MixTiers =
        {
            new[] { 1,  0,  0,  0 },
            new[] { 3, 30,  0,  0 },
            new[] { 5, 30, 15,  0 },
            new[] { 7, 25, 15, 15 },
            new[] { 10, 25, 20, 15 },
            new[] { 15, 25, 20, 20 },
        };

        // ---- Nâng cấp (feature-upgrade) và ví (feature-wallet) ----
        public const double HeroPriceBase = 30, HeroPriceRate = 1.35;
        public const double CastleHpPriceBase = 40, CastleHpPriceRate = 1.4;
        public const double CastleAttackPriceBase = 40, CastleAttackPriceRate = 1.4;
        public const double SkillPriceBase = 60, SkillPriceRate = 1.5;
        public const int MaxLevel = 0;                   // 0: không có cấp tối đa
        public const int StartMoney = 50;

        public static EnemyStats Enemy(EnemyKind kind)
        {
            foreach (var e in Enemies) if (e.Kind == kind) return e;
            throw new KeyNotFoundException(kind.ToString());
        }

        public static HeroStats Hero(HeroCode code)
        {
            foreach (var h in Heroes) if (h.Code == code) return h;
            throw new KeyNotFoundException(code.ToString());
        }

        // ---- Bảng nơi đặt và cột "Xem lại khi đổi" ----
        public const string HeroCompareNode = "feature-balance/balance-team/hero-compare";
        public const string IncomeNode = "feature-balance/balance-economy/income-and-prices";
        public const string BreakNode = "feature-balance/balance-difficulty-curve/break-forecast";
        public const string WaveFormulaNode = "feature-wave/difficulty-scaling/wave-formula";

        public static readonly TableEntry[] Table =
        {
            new TableEntry("map.strip",          "u",   Owner.Castle, "wave.spawn-point", "hero.reach"),
            new TableEntry("castle.body",        "u",   Owner.Castle, "castle.shot", "castle.thunder"),
            new TableEntry("castle.hp",          "máu", Owner.Castle, "enemy.stats", BreakNode),
            new TableEntry("castle.shot",        "sát thương, s, u", Owner.Castle, "enemy.stats"),
            new TableEntry("castle.thunder",     "sát thương, u, s", Owner.Castle, "enemy.stats", "wave.scale"),
            new TableEntry("castle.ice",         "%, s",  Owner.Castle, "wave.interval"),
            new TableEntry("castle.cheer",       "%, s",  Owner.Castle, "hero.stats", "enemy.stats"),
            new TableEntry("castle.level.hp",    "máu",   Owner.Castle, "upgrade.castle-hp-price"),
            new TableEntry("castle.level.attack","% lv1", Owner.Castle, "upgrade.castle-attack-price"),
            new TableEntry("castle.level.skill", "% lv1", Owner.Castle, "upgrade.skill-price"),

            new TableEntry("hero.stats",         "máu, sát thương, s, u, u/s", Owner.Hero, "upgrade.hero-price", HeroCompareNode),
            new TableEntry("hero.reach",         "u",   Owner.Hero, "map.strip"),
            new TableEntry("hero.respawn",       "s",   Owner.Hero, HeroCompareNode),
            new TableEntry("hero.level",         "% lv1", Owner.Hero, "upgrade.hero-price", HeroCompareNode),

            new TableEntry("enemy.stats",        "máu, sát thương, s, u, u/s", Owner.Enemy, "wave.scale", "castle.shot", "castle.thunder", BreakNode),
            new TableEntry("enemy.gold",         "xu",  Owner.Enemy, "upgrade.hero-price", BreakNode, IncomeNode),

            new TableEntry("wave.spawn-point",   "u",   Owner.Wave, "map.strip"),
            new TableEntry("wave.count",         "con", Owner.Wave, IncomeNode, BreakNode),
            new TableEntry("wave.interval",      "s",   Owner.Wave, BreakNode, "wave.alive-cap"),
            new TableEntry("wave.scale",         "hệ số", Owner.Wave, BreakNode),
            new TableEntry("wave.unlock",        "đợt", Owner.Wave, HeroCompareNode),
            new TableEntry("wave.boss",          "đợt", Owner.Wave, BreakNode),
            new TableEntry("wave.mix",           "%",   Owner.Wave, IncomeNode),
            new TableEntry("wave.alive-cap",     "con", Owner.Wave, "wave.interval"),
            new TableEntry("wave.gap",           "s",   Owner.Wave, BreakNode),
            new TableEntry("wave.clear-bonus",   "xu",  Owner.Wave, IncomeNode),

            new TableEntry("upgrade.hero-price",          "xu", Owner.Upgrade, IncomeNode),
            new TableEntry("upgrade.castle-hp-price",     "xu", Owner.Upgrade, IncomeNode),
            new TableEntry("upgrade.castle-attack-price", "xu", Owner.Upgrade, IncomeNode),
            new TableEntry("upgrade.skill-price",         "xu", Owner.Upgrade, IncomeNode),
            // Bảng để trống cột "Xem lại khi đổi" cho dòng cấp tối đa.
            new TableEntry("upgrade.max-level",           "cấp", Owner.Upgrade),

            new TableEntry("wallet.start",       "xu",  Owner.Wallet, BreakNode),
        };

        public static TableEntry Entry(string key)
        {
            foreach (var e in Table) if (e.Key == key) return e;
            return null;
        }
    }
}
