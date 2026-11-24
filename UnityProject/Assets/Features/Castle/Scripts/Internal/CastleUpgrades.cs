using System;

namespace Game.Castle
{
    /// <summary>Receives bought levels and passes each to the part of the castle it strengthens.</summary>
    internal sealed class CastleUpgrades : ICastleUpgrades
    {
        private readonly CastleBody _body;
        private readonly CastleSkills _skills;

        public CastleUpgrades(CastleBody body, CastleSkills skills)
        {
            _body = body ?? throw new ArgumentNullException(nameof(body));
            _skills = skills ?? throw new ArgumentNullException(nameof(skills));
        }

        /// <summary>Attack level; the auto shot reads it when it fires.</summary>
        public int AttackLevel { get; private set; } = 1;

        public void ApplyLevel(CastleUpgrade item, int level)
        {
            if (level < 1) return;
            switch (item)
            {
                case CastleUpgrade.Health: _body.ApplyHealthLevel(level); break;
                case CastleUpgrade.Attack: AttackLevel = level; break;
                case CastleUpgrade.Skills: _skills.ApplyLevel(level); break;
            }
        }

        /// <summary>A new run: every item back to level 1.</summary>
        public void Reset() => AttackLevel = 1;
    }
}
