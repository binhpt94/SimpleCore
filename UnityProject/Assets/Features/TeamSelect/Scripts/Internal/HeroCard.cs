using System.Collections.Generic;
using Game.Hero;

namespace Game.TeamSelect
{
    /// <summary>Which of the two role marks a card shows (design: hero-cards/card-spec).</summary>
    internal enum RoleMark
    {
        Melee,
        Ranged,
    }

    /// <summary>
    /// What one hero card shows (design: hero-cards/card-spec): portrait, name, fighting-style line, role mark,
    /// and the picked mark. No stat numbers.
    /// </summary>
    internal readonly struct HeroCard
    {
        public HeroCard(string heroCode, string name, string styleLine, RoleMark role, bool hasPortrait, bool picked)
        {
            HeroCode = heroCode;
            Name = name;
            StyleLine = styleLine;
            Role = role;
            HasPortrait = hasPortrait;
            Picked = picked;
        }

        public string HeroCode { get; }
        public string Name { get; }
        public string StyleLine { get; }
        public RoleMark Role { get; }

        /// <summary>False when the hero has no picture yet: the portrait area stays empty, the card still works.</summary>
        public bool HasPortrait { get; }

        public bool Picked { get; }
    }

    /// <summary>Whether a hero picture exists (sprites live in Unity; tests use a fake).</summary>
    internal interface IHeroPortraits
    {
        bool Has(string heroCode);
    }

    internal static class HeroCards
    {
        /// <summary>
        /// One card per roster hero, in roster order (the order never follows the team). A hero added to the
        /// roster gets a card at its roster place.
        /// </summary>
        public static List<HeroCard> Build(IHeroRoster roster, TeamSlots slots, TeamSelectRules rules, IHeroPortraits portraits)
        {
            var cards = new List<HeroCard>(roster.Heroes.Count);
            foreach (var hero in roster.Heroes)
            {
                cards.Add(new HeroCard(
                    hero.Code,
                    hero.Name,
                    rules.StyleLine(hero),
                    hero.Group == HeroGroup.Melee ? RoleMark.Melee : RoleMark.Ranged,
                    portraits != null && portraits.Has(hero.Code),
                    slots.Contains(hero.Code)));
            }
            return cards;
        }
    }
}
