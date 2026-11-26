using System;
using System.Globalization;
using Game.Castle;
using Game.Run;

namespace Game.Hud
{
    /// <summary>Turns the numbers other features report into what the HUD shows. Pure functions, no state.</summary>
    internal static class HudText
    {
        /// <summary>"current/max", both rounded down, never below 0.</summary>
        public static string Health(float health, float maxHealth)
        {
            return Whole(health) + "/" + Whole(maxHealth);
        }

        /// <summary>Share of the bar that is filled: health over max health, kept within 0..1.</summary>
        public static float HealthFill(float health, float maxHealth)
        {
            if (maxHealth <= 0f || health <= 0f) return 0f;
            return Math.Min(1f, health / maxHealth);
        }

        public static string Coins(int coins) => coins.ToString(CultureInfo.InvariantCulture);

        public static string Wave(int wave) => "Đợt " + wave.ToString(CultureInfo.InvariantCulture);

        /// <summary>Seconds left rounded up ("1" at 0.3 s); empty when nothing is left.</summary>
        public static string SkillSeconds(CastleSkillState state)
        {
            if (state.Ready || state.Cooldown <= 0f || state.CooldownLeft <= 0f) return string.Empty;
            return ((int)Math.Ceiling(state.CooldownLeft)).ToString(CultureInfo.InvariantCulture);
        }

        /// <summary>Share of the button still covered: cooldown left over full cooldown, within 0..1.</summary>
        public static float SkillCover(CastleSkillState state)
        {
            if (state.Ready || state.Cooldown <= 0f || state.CooldownLeft <= 0f) return 0f;
            return Math.Min(1f, state.CooldownLeft / state.Cooldown);
        }

        public static string SkillName(CastleSkill skill)
        {
            switch (skill)
            {
                case CastleSkill.Thunder: return "Sấm sét";
                case CastleSkill.Frost: return "Băng giá";
                case CastleSkill.Rally: return "Cổ vũ";
                default: return skill.ToString();
            }
        }

        /// <summary>True on the steps where the battle screen is up.</summary>
        public static bool IsBattle(RunStep step)
        {
            return step == RunStep.Playing || step == RunStep.Ending || step == RunStep.Lost;
        }

        private static string Whole(float value)
        {
            if (value <= 0f) return "0";
            return ((long)Math.Floor(value)).ToString(CultureInfo.InvariantCulture);
        }
    }
}
