using System;
using System.Collections.Generic;
using Game.Enemy;
using Game.Run;

namespace Game.Hero
{
    /// <summary>
    /// The team on the battlefield, by slot, and its rules: spawn, target, walk, hit, fall, respawn, level
    /// (live: feature-hero/*). Enemies see the targetable heroes through <see cref="IEnemyFoes"/> (key = slot).
    /// Pure C#: time comes in through <see cref="Tick"/>.
    /// </summary>
    internal sealed class HeroSquad : IHeroSquad, IEnemyFoes
    {
        private const float Epsilon = 1e-4f;

        private readonly HeroConfig _config;
        private readonly IEnemyField _field;
        private readonly List<HeroUnit> _units = new List<HeroUnit>();
        private readonly List<EnemyTarget> _alive = new List<EnemyTarget>();
        private readonly HashSet<int> _defeatedThisFrame = new HashSet<int>();
        private readonly List<int> _struck = new List<int>();

        public HeroSquad(HeroConfig config, IEnemyField field)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _field = field ?? throw new ArgumentNullException(nameof(field));
        }

        /// <summary>The team was replaced (a run started) or removed (battle cleared).</summary>
        public event Action TeamChanged;

        public event Action<HeroUnit, HeroStrike> Struck;

        /// <summary>A hit landed on the hero and it is still standing.</summary>
        public event Action<HeroUnit> Hurt;

        public event Action<HeroUnit> Fell;

        public event Action<HeroUnit> Respawned;

        public event Action<HeroUnit> LevelledUp;

        /// <summary>The team, slot 1 first; empty outside a run.</summary>
        public IReadOnlyList<HeroUnit> Units => _units;

        private float HalfWidth => _config.BoxWidth * 0.5f;

        /// <summary>A run starts: the team spawns at its slots' spawn points, at level 1 and full health.</summary>
        public void Begin(RunTeam team)
        {
            _units.Clear();
            var codes = team.HeroCodes;
            for (var slot = 0; slot < codes.Count; slot++)
            {
                var hero = _config.Find(codes[slot])
                           ?? throw new InvalidOperationException($"Hero code '{codes[slot]}' of slot {slot + 1} is not in the hero config.");
                if (slot >= _config.SlotSpawnX.Count)
                    throw new InvalidOperationException($"The hero config has no spawn point for slot {slot + 1}.");
                _units.Add(new HeroUnit(slot, hero, _config.SlotSpawnX[slot], HeroLevelStats.At(hero, 1, _config)));
            }
            TeamChanged?.Invoke();
        }

        /// <summary>The battle is cleared: every hero leaves.</summary>
        public void Clear()
        {
            if (_units.Count == 0) return;
            _units.Clear();
            TeamChanged?.Invoke();
        }

        // ------------------------------------------------------------------ IHeroSquad

        public void HealAll(float fractionOfMaxHealth)
        {
            if (fractionOfMaxHealth <= 0f) return;
            foreach (var unit in _units)
            {
                if (!unit.IsStanding) continue;
                var max = unit.Stats.MaxHealth;
                unit.Health = Math.Min(max, unit.Health + max * fractionOfMaxHealth);
            }
        }

        public void ApplyLevel(int slot, int level)
        {
            if (level < 1) throw new ArgumentOutOfRangeException(nameof(level), level, "Hero levels start at 1.");
            var unit = FindSlot(slot);
            if (unit == null || unit.Level == level) return;

            var raised = level > unit.Level;
            var before = unit.Stats.MaxHealth;
            unit.Level = level;
            unit.Stats = HeroLevelStats.At(unit.Hero, level, _config);
            if (unit.IsStanding)
            {
                var after = unit.Stats.MaxHealth;
                unit.Health = Math.Min(after, unit.Health + Math.Max(0f, after - before));
            }
            // A fallen hero respawns at the new level's full health.
            if (raised) LevelledUp?.Invoke(unit);
        }

        // ------------------------------------------------------------------ IEnemyFoes

