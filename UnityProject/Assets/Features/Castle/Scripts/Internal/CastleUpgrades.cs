using System;

namespace Game.Castle
{
    /// <summary>
    /// Receives bought levels and passes each to the part of the castle it strengthens. Levels only go up during
    /// a run (no refund, no downgrade): a level not above the item's current one is ignored.
    /// </summary>
    internal sealed class CastleUpgrades : ICastleUpgrades
    {
        private readonly CastleBody _body;
        private readonly CastleSkills _skills;
        private int _healthLevel = 1;
        private int _skillsLevel = 1;

        public CastleUpgrades(CastleBody body, CastleSkills skills)
        {
            _body = body ?? throw new ArgumentNullException(nameof(body));
            _skills = skills ?? throw new ArgumentNullException(nameof(skills));
        }

        /// <summary>Attack level; the auto shot reads it when it fires.</summary>
        public int AttackLevel { get; private set; } = 1;

        /// <summary>A level was applied to a standing castle; the view answers with a short pop.</summary>
        public event Action LevelApplied;

        public void ApplyLevel(CastleUpgrade item, int level)
        {
            if (level < 1 || _body.IsDestroyed) return;
            switch (item)
            {
                case CastleUpgrade.Health:
                    if (level <= _healthLevel) return;
                    _healthLevel = level;
                    _body.ApplyHealthLevel(level);
                    break;
                case CastleUpgrade.Attack:
                    if (level <= AttackLevel) return;
                    AttackLevel = level;
                    break;
                case CastleUpgrade.Skills:
                    if (level <= _skillsLevel) return;
                    _skillsLevel = level;
                    _skills.ApplyLevel(level);
                    break;
                default:
                    return;
            }
            LevelApplied?.Invoke();
        }

        /// <summary>A new run: every item back to level 1.</summary>
        public void Reset()
        {
            AttackLevel = 1;
            _healthLevel = 1;
            _skillsLevel = 1;
        }
    }
}
