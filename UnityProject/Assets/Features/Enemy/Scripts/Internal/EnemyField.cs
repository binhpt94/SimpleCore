using System;
using System.Collections.Generic;
using Game.Combat;
using Game.Wallet;

namespace Game.Enemy
{
    /// <summary>
    /// The enemies of the battlefield (live/feature-enemy). Plain C#: every living enemy walks toward the castle
    /// centre, stops at the first foe in range and hits it on its own rhythm; it takes damage, pays its coins and
    /// raises <see cref="Defeated"/> once.
    /// <para>
    /// From PROTOTYPE the shared combat drives it (live/feature-combat/combat-tick/tick-values, tick-order): once per
    /// combat tick, <see cref="Aim"/> walks and picks targets from the state at the start of the tick, then
    /// <see cref="Strike"/> deals the hits through <see cref="ICombat.Hit"/>; combat hands each enemy hit one total in
    /// <see cref="TakeTotal"/> and <see cref="ReportFallen"/> defeats and pays each fallen enemy once.
    /// <see cref="Tick"/> runs Aim and Strike together for callers without combat.
    /// </para>
    /// </summary>
    /// <remarks>
    /// Foe groups are tried in the order they were added: a group added earlier wins when foes of several groups are
    /// in range (the heroes are installed before the castle, so enemies hit a blocking hero rather than the castle).
    /// Distance is the gap along x between the two boxes, 0 when they overlap (feature-hero/hero-stats/hero-level-values).
    /// </remarks>
    internal sealed class EnemyField : IEnemyField, IEnemyCombat, ICombatReceiver, ICombatActor
    {
        // Step lengths summed as floats fall a hair short of whole seconds; a hit that is due within this is due now,
        // so a wait of t seconds ends on the first step where the time counted reaches t (clock-values#timer-in-steps).
        private const float HitWaitTolerance = 1e-4f;

        private readonly EnemyConfig _config;
        private readonly IWalletIncome _income;
        private readonly List<Unit> _alive = new List<Unit>();
        private readonly List<Unit> _tickBuffer = new List<Unit>();
        private readonly List<FoeGroup> _groups = new List<FoeGroup>();
        private readonly List<Unit> _fallen = new List<Unit>();
        private ICombat _combat;
        private int _combatReceiver = -1;
        private int _nextHandle = 1;
        private int _nextSpawnOrder;
        private bool _stopped;

        public EnemyField(EnemyConfig config, IWalletIncome income)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _income = income ?? throw new ArgumentNullException(nameof(income));
        }

        public event Action<EnemyDefeat> Defeated;

        /// <summary>An enemy came onto the battlefield.</summary>
        public event Action<EnemyLook> Spawned;

        /// <summary>An enemy hit its foe; the int is the direction it faces (+1 right, -1 left).</summary>
        public event Action<int, int> Struck;

        /// <summary>A ranged enemy hit its foe: a shot flies from the enemy to the foe (the damage already landed).</summary>
        public event Action<EnemyShot> Shot;

        /// <summary>A defeated enemy paid its coins: where it stood and how much.</summary>
        public event Action<EnemyReward> Rewarded;

        /// <summary>An enemy was hit and is still alive.</summary>
        public event Action<int> Hurt;

        /// <summary>An enemy whose type has an entrance came on screen: its centre first reached the entrance distance.</summary>
        public event Action<int> Entered;

        /// <summary>Every enemy was taken off the battlefield without being defeated.</summary>
        public event Action Cleared;

        public int AliveCount => _alive.Count;

        /// <summary>True after the battle stopped: enemies stand still and nobody is defeated or paid.</summary>
        public bool IsStopped => _stopped;

        public void Spawn(EnemySpawn spawn)
        {
            var kind = _config.Kind(spawn.Type);
            if (!kind.Available)
                throw new InvalidOperationException(
                    $"Enemy type {spawn.Type} is not in the game at this stage (EnemyConfig '{_config.name}'): " +
                    "the wave config asks for a type that cannot be spawned yet.");

            var scale = spawn.StatScale > 0f ? spawn.StatScale : 1f;
            var x = spawn.Side == EnemySide.Left ? -_config.SpawnDistance : _config.SpawnDistance;
            var unit = new Unit(_nextHandle++, _nextSpawnOrder++, spawn.Type, spawn.Side, kind, scale, x);
            _alive.Add(unit);
            Spawned?.Invoke(unit.Look());
            CheckEntrance(unit);
        }