        public void GetFoes(List<EnemyFoe> into)
        {
            if (into == null) throw new ArgumentNullException(nameof(into));
            into.Clear();
            foreach (var unit in _units)
                if (unit.IsTargetable) into.Add(new EnemyFoe(unit.Slot, unit.X, HalfWidth));
        }

        public void Hit(int key, float damage)
        {
            var unit = FindSlot(key);
            if (unit == null || !unit.IsTargetable || damage <= 0f) return;
            unit.Health -= damage;
            if (unit.Health > 0f)
            {
                Hurt?.Invoke(unit);
                return;
            }
            unit.Health = 0f;
            unit.IsStanding = false;
            unit.IsMoving = false;
            unit.TargetHandle = HeroUnit.NoTarget;
            unit.RespawnLeft = _config.RespawnSeconds;
            Fell?.Invoke(unit);
        }

        // ------------------------------------------------------------------ battle step

        /// <summary>Moves the battle on by <paramref name="deltaSeconds"/>: every hero waits, walks or hits.</summary>
        public void Tick(float deltaSeconds)
        {
            if (deltaSeconds <= 0f || _units.Count == 0) return;
            _field.GetAlive(_alive);
            _defeatedThisFrame.Clear();
            foreach (var unit in _units) TickUnit(unit, deltaSeconds);
        }

        private void TickUnit(HeroUnit unit, float dt)
        {
            if (!unit.IsStanding)
            {
                unit.RespawnLeft -= dt;
                if (unit.RespawnLeft <= 0f) Respawn(unit);
                return;
            }
            if (unit.GrowLeft > 0f)
            {
                unit.GrowLeft = Math.Max(0f, unit.GrowLeft - dt);
                unit.IsMoving = false;
                return;
            }

            unit.Cooldown = Math.Max(0f, unit.Cooldown - dt);
            var range = unit.Hero.range;

            var target = default(EnemyTarget);
            var keep = unit.Mode == HeroMode.Attacking
                       && TryFindAlive(unit.TargetHandle, out target)
                       && Gap(unit.X, target) <= range + Epsilon;
            if (!keep && !TrySelectNearest(unit.X, out target))
            {
                unit.TargetHandle = HeroUnit.NoTarget;
                WalkHome(unit, dt);
                return;
            }

            unit.TargetHandle = target.Handle;
            unit.Facing = target.X >= unit.X ? 1 : -1;
            unit.IsMoving = false;

            var gap = Gap(unit.X, target);
            if (gap > range + Epsilon)
            {
                var step = Math.Min(unit.Hero.moveSpeed * dt, gap - range);
                var newX = Clamp(unit.X + unit.Facing * step, _config.WalkHalfRange);
                unit.IsMoving = Math.Abs(newX - unit.X) > Epsilon * 0.1f;
                unit.X = newX;
                if (Gap(unit.X, target) > range + Epsilon)
                {
                    unit.Mode = HeroMode.Approaching;
                    return;
                }
            }

            unit.Mode = HeroMode.Attacking;
            if (unit.Cooldown <= 0f) Strike(unit, target);
        }

        private void WalkHome(HeroUnit unit, float dt)
        {
            unit.Mode = HeroMode.Waiting;
            var toHome = unit.SpawnX - unit.X;
            if (Math.Abs(toHome) <= Epsilon)
            {
                unit.X = unit.SpawnX;
                unit.IsMoving = false;
                return;
            }
            unit.Facing = toHome > 0f ? 1 : -1;
            var step = Math.Min(unit.Hero.moveSpeed * dt, Math.Abs(toHome));
            unit.X += unit.Facing * step;
            unit.IsMoving = true;
        }

