using System;

namespace Game.Hero
{
    /// <summary>
    /// The level-up pop of live feature-hero/hero-stats/hero-level-values (IDEA): the hero grows to <c>peakScale</c>
    /// and back to 1 within <c>seconds</c>. Pure maths so tests need no Unity objects; the view only reads it.
    /// </summary>
    internal static class HeroLevelUpPop
    {
        /// <summary>Scale of the hero <paramref name="secondsLeft"/> before the pop ends; 1 outside the pop.</summary>
        public static float ScaleAt(float secondsLeft, float peakScale, float seconds)
        {
            if (seconds <= 0f || secondsLeft <= 0f || secondsLeft >= seconds) return 1f;
            var progress = 1f - secondsLeft / seconds;
            return 1f + (peakScale - 1f) * (float)Math.Sin(Math.PI * progress);
        }
    }
}
