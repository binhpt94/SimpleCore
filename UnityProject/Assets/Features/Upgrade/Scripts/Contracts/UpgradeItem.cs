namespace Game.Upgrade
{
    public enum UpgradeKind
    {
        /// <summary>One level for the hero in <see cref="UpgradeItem.HeroSlot"/>.</summary>
        Hero,
        CastleHealth,
        CastleAttack,
        CastleSkills,
    }

    /// <summary>One thing the player can buy, at the moment it was read.</summary>
    public readonly struct UpgradeItem
    {
        public UpgradeItem(UpgradeKind kind, int heroSlot, string heroCode, int level, int nextPrice, bool canBuy)
        {
            Kind = kind;
            HeroSlot = heroSlot;
            HeroCode = heroCode;
            Level = level;
            NextPrice = nextPrice;
            CanBuy = canBuy;
        }

        public UpgradeKind Kind { get; }

        /// <summary>Team slot (0 for slot 1) for a hero item; -1 otherwise.</summary>
        public int HeroSlot { get; }

        /// <summary>Hero code for a hero item; null otherwise.</summary>
        public string HeroCode { get; }

        /// <summary>Current level, 1 at the start of a run.</summary>
        public int Level { get; }

        /// <summary>Price of the next level, in coins.</summary>
        public int NextPrice { get; }

        /// <summary>True when buying now would succeed (enough coins, battle running).</summary>
        public bool CanBuy { get; }
    }

    public enum PurchaseResult
    {
        Bought,
        NotEnoughCoins,
        /// <summary>Not now: run over, or no such item.</summary>
        Unavailable,
    }
}
