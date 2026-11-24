using System;
using System.Collections.Generic;

namespace Game.Hero
{
    /// <summary>The heroes of the config, in config order. Built once; never changes while the game runs.</summary>
    internal sealed class HeroRoster : IHeroRoster
    {
        private readonly List<HeroInfo> _heroes = new List<HeroInfo>();
        private readonly Dictionary<string, HeroInfo> _byCode = new Dictionary<string, HeroInfo>();

        public HeroRoster(HeroConfig config)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));
            foreach (var entry in config.Heroes)
            {
                var info = new HeroInfo(entry.code, entry.displayName, entry.group, entry.attack);
                _heroes.Add(info);
                _byCode.Add(entry.code, info);
            }
        }

        public IReadOnlyList<HeroInfo> Heroes => _heroes;

        public bool TryGet(string code, out HeroInfo hero)
        {
            if (code != null && _byCode.TryGetValue(code, out hero)) return true;
            hero = default;
            return false;
        }
    }
}
