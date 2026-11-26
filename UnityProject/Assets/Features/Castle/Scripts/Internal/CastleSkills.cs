using System;
using System.Collections.Generic;
using Game.Enemy;
using Game.Hero;
using Game.Run;

namespace Game.Castle
{
    /// <summary>
    /// The castle's three skills and their cooldowns. Pressing a ready skill applies it at once and starts its
    /// cooldown, even when nothing is in reach. The skills level raises every effect; cooldowns never change.
    /// Time comes in through <see cref="Tick"/>.
    /// </summary>
    internal sealed class CastleSkills : ICastleSkills
    {
        private readonly CastleConfig _config;
        private readonly ICastle _castle;
        private readonly IEnemyField _enemies;
        private readonly IHeroSquad _heroes;
        private readonly ICastleEnemyHits _hits;
        private readonly CastleSkill[] _skills;
        // Double so an 800-step cooldown of 0.025 s steps ends on exactly the 800th step.
        private readonly double[] _cooldownLeft;
        private readonly List<EnemyTarget> _alive = new List<EnemyTarget>();
        private int _level = 1;

        public CastleSkills(CastleConfig config, ICastle castle, IEnemyField enemies, IHeroSquad heroes, ICastleEnemyHits hits)
        {
            _hits = hits ?? throw new ArgumentNullException(nameof(hits));
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _castle = castle ?? throw new ArgumentNullException(nameof(castle));
            _enemies = enemies ?? throw new ArgumentNullException(nameof(enemies));
            _heroes = heroes ?? throw new ArgumentNullException(nameof(heroes));
            _skills = (CastleSkill[])(config.Skills ?? Array.Empty<CastleSkill>()).Clone();
            _cooldownLeft = new double[_skills.Length];
        }

        /// <summary>Thunder struck an enemy standing at this x (for the effect).</summary>
        public event Action<float> ThunderStruck;

        public IReadOnlyList<CastleSkill> Skills => _skills;

        /// <summary>True while any skill's cooldown is still running down.</summary>
        public bool AnyCooling
        {
            get
            {
                foreach (var left in _cooldownLeft)
                    if (left > 0d) return true;
                return false;
            }
        }

        public CastleSkillState GetState(CastleSkill skill)
        {
            var index = IndexOf(skill);
            if (index < 0) return new CastleSkillState(skill, false, 0f, 0f);
            var left = _cooldownLeft[index];
            return new CastleSkillState(skill, left <= 0d && CanAct(), (float)left, CooldownOf(skill));
        }

        public bool TryUse(CastleSkill skill)
        {
            var index = IndexOf(skill);
            if (index < 0 || _cooldownLeft[index] > 0d || !CanAct()) return false;

            Apply(skill);
            // A skill with no cooldown is ready again at once: Ready stays the one source the buttons read.
            var cooldown = CooldownOf(skill);
            _cooldownLeft[index] = cooldown <= CastleReach.TimeEpsilon ? 0d : cooldown;
            return true;
        }

        /// <summary>
        /// Cooldowns run down by one gameplay step of the shared clock (or any length of battle time in tests).
        /// A cooldown ends on the first step at which its full time has passed.
        /// </summary>
        public void Tick(float deltaSeconds)
        {
            if (deltaSeconds <= 0f) return;
            for (var i = 0; i < _cooldownLeft.Length; i++)
            {
                var left = _cooldownLeft[i] - deltaSeconds;
                _cooldownLeft[i] = left <= CastleReach.TimeEpsilon ? 0d : left;
            }
        }

        /// <summary>A new run: every skill ready, back to level 1.</summary>
        public void Reset()
        {
            _level = 1;
            Array.Clear(_cooldownLeft, 0, _cooldownLeft.Length);
        }

        public void ApplyLevel(int level)
        {
            if (level >= 1) _level = level;
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        /// <summary>Cheat: cooldown left set to 0..full cooldown (0 is ready); the skill is not used.</summary>
        public void ForceCooldownLeft(CastleSkill skill, float secondsLeft)
        {
            var index = IndexOf(skill);
            if (index < 0 || float.IsNaN(secondsLeft)) return;
            _cooldownLeft[index] = Math.Max(0d, Math.Min(CooldownOf(skill), secondsLeft));
        }
#endif

        private bool CanAct() => !_castle.IsDestroyed;

        private float Bonus => 1f + _config.SkillBonusPerLevel * (_level - 1);

        private void Apply(CastleSkill skill)
        {
            switch (skill)
            {
                case CastleSkill.Thunder:
                    var damage = _config.ThunderDamage * Bonus;
                    _enemies.GetAlive(_alive);
                    foreach (var enemy in _alive)
                    {
                        if (CastleReach.GapToCastle(enemy.X, enemy.HalfWidth, _config.HalfWidth) > _config.ThunderRange) continue;
                        // One hit per enemy in the zone, read when pressed (feature-combat: lightning_hits).
                        _hits.Hit(enemy.Handle, damage);
                        ThunderStruck?.Invoke(enemy.X);
                    }
                    break;
                case CastleSkill.Frost:
                    _enemies.SlowAll(_config.FrostSeconds * Bonus);
                    break;
                case CastleSkill.Rally:
                    _heroes.HealAll(_config.RallyFraction * Bonus);
                    break;
            }
        }

        private float CooldownOf(CastleSkill skill)
        {
            switch (skill)
            {
                case CastleSkill.Thunder: return _config.ThunderCooldown;
                case CastleSkill.Frost: return _config.FrostCooldown;
                case CastleSkill.Rally: return _config.RallyCooldown;
                default: return 0f;
            }
        }

        private int IndexOf(CastleSkill skill) => Array.IndexOf(_skills, skill);
    }
}
