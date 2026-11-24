namespace Game.Hero
{
    public enum HeroGroup
    {
        Melee,
        Ranged,
    }

    /// <summary>Who a hero's hit lands on.</summary>
    public enum HeroAttack
    {
        /// <summary>Only the target.</summary>
        SingleTarget,
        /// <summary>The target and every enemy in a stretch in front of the hero.</summary>
        FrontCleave,
        /// <summary>The target and every enemy close around it.</summary>
        Blast,
    }

    /// <summary>One hero of the roster, as shown when choosing a team.</summary>
    public readonly struct HeroInfo
    {
        public HeroInfo(string code, string name, HeroGroup group, HeroAttack attack)
        {
            Code = code;
            Name = name;
            Group = group;
            Attack = attack;
        }

        /// <summary>Stable key of the hero (for example "knight"); what a team is made of.</summary>
        public string Code { get; }

        /// <summary>Display name.</summary>
        public string Name { get; }

        public HeroGroup Group { get; }

        public HeroAttack Attack { get; }
    }
}
