using System;
using System.Collections.Generic;
using Game.Enemy;

namespace Game.Castle
{
    /// <summary>
    /// The castle's auto shot (auto-shot-values): one shot every SecondsPerShot at the living, on-screen enemy
    /// nearest the castle edge within ShotRange, both sides; ties go to the enemy that came first. Damage is dealt
    /// when the shot fires. With nothing in range the timer waits, ready, and the next enemy in range is shot at once.
    /// </summary>
    internal sealed class CastleAutoShot
    {
        private readonly CastleConfig _config;
        private readonly ICastle _castle;
        private readonly IEnemyField _enemies;
        private readonly CastleUpgrades _upgrades;
        private readonly List<EnemyTarget> _alive = new List<EnemyTarget>();
        private float _sinceShot;

        public CastleAutoShot(CastleConfig config, ICastle castle, IEnemyField enemies, CastleUpgrades upgrades)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _castle = castle ?? throw new ArgumentNullException(nameof(castle));
            _enemies = enemies ?? throw new ArgumentNullException(nameof(enemies));
            _upgrades = upgrades ?? throw new ArgumentNullException(nameof(upgrades));
            Reset();
        }

        /// <summary>A shot left the castle edge at <c>fromX</c> towards the enemy <c>handle</c> at <c>toX</c>.</summary>
        public event Action<int, float, float> Fired;

        /// <summary>Damage of one shot at the current attack level.</summary>
        public float Damage => _config.ShotDamage * (1f + _config.AttackBonusPerLevel * (_upgrades.AttackLevel - 1));

        /// <summary>A new run: ready to shoot at once.</summary>
        public void Reset() => _sinceShot = _config.SecondsPerShot;

        /// <summary>Advances time; the caller does not call this while paused or outside a battle.</summary>
        public void Tick(float deltaSeconds)
        {
            if (_castle.IsDestroyed || deltaSeconds < 0f) return;

            _sinceShot = Math.Min(_config.SecondsPerShot, _sinceShot + deltaSeconds);
            if (_sinceShot < _config.SecondsPerShot - CastleReach.TimeEpsilon) return;
            if (!TryPickTarget(out var target)) return;

            _sinceShot = 0f;
            var fromX = target.X < 0f ? -_config.HalfWidth : _config.HalfWidth;
            _enemies.Damage(target.Handle, Damage);
            Fired?.Invoke(target.Handle, fromX, target.X);
        }

        private bool TryPickTarget(out EnemyTarget target)
        {
            target = default;
            var found = false;
            var bestGap = float.MaxValue;
            _enemies.GetAlive(_alive);
            foreach (var enemy in _alive)
            {
                if (Math.Abs(enemy.X) > _config.ScreenHalfWidth) continue;
                var gap = CastleReach.GapToCastle(enemy.X, enemy.HalfWidth, _config.HalfWidth);
                if (gap > _config.ShotRange) continue;
                if (found && (gap > bestGap || (gap == bestGap && enemy.SpawnOrder >= target.SpawnOrder))) continue;
                target = enemy;
                bestGap = gap;
                found = true;
            }
            return found;
        }
    }
}