        private void Strike(HeroUnit unit, EnemyTarget target)
        {
            var hero = unit.Hero;
            unit.Cooldown = unit.Stats.SecondsPerAttack;

            _struck.Clear();
            _struck.Add(target.Handle);
            switch (hero.attack)
            {
                case HeroAttack.FrontCleave:
                    // From the hero's centre (so enemies overlapping it count) to areaSize past its front edge,
                    // only enemies on the target's side by centre.
                    var far = unit.X + unit.Facing * (HalfWidth + hero.areaSize);
                    var lo = Math.Min(unit.X, far);
                    var hi = Math.Max(unit.X, far);
                    foreach (var enemy in _alive)
                    {
                        if (!IsAvailable(enemy) || enemy.Handle == target.Handle) continue;
                        var sameSide = unit.Facing > 0 ? enemy.X >= unit.X : enemy.X <= unit.X;
                        if (sameSide && enemy.X + enemy.HalfWidth >= lo && enemy.X - enemy.HalfWidth <= hi)
                            _struck.Add(enemy.Handle);
                    }
                    break;
                case HeroAttack.Blast:
                    foreach (var enemy in _alive)
                    {
                        if (!IsAvailable(enemy) || enemy.Handle == target.Handle) continue;
                        if (Math.Abs(enemy.X - target.X) - enemy.HalfWidth <= hero.areaSize) _struck.Add(enemy.Handle);
                    }
                    break;
            }

            var damage = unit.Stats.Damage;
            foreach (var handle in _struck)
                if (_field.Damage(handle, damage)) _defeatedThisFrame.Add(handle);

            var area = hero.attack == HeroAttack.SingleTarget ? 0f : hero.areaSize;
            var front = unit.X + unit.Facing * HalfWidth;
            Struck?.Invoke(unit, new HeroStrike(hero.group, hero.attack, unit.X, front, target.X, unit.Facing, area));
        }

        private void Respawn(HeroUnit unit)
        {
            unit.IsStanding = true;
            unit.X = unit.SpawnX;
            unit.Health = unit.Stats.MaxHealth;
            unit.GrowLeft = _config.RespawnGrowSeconds;
            unit.RespawnLeft = 0f;
            unit.Cooldown = 0f;
            unit.Mode = HeroMode.Waiting;
            unit.TargetHandle = HeroUnit.NoTarget;
            unit.IsMoving = false;
            Respawned?.Invoke(unit);
        }

        // ------------------------------------------------------------------ targeting helpers

        /// <summary>Gap along x between the hero's box and the enemy's box; 0 when they overlap.</summary>
        private float Gap(float heroX, EnemyTarget enemy) =>
            Math.Max(0f, Math.Abs(heroX - enemy.X) - HalfWidth - enemy.HalfWidth);

        private bool IsAvailable(EnemyTarget enemy) => !_defeatedThisFrame.Contains(enemy.Handle);

        private bool TryFindAlive(int handle, out EnemyTarget found)
        {
            foreach (var enemy in _alive)
            {
                if (enemy.Handle != handle || !IsAvailable(enemy)) continue;
                found = enemy;
                return true;
            }
            found = default;
            return false;
        }

        /// <summary>Nearest on-screen enemy; ties go to the one closer to the castle, then the one that came first.</summary>
        private bool TrySelectNearest(float heroX, out EnemyTarget best)
        {
            best = default;
            var found = false;
            var bestGap = 0f;
            foreach (var enemy in _alive)
            {
                if (!IsAvailable(enemy) || Math.Abs(enemy.X) > _config.TargetHalfRange) continue;
                var gap = Gap(heroX, enemy);
                if (found)
                {
                    if (gap > bestGap + Epsilon) continue;
                    if (Math.Abs(gap - bestGap) <= Epsilon)
                    {
                        var closer = Math.Abs(enemy.X) < Math.Abs(best.X) - Epsilon;
                        var level = Math.Abs(Math.Abs(enemy.X) - Math.Abs(best.X)) <= Epsilon;
                        if (!closer && !(level && enemy.SpawnOrder < best.SpawnOrder)) continue;
                    }
                }
                best = enemy;
                bestGap = gap;
                found = true;
            }
            return found;
        }

        private static float Clamp(float x, float halfRange) => Math.Max(-halfRange, Math.Min(halfRange, x));

        private HeroUnit FindSlot(int slot)
        {
            foreach (var unit in _units)
                if (unit.Slot == slot) return unit;
            return null;
        }
    }
}
