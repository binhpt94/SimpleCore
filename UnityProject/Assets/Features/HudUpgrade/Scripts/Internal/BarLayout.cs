using System;
using System.Collections.Generic;

namespace Game.HudUpgrade
{
    /// <summary>
    /// Where the bar's buttons go along the bottom edge, in canvas units (px on the 1920x1080 reference screen).
    /// Buttons of one group sit <see cref="Settings.ButtonGap"/> apart; a new group starts after the wider
    /// <see cref="Settings.GroupGap"/>. The whole bar must end at least 40 units left of the skill cluster of
    /// feature-hud (x 1420 on the reference screen, 500 units from the right edge): when the screen is narrower
    /// the bar shrinks uniformly to keep that gap instead of touching or running under it.
    /// </summary>
    internal static class BarLayout
    {
        internal readonly struct Settings
        {
            public Settings(float barLeft, float buttonWidth, float buttonGap, float groupGap, float rightReserve)
            {
                BarLeft = barLeft;
                ButtonWidth = buttonWidth;
                ButtonGap = buttonGap;
                GroupGap = groupGap;
                RightReserve = rightReserve;
            }

            /// <summary>Left edge of the first button, from the screen's left edge.</summary>
            public float BarLeft { get; }
            public float ButtonWidth { get; }
            public float ButtonGap { get; }
            public float GroupGap { get; }

            /// <summary>
            /// Width kept free at the right edge: the skill cluster (1920 - 1420 = 500) plus the least gap
            /// before it (40, frame bar-buttons), so 540.
            /// </summary>
            public float RightReserve { get; }
        }

        internal readonly struct Result
        {
            public Result(float[] buttonLefts, float width, float scale)
            {
                ButtonLefts = buttonLefts;
                Width = width;
                Scale = scale;
            }

            /// <summary>Left edge of each button inside the bar, before scaling.</summary>
            public float[] ButtonLefts { get; }

            /// <summary>Bar width before scaling.</summary>
            public float Width { get; }

            /// <summary>1 when the bar fits; below 1 when it had to shrink to stay left of the skill cluster.</summary>
            public float Scale { get; }

            /// <summary>Right edge of the last button on screen, after scaling.</summary>
            public float RightEdge(Settings s) => s.BarLeft + Width * Scale;
        }

        public static Result Compute(IReadOnlyList<int> groups, float canvasWidth, Settings s)
        {
            if (groups == null) throw new ArgumentNullException(nameof(groups));
            var lefts = new float[groups.Count];
            var x = 0f;
            for (var i = 0; i < groups.Count; i++)
            {
                if (i > 0) x += groups[i] != groups[i - 1] ? s.GroupGap : s.ButtonGap;
                lefts[i] = x;
                x += s.ButtonWidth;
            }

            var room = canvasWidth - s.RightReserve - s.BarLeft;
            var scale = x <= 0 || room >= x ? 1f : Math.Max(0f, room / x);
            return new Result(lefts, x, scale);
        }
    }
}
