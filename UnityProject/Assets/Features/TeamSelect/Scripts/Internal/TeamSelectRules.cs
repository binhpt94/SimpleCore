using System;
using System.Collections.Generic;
using Game.Hero;

namespace Game.TeamSelect
{
    /// <summary>
    /// Numbers and words of the team select screen (design: team-slots/slots-spec, team-memory/memory-rules,
    /// hero-cards/card-spec). Built from <see cref="TeamSelectConfig"/> in the game, by hand in tests.
    /// </summary>
    internal sealed class TeamSelectRules
    {
        private readonly string[] _defaultTeam;
        private readonly Dictionary<string, string> _roleWords;

        public TeamSelectRules(
            int slotCount,
            IReadOnlyList<string> defaultTeam,
            string meleeWord,
            string rangedWord,
            string separator,
            IReadOnlyDictionary<string, string> roleWordByHero,
            string singleTargetWord,
            string frontCleaveWord,
            string blastWord)
        {
            if (slotCount <= 0) throw new ArgumentOutOfRangeException(nameof(slotCount), "TeamSelect: slot count must be > 0.");
            if (defaultTeam == null) throw new ArgumentNullException(nameof(defaultTeam));
            SlotCount = slotCount;
            _defaultTeam = new string[defaultTeam.Count];
            for (var i = 0; i < _defaultTeam.Length; i++) _defaultTeam[i] = defaultTeam[i];
            MeleeWord = meleeWord ?? string.Empty;
            RangedWord = rangedWord ?? string.Empty;
            Separator = separator ?? string.Empty;
            _roleWords = new Dictionary<string, string>();
            if (roleWordByHero != null)
                foreach (var pair in roleWordByHero) _roleWords[pair.Key] = pair.Value;
            SingleTargetWord = singleTargetWord ?? string.Empty;
            FrontCleaveWord = frontCleaveWord ?? string.Empty;
            BlastWord = blastWord ?? string.Empty;
        }

        /// <summary>Number of slots in a team (3).</summary>
        public int SlotCount { get; }

        /// <summary>Team put in the slots while no team is remembered, by hero code, slot 1 first.</summary>
        public IReadOnlyList<string> DefaultTeam => _defaultTeam;

        public string MeleeWord { get; }
        public string RangedWord { get; }
        public string Separator { get; }
        public string SingleTargetWord { get; }
        public string FrontCleaveWord { get; }
        public string BlastWord { get; }

        /// <summary>The fighting-style line of a card: group, separator, role word (for example "Cận chiến · Chắn").</summary>
        public string StyleLine(HeroInfo hero)
        {
            var group = hero.Group == HeroGroup.Melee ? MeleeWord : RangedWord;
            return group + Separator + RoleWord(hero);
        }

        private string RoleWord(HeroInfo hero)
        {
            if (hero.Code != null && _roleWords.TryGetValue(hero.Code, out var word) && !string.IsNullOrEmpty(word))
                return word;
            switch (hero.Attack)
            {
                case HeroAttack.FrontCleave: return FrontCleaveWord;
                case HeroAttack.Blast: return BlastWord;
                default: return SingleTargetWord;
            }
        }

        /// <summary>The values of the design (slots-spec, memory-rules, card-spec).</summary>
        public static TeamSelectRules Design(IReadOnlyList<string> defaultTeam, IReadOnlyDictionary<string, string> roleWordByHero)
        {
            return new TeamSelectRules(3, defaultTeam, "Cận chiến", "Bắn xa", " · ", roleWordByHero,
                "Một mục tiêu", "Chém vùng", "Nổ vùng");
        }
    }
}
