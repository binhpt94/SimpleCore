using System;
using System.Collections.Generic;
using Game.Combat;
using Game.Enemy;
using Game.Run;

namespace Game.Hero
{
    /// <summary>
    /// The team on the battlefield, by slot, and its rules: spawn, target, walk, hit, fall, respawn, level
    /// (live: feature-hero/*). Enemies see the targetable heroes through <see cref="IEnemyFoes"/> (key = slot).
    /// Pure C#: time comes in through <see cref="Aim"/> and <see cref="Strike"/>, once per combat tick (one per gameplay
    /// step of the shared clock), or through <see cref="Tick"/> without the shared combat.
    /// </summary>
    internal sealed class HeroSquad : IHeroSquad, IEnemyFoes, IHeroSpawnPoints, IHeroDownState, IHeroRespawnTime, ICombatReceiver
    {
        private const float Epsilon = 1e-4f;

        private readonly HeroConfig _config;
        private readonly IEnemyField _field;
        private readonly IHeroHits _hits;

        /// <summary>Heroes whose health reached 0 this combat tick, not yet reported as fallen.</summary>
        private readonly List<HeroUnit> _emptied = new List<HeroUnit>();
        private readonly List<HeroUnit> _units = new List<HeroUnit>();
        private readonly List<EnemyTarget> _alive = new List<EnemyTarget>();
        private readonly HashSet<int> _defeatedThisFrame = new HashSet<int>();
        private readonly List<int> _struck = new List<int>();

        /// <summary>Start and respawn x by slot for the next run; the config's values until the castle gives its own.</summary>
        private readonly List<float> _spawnX = new List<float>();

        /// <param name="hits">Where the heroes' hits go; null: straight onto the enemy field (no shared combat).</param>
        public HeroSquad(HeroConfig config, IEnemyField field, IHeroHits hits = null)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _field = field ?? throw new ArgumentNullException(nameof(field));
            _hits = hits ?? new DirectHeroHits(field);
            _spawnX.AddRange(_config.SlotSpawnX);
        }

        /// <summary>Raised with the slot when its hero goes down or stands up again (IHeroDownState).</summary>
        public event Action<int> DownChanged;

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

        /// <summary>How many heroes a team going into battle has: one per slot.</summary>
        public int TeamSize => _config.SlotSpawnX.Count;

        private float HalfWidth => _config.BoxWidth * 0.5f;

        /// <summary>A run starts: the team spawns at its slots' spawn points, at level 1 and full health.</summary>
        public void Begin(RunTeam team)
        {
            var wereDown = DownSlots();
            _units.Clear();
            _emptied.Clear();
            var codes = team.HeroCodes;
            for (var slot = 0; slot < codes.Count; slot++)
            {
                var hero = _config.Find(codes[slot])
                           ?? throw new InvalidOperationException($"Hero code '{codes[slot]}' of slot {slot + 1} is not in the hero config.");
                if (slot >= _spawnX.Count)
                    throw new InvalidOperationException($"There is no spawn point for slot {slot + 1}.");
                _units.Add(new HeroUnit(slot, hero, _spawnX[slot], HeroLevelStats.At(hero, 1, _config)));
            }
            TeamChanged?.Invoke();
            RaiseDownChanged(wereDown);
        }

        /// <summary>The battle is cleared: every hero leaves.</summary>
        public void Clear()
        {
            if (_units.Count == 0) return;
            var wereDown = DownSlots();
            _units.Clear();
            _emptied.Clear();
            TeamChanged?.Invoke();
            RaiseDownChanged(wereDown);
        }

        // ------------------------------------------------------------------ IHeroDownState

        public bool IsDown(int slot)
        {
            var unit = FindSlot(slot);
            return unit != null && !unit.IsStanding;
        }

        // ------------------------------------------------------------------ IHeroRespawnTime

        public float RespawnSecondsLeft(int slot)
        {
            var unit = FindSlot(slot);
            return unit == null || unit.IsStanding ? 0f : Math.Max(0f, unit.RespawnLeft);
        }

        private List<int> DownSlots()
        {
            var slots = new List<int>();
            foreach (var unit in _units)
                if (!unit.IsStanding) slots.Add(unit.Slot);
            return slots;
        }

        private void RaiseDownChanged(List<int> slots)
        {
            foreach (var slot in slots) DownChanged?.Invoke(slot);
        }

        // ------------------------------------------------------------------ IHeroSpawnPoints

