using System;
using UnityEngine;

namespace Game.Enemy
{
    /// <summary>
    /// Balance numbers of the enemies (live/feature-enemy: enemy-type-values, enemy-stat-values, reward-values,
    /// enemy-move-values). Read only at runtime.
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Enemy/Enemy Config", fileName = "EnemyConfig")]
    internal sealed class EnemyConfig : ScriptableObject
    {
        [Tooltip("Distance from the castle centre where enemies appear (u); left side is negative.")]
        [SerializeField] private float _spawnDistance = 17f;

        [Tooltip("Walk speed multiplier while slowed by the castle's Frost.")]
        [SerializeField] private float _slowMoveFactor = 0.5f;

        [Tooltip("Seconds-per-hit multiplier while slowed by the castle's Frost.")]
        [SerializeField] private float _slowHitFactor = 2f;

        [SerializeField] private EnemyKind[] _kinds = Array.Empty<EnemyKind>();

        public float SpawnDistance => _spawnDistance;
        public float SlowMoveFactor => _slowMoveFactor;
        public float SlowHitFactor => _slowHitFactor;

        /// <summary>The row of <paramref name="type"/>; throws when the config has no row for it.</summary>
        public EnemyKind Kind(EnemyType type)
        {
            if (_kinds != null)
            {
                for (var i = 0; i < _kinds.Length; i++)
                {
                    if (_kinds[i] != null && _kinds[i].Type == type) return _kinds[i];
                }
            }

            throw new InvalidOperationException($"EnemyConfig '{name}' has no row for enemy type {type}.");
        }

        /// <summary>Names of the enemy types without a row; empty when every type has one.</summary>
        public string MissingKinds()
        {
            var missing = "";
            foreach (EnemyType type in Enum.GetValues(typeof(EnemyType)))
            {
                var found = false;
                if (_kinds != null)
                {
                    for (var i = 0; i < _kinds.Length; i++)
                    {
                        if (_kinds[i] != null && _kinds[i].Type == type) { found = true; break; }
                    }
                }

                if (!found) missing += (missing.Length == 0 ? "" : ", ") + type;
            }

            return missing;
        }
    }

    /// <summary>Base numbers of one enemy type at wave 1.</summary>
    [Serializable]
    internal sealed class EnemyKind
    {
        [SerializeField] private EnemyType _type;
        [SerializeField] private float _width = 1f;
        [SerializeField] private float _height = 1.2f;
        [SerializeField] private float _health = 40f;
        [SerializeField] private float _damage = 5f;
        [SerializeField] private float _secondsPerHit = 1f;
        [SerializeField] private float _range = 1f;
        [SerializeField] private float _moveSpeed = 1.5f;
        [SerializeField] private int _coins = 5;

        [Tooltip("On: hits from afar; every hit shows a shot flying to the foe (damage still lands at once).")]
        [SerializeField] private bool _ranged;

        [Tooltip("Off: this type is not in the game at the current stage; asking to spawn it is an error.")]
        [SerializeField] private bool _available = true;

        public EnemyType Type => _type;
        public bool Available => _available;
        public float Width => _width;
        public float Height => _height;
        public float Health => _health;
        public float Damage => _damage;
        public float SecondsPerHit => _secondsPerHit;
        public float Range => _range;
        public float MoveSpeed => _moveSpeed;
        public int Coins => _coins;
        public bool Ranged => _ranged;
    }
}
