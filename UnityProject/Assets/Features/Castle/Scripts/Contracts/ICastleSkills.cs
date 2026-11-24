using System.Collections.Generic;

namespace Game.Castle
{
    /// <summary>The castle's skills the player presses. The castle keeps every cooldown; callers only show and ask.</summary>
    public interface ICastleSkills
    {
        /// <summary>The skills in this battle, in button order.</summary>
        IReadOnlyList<CastleSkill> Skills { get; }

        CastleSkillState GetState(CastleSkill skill);

        /// <summary>
        /// Uses the skill when it is ready, the castle stands and the battle is not paused; starts its cooldown.
        /// Returns false and changes nothing otherwise.
        /// </summary>
        bool TryUse(CastleSkill skill);
    }
}
