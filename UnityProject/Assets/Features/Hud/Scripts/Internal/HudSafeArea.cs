using System;

namespace Game.Hud
{
    /// <summary>How far the HUD parts move in from each screen edge, in reference units (screen is 1080 tall).</summary>
    internal readonly struct HudInsets
    {
        public readonly float Left;
        public readonly float Right;
        public readonly float Top;
        public readonly float Bottom;

        public HudInsets(float left, float right, float top, float bottom)
        {
            Left = left;
            Right = right;
            Top = top;
            Bottom = bottom;
        }

        public static HudInsets None => new HudInsets(0f, 0f, 0f, 0f);

        public bool IsNone => Left <= 0f && Right <= 0f && Top <= 0f && Bottom <= 0f;
    }

    /// <summary>
    /// Device safe area (feature-hud/hud-layout/layout-spec#safe-area: notch, rounded corners, navigation bar)
    /// turned into HUD insets. The layout already keeps a margin from each edge (left and right 40, top 10,
    /// bottom 20 on the reference screen), so a part only moves by the share of the cut that its margin
    /// does not already cover. Pure functions, no state.
    /// </summary>
    internal static class HudSafeArea
    {
        public const float ReferenceHeight = 1080f;
        public const float SideMargin = 40f;
        public const float TopMargin = 10f;
        public const float BottomMargin = 20f;

        /// <param name="screenWidth">Screen size in pixels.</param>
        /// <param name="screenHeight">Screen size in pixels.</param>
        /// <param name="safeX">Safe area in pixels, origin bottom-left (as Unity's Screen.safeArea).</param>
        public static HudInsets Insets(float screenWidth, float screenHeight, float safeX, float safeY, float safeWidth, float safeHeight)
        {
            if (screenWidth <= 0f || screenHeight <= 0f) return HudInsets.None;

            // The canvas scales by height: one reference unit is screenHeight / 1080 pixels.
            var unit = screenHeight / ReferenceHeight;
            var left = Clamp(safeX) / unit;
            var right = Clamp(screenWidth - (safeX + safeWidth)) / unit;
            var bottom = Clamp(safeY) / unit;
            var top = Clamp(screenHeight - (safeY + safeHeight)) / unit;

            return new HudInsets(
                Beyond(left, SideMargin),
                Beyond(right, SideMargin),
                Beyond(top, TopMargin),
                Beyond(bottom, BottomMargin));
        }

        private static float Clamp(float pixels) => Math.Max(0f, pixels);

        private static float Beyond(float cut, float margin) => Math.Max(0f, cut - margin);
    }
}
