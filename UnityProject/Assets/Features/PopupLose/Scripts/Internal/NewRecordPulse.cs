using System;

namespace Game.PopupLose
{
    /// <summary>
    /// Scale of the "Kỉ lục mới!" line and its band over time (result-spec): 1.0 → 1.15 → 1.0 every second,
    /// looping until the popup closes. Smooth (cosine) so the line does not jerk at either end.
    /// </summary>
    internal static class NewRecordPulse
    {
        internal const float MinScale = 1f;
        internal const float MaxScale = 1.15f;
        internal const float PeriodSeconds = 1f;

        /// <summary>Scale at <paramref name="seconds"/> after the line appeared.</summary>
        internal static float ScaleAt(float seconds)
        {
            var phase = seconds / PeriodSeconds;
            phase -= (float)Math.Floor(phase);
            var wave = (1f - (float)Math.Cos(2.0 * Math.PI * phase)) * 0.5f;   // 0 → 1 → 0
            return MinScale + (MaxScale - MinScale) * wave;
        }
    }
}