        public void GetAlive(List<EnemyTarget> into)
        {
            if (into == null) throw new ArgumentNullException(nameof(into));
            into.Clear();
            for (var i = 0; i < _alive.Count; i++)
            {
                var unit = _alive[i];
                into.Add(new EnemyTarget(unit.Handle, unit.Side, unit.X, unit.HalfWidth, unit.SpawnOrder));
            }
        }

        /// <summary>Clears <paramref name="into"/> and fills it with how every living enemy looks now.</summary>
        public void GetLooks(List<EnemyLook> into)
        {
            if (into == null) throw new ArgumentNullException(nameof(into));
            into.Clear();
            for (var i = 0; i < _alive.Count; i++) into.Add(_alive[i].Look());
        }

        public bool Damage(int handle, float amount)
        {
            if (_stopped || amount <= 0f) return false;
            var index = IndexOf(handle);
            if (index < 0) return false;

            var unit = _alive[index];
            if (unit.Health <= 0f) return false;
            unit.Health -= amount;
            if (unit.Health > 0f)
            {
                Hurt?.Invoke(unit.Handle);
                return false;
            }

            Defeat(unit);
            return true;
        }

        /// <summary>The number combat gave the enemies; only after <see cref="JoinCombat"/>.</summary>
        public int CombatReceiver =>
            _combat != null
                ? _combatReceiver
                : throw new InvalidOperationException("The enemies have not joined combat yet (EnemyField.JoinCombat).");

        /// <summary>Adds the enemies to combat as a receiver (hits on enemies) and as an actor (enemies walk and hit).</summary>
        public void JoinCombat(ICombat combat)
        {
            if (combat == null) throw new ArgumentNullException(nameof(combat));
            if (_combat != null) throw new InvalidOperationException("The enemies already joined combat.");
            _combat = combat;
            _combatReceiver = combat.AddReceiver(this);
            combat.AddActor(this);
        }

        public void AddCombatFoes(IEnemyFoes foes, int combatReceiver)
        {
            if (foes == null) throw new ArgumentNullException(nameof(foes));
            if (_combat == null)
                throw new InvalidOperationException("The enemies have not joined combat yet: combat foes cannot be added.");

            // A group already added the old way moves to combat and keeps its place in the try order.
            var group = GroupOf(foes);
            if (group != null) group.Receiver = combatReceiver;
            else _groups.Add(new FoeGroup(foes, combatReceiver));
        }

        public bool CanBeHit(int key)
        {
            if (_stopped) return false;
            var index = IndexOf(key);
            return index >= 0 && _alive[index].Health > 0f;
        }

        public void TakeTotal(int key, float total)
        {
            if (_stopped || total <= 0f) return;
            var index = IndexOf(key);
            if (index < 0) return;

            var unit = _alive[index];
            if (unit.Health <= 0f) return;
            unit.Health -= total;
            if (unit.Health > 0f) Hurt?.Invoke(unit.Handle);
            else _fallen.Add(unit);
        }

        public void ReportFallen()
        {
            // Defeated once each, coins once each, however many sides the total came from (tick-values#kill-credit).
            for (var i = 0; i < _fallen.Count; i++)
            {
                var unit = _fallen[i];
                if (!_stopped && unit.Alive) Defeat(unit);
            }

            _fallen.Clear();
        }

        public void SlowAll(float seconds)
        {
            if (_stopped || seconds <= 0f) return;
            for (var i = 0; i < _alive.Count; i++) _alive[i].SlowLeft = seconds;
        }

        public void AddFoes(IEnemyFoes foes)
        {
            if (foes == null) throw new ArgumentNullException(nameof(foes));
            if (GroupOf(foes) != null) return;
            _groups.Add(new FoeGroup(foes, -1));
        }

        /// <summary>Moves the battle on by <paramref name="seconds"/> without combat: <see cref="Aim"/> then <see cref="Strike"/>.</summary>
        public void Tick(float seconds)
        {
            Aim(seconds);
            Strike(seconds);
        }

        /// <summary>First part of a combat tick: every enemy walks, or stops at the foe it picks from the state at the start of the tick.</summary>
        public void Aim(float stepSeconds)
        {
            if (_stopped || stepSeconds <= 0f || _alive.Count == 0) return;

            for (var g = 0; g < _groups.Count; g++) _groups[g].Foes.GetFoes(_groups[g].Snapshot);

            _tickBuffer.Clear();
            _tickBuffer.AddRange(_alive);
            for (var i = 0; i < _tickBuffer.Count; i++)
            {
                var unit = _tickBuffer[i];
                if (unit.Alive) Move(unit, stepSeconds);
            }

            _tickBuffer.Clear();
        }

