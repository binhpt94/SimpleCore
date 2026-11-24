namespace Game.Castle
{
    public enum CastleSkill
    {
        /// <summary>Damages every enemy in the zone around the castle.</summary>
        Thunder,
        /// <summary>Slows every living enemy.</summary>
        Frost,
        /// <summary>Heals every standing hero.</summary>
        Rally,
    }

    /// <summary>A skill's state at the moment it was read.</summary>
    public readonly struct CastleSkillState
    {
        public CastleSkillState(CastleSkill skill, bool ready, float cooldownLeft, float cooldown)
        {
            Skill = skill;
            Ready = ready;
            CooldownLeft = cooldownLeft;
            Cooldown = cooldown;
        }

        public CastleSkill Skill { get; }

        /// <summary>True when pressing it now would use it.</summary>
        public bool Ready { get; }

        /// <summary>Seconds until ready again; 0 when ready.</summary>
        public float CooldownLeft { get; }

        /// <summary>The full cooldown in seconds.</summary>
        public float Cooldown { get; }
    }
}
