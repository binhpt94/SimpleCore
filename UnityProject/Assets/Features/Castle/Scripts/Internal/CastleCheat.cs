#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;

namespace Game.Castle
{
    /// <summary>Dev-only: forces castle health and skill cooldowns for cheat states (feature cheat nodes).</summary>
    internal sealed class CastleCheat : ICastleCheat
    {
        private readonly CastleBody _body;
        private readonly CastleSkills _skills;

        public CastleCheat(CastleBody body, CastleSkills skills)
        {
            _body = body ?? throw new ArgumentNullException(nameof(body));
            _skills = skills ?? throw new ArgumentNullException(nameof(skills));
        }

        public void SetHealth(float health) => _body.ForceHealth(health);

        public void SetCooldownLeft(CastleSkill skill, float secondsLeft) => _skills.ForceCooldownLeft(skill, secondsLeft);
    }
}
#endif
