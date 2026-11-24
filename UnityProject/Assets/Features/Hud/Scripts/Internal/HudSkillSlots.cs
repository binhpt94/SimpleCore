using Game.Castle;

namespace Game.Hud
{
    /// <summary>
    /// Fixed slots of the skill cluster (feature-hud/hud-layout/layout-spec): Thunder, Frost, Rally from left to right,
    /// x 1420..1880 on the 1920x1080 reference screen. A skill keeps its slot whatever else the battle has;
    /// a slot whose skill is not in the battle stays empty. Pure functions, no state.
    /// </summary>
    internal static class HudSkillSlots
    {
        public const int Count = 3;
        public const float Size = 140f;
        public const float Gap = 20f;
        public const float ClusterLeft = 1420f;
        public const float ClusterTop = 920f;
        public const float ClusterWidth = Count * Size + (Count - 1) * Gap;

        /// <summary>Slot of a skill, 0 = leftmost; -1 for a skill the cluster has no slot for.</summary>
        public static int SlotOf(CastleSkill skill)
        {
            switch (skill)
            {
                case CastleSkill.Thunder: return 0;
                case CastleSkill.Frost: return 1;
                case CastleSkill.Rally: return 2;
                default: return -1;
            }
        }

        /// <summary>Left edge of a slot on the reference screen.</summary>
        public static float Left(int slot) => ClusterLeft + slot * (Size + Gap);
    }
}
