using System;
using System.Collections.Generic;
using Game.Enemy;
using Game.Run;

namespace Game.Castle
{
    /// <summary>
    /// The castle's health. Enemies hit it through <see cref="IEnemyFoes"/>; when health reaches 0 the run is
    /// told it is lost (once), then <see cref="Destroyed"/> is raised. Health never regenerates; only the
    /// health upgrade adds to it.
    /// </summary>
    internal sealed class CastleBody : ICastle, IEnemyFoes
    {
        private const int FoeKey = 0;

        private readonly CastleConfig _config;
        private readonly IRunFlow _runFlow;

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

        /// <summary>A new run: full health at level 1, standing again.</summary>
        public void Reset()
        {
            IsDestroyed = false;
            MaxHealth = _config.MaxHealth;
            Health = MaxHealth;
            HealthChanged?.Invoke();
        }

        /// <summary>Health upgrade: max health is level 1's plus a fixed amount per level; current health gains the same difference.</summary>
        public void ApplyHealthLevel(int level)
        {
            if (IsDestroyed || level < 1) return;
            var newMax = _config.MaxHealth + _config.HealthPerLevel * (level - 1);
            var added = newMax - MaxHealth;
            MaxHealth = newMax;
            Health = Math.Max(0f, Math.Min(MaxHealth, Health + added));
            HealthChanged?.Invoke();
        }

        public void GetFoes(List<EnemyFoe> into)
        {
            into.Clear();
            if (!IsDestroyed) into.Add(new EnemyFoe(FoeKey, 0f, _config.HalfWidth));
        }

        public void Hit(int key, float damage)
        {
            if (key != FoeKey || IsDestroyed || damage <= 0f) return;

            Health -= damage;
            Struck?.Invoke();
            if (Health > 0f)
            {
                HealthChanged?.Invoke();
                return;
            }

            Health = 0f;
            IsDestroyed = true;
            HealthChanged?.Invoke();
            _runFlow.Lose();
            Destroyed?.Invoke();
        }
    }
}
