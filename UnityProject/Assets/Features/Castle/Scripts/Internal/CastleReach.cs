using System;

namespace Game.Castle
{
    /// <summary>Distance as the game measures it: the empty gap between two boxes' edges, never below 0.</summary>
    internal static class CastleReach
    {
        /// <summary>Seconds left below this count as done, so float rounding never holds a timer back.</summary>
        public const float TimeEpsilon = 1e-4f;

        /// <summary>Gap between a box centred at <paramref name="x"/> and the nearer castle edge (castle centred on x = 0).</summary>
        public static float GapToCastle(float x, float halfWidth, float castleHalfWidth)
            => Math.Max(0f, Math.Abs(x) - halfWidth - castleHalfWidth);
    }
}
