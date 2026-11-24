using System;

namespace Game.Upgrade
{
    /// <summary>Price curve of one item: next price = round up (base × growth^(level − 1)).</summary>
    internal readonly struct PriceCurve
    {
        public PriceCurve(int basePrice, double growth)
        {
            if (basePrice <= 0) throw new ArgumentOutOfRangeException(nameof(basePrice), "Base price must be above 0.");
            if (growth < 1.0) throw new ArgumentOutOfRangeException(nameof(growth), "Growth must be at least 1.");
            BasePrice = basePrice;
            Growth = growth;
        }

        public int BasePrice { get; }

        public double Growth { get; }

        /// <summary>Price to go from <paramref name="level"/> to level + 1, in coins.</summary>
        public int PriceAt(int level)
        {
            if (level < 1) throw new ArgumentOutOfRangeException(nameof(level), "Levels start at 1.");
            var raw = BasePrice * Math.Pow(Growth, level - 1);
            // Drop floating point noise first, so 40 × 1.4 = 56.00000000000001 costs 56, not 57.
            var price = Math.Ceiling(Math.Round(raw, 6));
            return price >= int.MaxValue ? int.MaxValue : (int)price;
        }
    }

    /// <summary>The balance numbers of the shop, read from the config asset.</summary>
    internal readonly struct UpgradePrices
    {
        public UpgradePrices(int teamSize, PriceCurve hero, PriceCurve castleHealth, PriceCurve castleAttack,
            PriceCurve castleSkills)
        {
            if (teamSize <= 0) throw new ArgumentOutOfRangeException(nameof(teamSize), "Team size must be above 0.");
            TeamSize = teamSize;
            Hero = hero;
            CastleHealth = castleHealth;
            CastleAttack = castleAttack;
            CastleSkills = castleSkills;
        }

        /// <summary>Heroes a run must bring; one hero item each.</summary>
        public int TeamSize { get; }

        public PriceCurve Hero { get; }

        public PriceCurve CastleHealth { get; }

        public PriceCurve CastleAttack { get; }

        public PriceCurve CastleSkills { get; }

        public PriceCurve For(UpgradeKind kind)
        {
            switch (kind)
            {
                case UpgradeKind.Hero: return Hero;
                case UpgradeKind.CastleHealth: return CastleHealth;
                case UpgradeKind.CastleAttack: return CastleAttack;
                case UpgradeKind.CastleSkills: return CastleSkills;
                default: throw new ArgumentOutOfRangeException(nameof(kind), kind, null);
            }
        }
    }
}
