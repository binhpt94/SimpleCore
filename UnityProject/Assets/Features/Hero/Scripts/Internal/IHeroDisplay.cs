using UnityEngine;

namespace Game.Hero
{
    /// <summary>
    /// One hero drawn on the battlefield. Implemented in View; the presenter only pushes what to show.
    /// Positions are in battlefield units (u), x = 0 at the castle centre, feet at y = 0.
    /// Animations move on only through <see cref="Animate"/>, so they stand still while the battle does not run.
    /// </summary>
    internal interface IHeroDisplay
    {
        void Setup(string heroCode, Sprite sprite, Color placeholderColor, float width, float height);

        void SetPose(float x, int facing, bool moving);

        void Animate(float deltaSeconds);

        void PlayStrike(HeroStrike strike);

        void PlayHurt();

        void PlayFall(float seconds);

        void PlayRespawn(float seconds);

        /// <summary>Grow to <paramref name="peakScale"/> and back within <paramref name="seconds"/> (<see cref="HeroLevelUpPop"/>).</summary>
        void PlayLevelUp(float peakScale, float seconds);

        /// <summary>Shows the number <paramref name="level"/> above the hero for <paramref name="seconds"/>; a new call replaces it.</summary>
        void ShowLevel(int level, float seconds);

        void Remove();
    }

    /// <summary>Makes hero displays; behind an interface so tests need no Unity objects.</summary>
    internal interface IHeroDisplayFactory
    {
        IHeroDisplay Create();

        /// <summary>Removes everything this factory made.</summary>
        void RemoveAll();
    }
}
