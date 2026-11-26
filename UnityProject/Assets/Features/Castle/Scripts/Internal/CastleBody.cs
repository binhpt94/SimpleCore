using System;
using System.Collections.Generic;
using Game.Combat;
using Game.Enemy;
using Game.Run;

namespace Game.Castle
{
    /// <summary>
    /// The castle's health. Enemies hit it through <see cref="IEnemyFoes"/>; when health reaches 0 the run is
    /// told it is lost (once), then <see cref="Destroyed"/> is raised. Health never regenerates; only the
    /// health upgrade adds to it.
    /// </summary>
    internal sealed class CastleBody : ICastle, IEnemyFoes, ICombatReceiver
    {
        private const int FoeKey = 0;

        private readonly CastleConfig _config;
        private readonly IRunFlow _runFlow;
        private bool _fallPending;

        public CastleBody(CastleConfig config, IRunFlow runFlow)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _runFlow = runFlow ?? throw new ArgumentNullException(nameof(runFlow));
            MaxHealth = config.MaxHealth;
            Health = MaxHealth;
        }

        public float Health { get; private set; }

        public float MaxHealth { get; private set; }

        public bool IsDestroyed { get; private set; }

        public event Action HealthChanged;

        public event Action Destroyed;

        /// <summary>An enemy hit the standing castle (for the hit flash).</summary>
        public event Action Struck;

        /// <summary>
        /// Where the hero in team slot <paramref name="slot"/> (0 = the first chosen) starts the battle:
        /// at a castle edge, x in u. Slots past the configured ones repeat the pattern.
        /// </summary>
        public float HeroStartX(int slot)
        {
            var points = _config.HeroSlotX;
            if (slot < 0) throw new ArgumentOutOfRangeException(nameof(slot), slot, "Team slot starts at 0.");
            if (points == null || points.Length == 0)
                throw new InvalidOperationException($"CastleConfig '{_config.name}' has no hero start points.");
            return points[slot % points.Length];
        }

        /// <summary>A new run: full health at level 1, standing again.</summary>
        public void Reset()
        {
            IsDestroyed = false;
            _fallPending = false;
            MaxHealth = _config.MaxHealth;
            Health = MaxHealth;
            HealthChanged?.Invoke();
        }

        /// <summary>Health upgrade: max health is level 1's plus a fixed amount per level; current health gains the same difference.</summary>
        public void ApplyHealthLevel(int level)
        {
            if (IsDestroyed || _fallPending || level < 1) return;
            var newMax = _config.MaxHealth + _config.HealthPerLevel * (level - 1);
            var added = newMax - MaxHealth;
            MaxHealth = newMax;
            Health = Math.Max(0f, Math.Min(MaxHealth, Health + added));
            HealthChanged?.Invoke();
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        /// <summary>Cheat: health set to 1..MaxHealth, never destroys; ignored once destroyed.</summary>
        public void ForceHealth(float health)
        {
            if (IsDestroyed || float.IsNaN(health)) return;
            Health = Math.Max(1f, Math.Min(MaxHealth, health));
            HealthChanged?.Invoke();
        }
#endif

        public void GetFoes(List<EnemyFoe> into)
        {
            into.Clear();
            if (!IsDestroyed && !_fallPending) into.Add(new EnemyFoe(FoeKey, 0f, _config.HalfWidth));
        }

        /// <summary>A hit straight on the castle, outside combat: health is taken and a fall is reported at once.</summary>
        public void Hit(int key, float damage)
        {
            if (!CanBeHit(key) || damage <= 0f) return;
            TakeTotal(key, damage);
            ReportFallen();
        }

        // Shared combat (feature-combat/combat-tick, PROTOTYPE): the enemies' hits on the castle in one tick come as
        // one total; the fall is reported once, after every owner took its totals.

        public bool CanBeHit(int key) => key == FoeKey && !IsDestroyed && !_fallPending;

        public void TakeTotal(int key, float total)
        {
            if (!CanBeHit(key) || total <= 0f) return;

            Health -= total;
            Struck?.Invoke();
            if (Health > 0f)
            {
                HealthChanged?.Invoke();
                return;
            }

            Health = 0f;
            _fallPending = true;
            HealthChanged?.Invoke();
        }

        public void ReportFallen()
        {
            if (!_fallPending) return;
            _fallPending = false;
            IsDestroyed = true;
            _runFlow.Lose();
            Destroyed?.Invoke();
        }
    }
}
