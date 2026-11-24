using System.Collections.Generic;

namespace Game.Hero
{
    /// <summary>Every hero the player can pick, in display order. Fixed while the game runs.</summary>
    public interface IHeroRoster
    {
        IReadOnlyList<HeroInfo> Heroes { get; }

        bool TryGet(string code, out HeroInfo hero);
    }
}
