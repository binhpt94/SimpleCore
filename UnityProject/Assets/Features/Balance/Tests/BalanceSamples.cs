namespace Game.Balance.Tests
{
    // Đội mẫu và cách mua của bảng "Kết quả tính thử" (break-forecast):
    // Hiệp sĩ + Cung thủ + Pháp sư, tiêu hết tiền đầu mỗi đợt.
    internal sealed class SampleWave
    {
        public SampleWave(int wave, int money, TeamBuild build, double d, double h, double t, double p,
            double e, double castleMaxHp, double heroHp)
        {
            Wave = wave; Money = money; Build = build; D = d; H = h; T = t; P = p; E = e;
            CastleMaxHp = castleMaxHp; HeroHp = heroHp;
        }

        public int Wave { get; }
        public int Money { get; }
        public TeamBuild Build { get; }
        public double D { get; }
        public double H { get; }
        public double T { get; }
        public double P { get; }
        public double E { get; }
        public double CastleMaxHp { get; }
        public double HeroHp { get; }
    }

    internal static class BalanceSamples
    {
        public static readonly HeroCode[] MixedTeam = { HeroCode.Knight, HeroCode.Archer, HeroCode.Mage };
        public static readonly HeroCode[] MeleeTeam = { HeroCode.Knight, HeroCode.Axeman, HeroCode.Swordsman };

        static TeamBuild B(int knight, int archer, int mage, int hp, int atk, int thunder, int ice, int cheer) =>
            new TeamBuild(MixedTeam, new[] { knight, archer, mage }, hp, atk, thunder, ice, cheer);

        // Bảng ghi "hero cấp 5/5/6" ở đợt 10 và "một con 8" ở đợt 15 mà không nói con nào:
        // đợt 10 chọn Hiệp sĩ cấp 6 (khớp tổng máu hero 877); đợt 15 chọn Cung thủ cấp 8 (khớp D 173).
        public static SampleWave[] Rows() => new[]
        {
            new SampleWave(1,  50,   B(1, 1, 1, 1, 1, 1, 1, 1),  78, 320,  7,     0.6, 40,  1000, 520),
            new SampleWave(5,  393,  B(3, 3, 3, 2, 2, 2, 1, 1), 103, 1092, 12,    0.9, 119, 1150, 676),
            new SampleWave(9,  1070, B(5, 5, 5, 4, 3, 3, 1, 1), 135, 2025, 13.8,  1.1, 230, 1450, 832),
            new SampleWave(10, 1282, B(6, 5, 5, 4, 4, 3, 1, 2), 140, 3391, 13.75, 1.8, 309, 1450, 877),
            new SampleWave(11, 1624, B(6, 6, 6, 5, 4, 3, 1, 2), 149, 2940, 13.5,  1.5, 313, 1600, 910),
            new SampleWave(15, 2791, B(7, 8, 7, 6, 6, 4, 2, 2), 173, 4620, 10.5,  2.5, 489, 1750, 988),
            new SampleWave(19, 4273, B(9, 9, 9, 6, 6, 5, 2, 2), 205, 6440, 12.9,  2.4, 690, 1750, 1144),
            new SampleWave(20, 4685, B(9, 9, 9, 7, 7, 5, 3, 2), 207, 8222, 13.5,  2.9, 801, 1900, 1144),
        };

        public static SampleWave Row(int wave)
        {
            foreach (var r in Rows()) if (r.Wave == wave) return r;
            return null;
        }
    }
}
