using System;
using System.Linq;
using M = Game.Balance.Tests.BalanceMath;

namespace Game.Balance.Tests
{
    // Ba cách chơi để chơi thử (break-forecast, mục "Ba cách chơi để chơi thử"): vụng, thường, khéo.
    // Đây là bản đối chiếu cho test và cho tester mô phỏng; Balance không có code chạy trong game.
    internal enum PlayStyle { Clumsy, Normal, Skilled }

    // Món mua được của đội mẫu Hiệp sĩ + Cung thủ + Pháp sư.
    internal enum BuyItem { Knight, Archer, Mage, CastleHp, CastleAttack, Thunder, Ice, Cheer }

    // Cấp từng món lúc quyết định mua.
    internal sealed class ShopLevels
    {
        public ShopLevels(int knight, int archer, int mage, int castleHp, int castleAttack,
            int thunder, int ice, int cheer)
        {
            Knight = knight; Archer = archer; Mage = mage; CastleHp = castleHp; CastleAttack = castleAttack;
            Thunder = thunder; Ice = ice; Cheer = cheer;
        }

        public int Knight { get; }
        public int Archer { get; }
        public int Mage { get; }
        public int CastleHp { get; }
        public int CastleAttack { get; }
        public int Thunder { get; }
        public int Ice { get; }
        public int Cheer { get; }

        public int LowestHero => Math.Min(Knight, Math.Min(Archer, Mage));

        public int Level(BuyItem item)
        {
            switch (item)
            {
                case BuyItem.Knight: return Knight;
                case BuyItem.Archer: return Archer;
                case BuyItem.Mage: return Mage;
                case BuyItem.CastleHp: return CastleHp;
                case BuyItem.CastleAttack: return CastleAttack;
                case BuyItem.Thunder: return Thunder;
                case BuyItem.Ice: return Ice;
                default: return Cheer;
            }
        }

        public static ShopLevels Fresh() => new ShopLevels(1, 1, 1, 1, 1, 1, 1, 1);
    }

    internal static class BalancePlayStyles
    {
        // ---- Mua ----
        public const double SkilledMaxIdleSeconds = 3;   // khéo: không để tiền nằm yên quá 3 giây khi đủ giá
        public const double NormalHeroShare = 0.55;      // thường: 55% hero, 45% thành và kĩ năng
        public const double SkilledCastleHpRepairBelow = 0.5;

        public static UpgradeLine Line(BuyItem item)
        {
            switch (item)
            {
                case BuyItem.Knight: case BuyItem.Archer: case BuyItem.Mage: return UpgradeLine.Hero;
                case BuyItem.CastleHp: return UpgradeLine.CastleHp;
                case BuyItem.CastleAttack: return UpgradeLine.CastleAttack;
                default: return UpgradeLine.Skill;
            }
        }

        public static long PriceOf(BuyItem item, ShopLevels l) => M.Price(Line(item), l.Level(item));

        // Có mua ngay lúc này không. Thường chỉ mua ở đầu lúc thở; vụng và khéo mua mỗi khi đủ tiền.
        public static bool BuysNow(PlayStyle style, bool atGapStart, long money, long nextPrice)
        {
            if (style == PlayStyle.Normal && !atGapStart) return false;
            return money >= nextPrice;
        }

        // Vụng: món rẻ nhất lúc đó; hoà giá thì món đứng trước trong BuyItem.
        public static BuyItem ClumsyNext(ShopLevels l) =>
            ((BuyItem[])Enum.GetValues(typeof(BuyItem)))
                .OrderBy(i => PriceOf(i, l)).ThenBy(i => (int)i).First();

        // Khéo: dòng đầu tiên còn đúng của thứ tự ưu tiên.
        public static BuyItem SkilledNext(ShopLevels l, double castleHpFraction)
        {
            if (castleHpFraction < SkilledCastleHpRepairBelow) return BuyItem.CastleHp;            // 1
            if (l.Mage <= l.LowestHero + 1) return BuyItem.Mage;                                   // 2
            if (l.CastleAttack < l.Mage - 2) return BuyItem.CastleAttack;                           // 3
            if (l.Thunder < (int)Math.Ceiling(l.Mage / 3.0)) return BuyItem.Thunder;                // 4
            int low = l.LowestHero;                                                                 // 5
            if (l.Knight == low) return BuyItem.Knight;
            if (l.Archer == low) return BuyItem.Archer;
            if (l.Mage == low) return BuyItem.Mage;
            return BuyItem.CastleHp;                                                                // 6
        }

        // Khéo: trong đợt ngay trước đợt Trùm (9, 19, ...) để dành giá Băng giá cấp kế nếu Băng giá còn cấp 1.
        public static long SkilledReserve(int wave, ShopLevels l) =>
            M.IsBossWave(wave + 1) && l.Ice == 1 ? PriceOf(BuyItem.Ice, l) : 0;

        // ---- Kĩ năng thành (chỉ khi kĩ năng đã hồi xong) ----
        public static bool PressThunder(PlayStyle style, int wave, int enemiesNearCastle,
            double castleHpFraction, bool enemyHittingCastle)
        {
            switch (style)
            {
                case PlayStyle.Clumsy: return true;
                case PlayStyle.Normal: return enemiesNearCastle >= 4;
                default:
                    int crowd = wave <= 5 ? 4 : 6;
                    return enemiesNearCastle >= crowd || (castleHpFraction < 0.3 && enemyHittingCastle);
            }
        }

        // bossToEdge: khoảng cách Trùm tới mép thành (u); chỉ dùng ở đợt Trùm.
        public static bool PressIce(PlayStyle style, int wave, int aliveEnemies, double bossToEdge)
        {
            switch (style)
            {
                case PlayStyle.Clumsy: return true;
                case PlayStyle.Normal: return aliveEnemies >= 15;
                default:
                    if (M.IsBossWave(wave)) return bossToEdge <= 4;
                    return aliveEnemies >= 20;
            }
        }

        public static bool PressCheer(PlayStyle style, int heroesUnderFortyPercent, double knightHpFraction)
        {
            switch (style)
            {
                case PlayStyle.Clumsy: return true;
                case PlayStyle.Normal: return heroesUnderFortyPercent >= 1;
                default: return heroesUnderFortyPercent >= 2 || knightHpFraction < 0.3;
            }
        }

        // ---- Mục tiêu khoảng cách khéo và vụng (PROTOTYPE) ----
        public const int RunsPerStyle = 5;
        public const int ClumsyBreakMin = 10, ClumsyBreakMax = 12;
        public const int SkilledBreakMin = 16, SkilledBreakMax = 19;
        public const int PrototypeMinGap = 4, FullMinGap = 6;

        public static double Median(params int[] breakWaves)
        {
            var s = breakWaves.OrderBy(x => x).ToArray();
            int n = s.Length;
            return n % 2 == 1 ? s[n / 2] : (s[n / 2 - 1] + s[n / 2]) / 2.0;
        }

        // Đạt khi mỗi cách chơi có đủ số lượt và trung vị khéo hơn trung vị vụng ít nhất minGap đợt.
        public static bool GapReached(int[] clumsy, int[] skilled, int minGap) =>
            clumsy.Length >= RunsPerStyle && skilled.Length >= RunsPerStyle
            && Median(skilled) - Median(clumsy) >= minGap;
    }
}