        /// <summary>
        /// The points are read when a run starts, so heroes already on the field keep theirs until the next run.
        /// A list without a point for every slot is refused and the current points stay.
        /// </summary>
        public void SetSpawnX(IReadOnlyList<float> xBySlot)
        {
            if (xBySlot == null) throw new ArgumentNullException(nameof(xBySlot));
            if (xBySlot.Count < TeamSize)
                throw new ArgumentException(
                    $"Hero spawn points cover {xBySlot.Count} slot(s); slot {xBySlot.Count + 1} of {TeamSize} has no point.",
                    nameof(xBySlot));
            _spawnX.Clear();
            for (var slot = 0; slot < TeamSize; slot++) _spawnX.Add(xBySlot[slot]);
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

        /// <summary>An enemy hit a hero directly, without the shared combat: health is taken and a fall reported at once.</summary>
        public void Hit(int key, float damage)
        {
            TakeTotal(key, damage);
            ReportFallen();
        }

        // ------------------------------------------------------------------ ICombatReceiver (shared combat)

        /// <summary>Only a standing hero that finished growing back takes hits.</summary>
        public bool CanBeHit(int key)
        {
            var unit = FindSlot(key);
            return unit != null && unit.IsTargetable;
        }

        /// <summary>Takes the step's total from the hero; a hero left at 0 falls when <see cref="ReportFallen"/> comes.</summary>
        public void TakeTotal(int key, float total)
        {
            var unit = FindSlot(key);
            if (unit == null || !unit.IsTargetable || total <= 0f || _emptied.Contains(unit)) return;
            unit.Health -= total;
            if (unit.Health > 0f)
            {
                Hurt?.Invoke(unit);
                return;
            }
            unit.Health = 0f;
            _emptied.Add(unit);
        }

        public void ReportFallen()
        {
            if (_emptied.Count == 0) return;
            var fallen = _emptied.ToArray();
            _emptied.Clear();
            foreach (var unit in fallen)
            {
                if (!_units.Contains(unit)) continue;
                unit.IsStanding = false;
                unit.IsMoving = false;
                unit.TargetHandle = HeroUnit.NoTarget;
                unit.RespawnLeft = _config.RespawnSeconds;
                Fell?.Invoke(unit);
                DownChanged?.Invoke(unit.Slot);
            }
        }

        // ------------------------------------------------------------------ battle step

        /// <summary>
        /// Moves the battle on by <paramref name="deltaSeconds"/> without the shared combat: each hero in turn waits,
        /// walks and hits, so a hero sees enemies defeated by the heroes before it in the same step.
        /// </summary>
        public void Tick(float deltaSeconds)
        {
            if (deltaSeconds <= 0f || _units.Count == 0) return;
            _field.GetAlive(_alive);
            _defeatedThisFrame.Clear();
            foreach (var unit in _units)
            {
                AimUnit(unit, deltaSeconds);
                StrikeUnit(unit);
            }
        }

        /// <summary>
        /// First part of a combat tick (ICombatActor.Aim): every hero counts its waits, respawns, picks its target and
        /// walks, all from the enemies as they stand at the start of the tick.
        /// </summary>
        public void Aim(float deltaSeconds)
        {
            if (deltaSeconds <= 0f || _units.Count == 0) return;
            _field.GetAlive(_alive);
            _defeatedThisFrame.Clear();
            foreach (var unit in _units) AimUnit(unit, deltaSeconds);
        }

        /// <summary>Second part of a combat tick (ICombatActor.Strike): every hero in range whose wait is over hits.</summary>
        public void Strike()
        {
            foreach (var unit in _units) StrikeUnit(unit);
        }

        private void StrikeUnit(HeroUnit unit)
        {
            if (!unit.IsStanding || unit.GrowLeft > 0f || unit.Mode != HeroMode.Attacking || unit.Cooldown > 0f) return;
            if (TryFindAlive(unit.TargetHandle, out var target)) Strike(unit, target);
        }

        private void AimUnit(HeroUnit unit, float dt)
        {
            if (!unit.IsStanding)
            {
                // A wait of t seconds ends on the first step whose counted time reaches t (feature-sim-clock clock-values);
                // Epsilon absorbs the float error of adding up 0.025 s steps.
                unit.RespawnLeft -= dt;
                if (unit.RespawnLeft <= Epsilon) Respawn(unit);
                return;
            }
            if (unit.GrowLeft > 0f)
            {
                unit.GrowLeft = Done(unit.GrowLeft - dt);
                unit.IsMoving = false;
                return;
            }

            unit.Cooldown = Done(unit.Cooldown - dt);
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
                if (_hits.Hit(handle, damage)) _defeatedThisFrame.Add(handle);

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
            DownChanged?.Invoke(unit.Slot);
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

        /// <summary>Time left of a wait; 0 once it is within <see cref="Epsilon"/> of the end.</summary>
        private static float Done(float left) => left <= Epsilon ? 0f : left;

        private static float Clamp(float x, float halfRange) => Math.Max(-halfRange, Math.Min(halfRange, x));

        private HeroUnit FindSlot(int slot)
        {
            foreach (var unit in _units)
                if (unit.Slot == slot) return unit;
            return null;
        }
    }
}