        /// <summary>Second part of a combat tick: every enemy standing at a foe whose hit wait ran out hits it.</summary>
        public void Strike(float stepSeconds)
        {
            if (_stopped || _alive.Count == 0) return;

            _tickBuffer.Clear();
            _tickBuffer.AddRange(_alive);
            for (var i = 0; i < _tickBuffer.Count; i++)
            {
                if (_stopped) break;
                var unit = _tickBuffer[i];
                if (unit.Alive && unit.Health > 0f) Hit(unit);
            }

            _tickBuffer.Clear();
        }

        /// <summary>A new run begins: no enemy left, the battle may act again.</summary>
        public void ResetForRun()
        {
            _stopped = false;
            Clear();
        }

        /// <summary>The castle fell or the run was abandoned: enemies stand still, nobody is defeated or paid.</summary>
        public void Stop() => _stopped = true;

        /// <summary>The player left the battle: every enemy is removed, none counts as defeated.</summary>
        public void Clear()
        {
            _fallen.Clear();
            if (_alive.Count == 0) return;
            for (var i = 0; i < _alive.Count; i++) _alive[i].Alive = false;
            _alive.Clear();
            Cleared?.Invoke();
        }

        private void Defeat(Unit unit)
        {
            _alive.Remove(unit);
            unit.Alive = false;
            _income.Add(unit.Kind.Coins);
            Rewarded?.Invoke(new EnemyReward(unit.Handle, unit.X, unit.Kind.Height, unit.Kind.Coins));
            Defeated?.Invoke(new EnemyDefeat(unit.Handle, unit.Type, unit.Side));
        }

        private void Hit(Unit unit)
        {
            if (unit.TargetGroup < 0 || unit.HitWait > HitWaitTolerance) return;

            unit.HitWait = unit.Kind.SecondsPerHit;
            var group = _groups[unit.TargetGroup];
            if (group.Receiver >= 0)
                _combat.Hit(new CombatHit(group.Receiver, unit.TargetKey, unit.HitDamage, CombatSource.Enemy));
            else
                group.Foes.Hit(unit.TargetKey, unit.HitDamage);

            Struck?.Invoke(unit.Handle, unit.Direction);
            if (unit.Kind.Ranged)
                Shot?.Invoke(new EnemyShot(unit.Handle, unit.X, unit.Kind.Height * 0.5f, unit.TargetX));
        }

        private FoeGroup GroupOf(IEnemyFoes foes)
        {
            for (var i = 0; i < _groups.Count; i++)
            {
                if (_groups[i].Foes == foes) return _groups[i];
            }

            return null;
        }

        private void Move(Unit unit, float seconds)
        {
            var slowed = unit.SlowLeft > 0f;
            var slowedFor = slowed ? Math.Min(unit.SlowLeft, seconds) : 0f;
            // Frost ends on the first step where the time counted reaches it, like any wait (clock-values#timer-in-steps).
            var slowLeft = unit.SlowLeft - seconds;
            unit.SlowLeft = slowLeft > HitWaitTolerance ? slowLeft : 0f;

            // The hit wait runs all the time, also while walking; slowed time counts at the slowed rate.
            var hitFactor = _config.SlowHitFactor > 0f ? _config.SlowHitFactor : 1f;
            unit.HitWait = Math.Max(0f, unit.HitWait - (seconds - slowedFor) - slowedFor / hitFactor);

            if (FindFoe(unit, out var group, out var foe))
            {
                unit.Walking = false;
                unit.TargetGroup = group;
                unit.TargetKey = foe.Key;
                unit.TargetX = foe.X;
                return;
            }

            unit.TargetGroup = -1;
            var walked = unit.Kind.MoveSpeed * ((seconds - slowedFor) + _config.SlowMoveFactor * slowedFor);

            // Never walk past the castle centre into the other side.
            var limit = unit.HalfWidth;
            var next = unit.X + unit.Direction * walked;
            next = unit.Direction > 0 ? Math.Min(next, -limit) : Math.Max(next, limit);
            unit.Walking = next != unit.X;
            unit.X = next;
            CheckEntrance(unit);
        }

        private void CheckEntrance(Unit unit)
        {
            if (unit.Entered || !unit.Kind.Entrance) return;
            if (Math.Abs(unit.X) > _config.EntranceDistance) return;
            unit.Entered = true;
            Entered?.Invoke(unit.Handle);
        }

        private bool FindFoe(Unit unit, out int group, out EnemyFoe foe)
        {
            // The first group with a foe in range wins; inside that group the enemy keeps the foe it is already
            // hitting while it stays in range, otherwise it takes the nearest one.
            for (var g = 0; g < _groups.Count; g++)
            {
                var foes = _groups[g].Snapshot;
                var best = -1;
                var bestGap = float.MaxValue;
                for (var i = 0; i < foes.Count; i++)
                {
                    var gap = Gap(unit, foes[i]);
                    if (gap > unit.Kind.Range) continue;
                    if (unit.TargetGroup == g && foes[i].Key == unit.TargetKey)
                    {
                        best = i;
                        break;
                    }

                    if (gap < bestGap)
                    {
                        best = i;
                        bestGap = gap;
                    }
                }

                if (best >= 0)
                {
                    group = g;
                    foe = foes[best];
                    return true;
                }
            }

            group = -1;
            foe = default;
            return false;
        }

        private static float Gap(Unit unit, EnemyFoe foe) =>
            Math.Max(0f, Math.Abs(unit.X - foe.X) - unit.HalfWidth - foe.HalfWidth);

        private int IndexOf(int handle)
        {
            for (var i = 0; i < _alive.Count; i++)
            {
                if (_alive[i].Handle == handle) return i;
            }

            return -1;
        }

        /// <summary>A group of foes; hits go through combat to <see cref="Receiver"/>, or straight to the foes when it is -1.</summary>
        private sealed class FoeGroup
        {
            public FoeGroup(IEnemyFoes foes, int receiver)
            {
                Foes = foes;
                Receiver = receiver;
            }

            public IEnemyFoes Foes { get; }
            public int Receiver { get; set; }
            public List<EnemyFoe> Snapshot { get; } = new List<EnemyFoe>();
        }

        private sealed class Unit
        {
            public Unit(int handle, int spawnOrder, EnemyType type, EnemySide side, EnemyKind kind, float scale, float x)
            {
                Handle = handle;
                SpawnOrder = spawnOrder;
                Type = type;
                Side = side;
                Kind = kind;
                HalfWidth = kind.Width * 0.5f;
                HitDamage = kind.Damage * scale;
                Health = kind.Health * scale;
                Direction = side == EnemySide.Left ? 1 : -1;
                X = x;
            }

            public int Handle { get; }
            public int SpawnOrder { get; }
            public EnemyType Type { get; }
            public EnemySide Side { get; }
            public EnemyKind Kind { get; }
            public float HalfWidth { get; }
            public float HitDamage { get; }
            public int Direction { get; }
            public float Health { get; set; }
            public float X { get; set; }
            public float SlowLeft { get; set; }
            public float HitWait { get; set; }
            public bool Walking { get; set; } = true;
            public int TargetGroup { get; set; } = -1;
            public int TargetKey { get; set; }
            public float TargetX { get; set; }
            public bool Alive { get; set; } = true;
            public bool Entered { get; set; }

            public EnemyLook Look() =>
                new EnemyLook(Handle, Type, X, Kind.Width, Kind.Height, Direction, Walking, SlowLeft > 0f);
        }
    }

    /// <summary>A shot of a ranged enemy: from the enemy at <see cref="FromX"/>, <see cref="Y"/> to the foe at <see cref="ToX"/>.</summary>
    internal readonly struct EnemyShot
    {
        public EnemyShot(int handle, float fromX, float y, float toX)
        {
            Handle = handle;
            FromX = fromX;
            Y = y;
            ToX = toX;
        }

        public int Handle { get; }
        public float FromX { get; }
        public float Y { get; }
        public float ToX { get; }
    }

    /// <summary>Coins paid by one defeated enemy, and where it stood (x, top of its box).</summary>
    internal readonly struct EnemyReward
    {
        public EnemyReward(int handle, float x, float top, int coins)
        {
            Handle = handle;
            X = x;
            Top = top;
            Coins = coins;
        }

        public int Handle { get; }
        public float X { get; }
        public float Top { get; }
        public int Coins { get; }
    }

    /// <summary>How one enemy looks at the moment it was read: what the view draws.</summary>
    internal readonly struct EnemyLook
    {
        public EnemyLook(int handle, EnemyType type, float x, float width, float height, int direction, bool walking,
            bool slowed)
        {
            Handle = handle;
            Type = type;
            X = x;
            Width = width;
            Height = height;
            Direction = direction;
            Walking = walking;
            Slowed = slowed;
        }

        public int Handle { get; }
        public EnemyType Type { get; }
        public float X { get; }
        public float Width { get; }
        public float Height { get; }

        /// <summary>+1 faces right (comes from the left), -1 faces left.</summary>
        public int Direction { get; }

        public bool Walking { get; }
        public bool Slowed { get; }
    }
}
