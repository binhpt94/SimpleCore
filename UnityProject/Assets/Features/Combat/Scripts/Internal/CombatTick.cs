using System;
using System.Collections.Generic;

namespace Game.Combat
{
    /// <summary>
    /// One combat tick per fixed step: actors aim, actors strike, then the hits of the step are gathered,
    /// hits on sides that no longer take hits are dropped, the rest are summed per side, each owner takes its
    /// totals once, every owner reports its fallen sides, and the tick is reported done.
    /// </summary>
    internal sealed class CombatTick : ICombat
    {
        private readonly List<ICombatReceiver> _receivers = new List<ICombatReceiver>();
        private readonly List<ICombatActor> _actors = new List<ICombatActor>();
        private List<CombatHit> _incoming = new List<CombatHit>();
        private List<CombatHit> _settling = new List<CombatHit>();

        // Totals of the tick, in the order each side was first hit, so the result is the same on every run.
        private readonly List<Total> _totals = new List<Total>();
        private readonly Dictionary<(int receiver, int key), int> _totalIndex = new Dictionary<(int, int), int>();
        private readonly List<int> _hitReceivers = new List<int>();
        private readonly HashSet<int> _hitReceiverSet = new HashSet<int>();

        public event Action TickDone;

        public int AddReceiver(ICombatReceiver receiver)
        {
            if (receiver == null) throw new ArgumentNullException(nameof(receiver));
            _receivers.Add(receiver);
            return _receivers.Count - 1;
        }

        public void AddActor(ICombatActor actor)
        {
            if (actor == null) throw new ArgumentNullException(nameof(actor));
            _actors.Add(actor);
        }

        public void Hit(in CombatHit hit)
        {
            if (hit.Receiver < 0 || hit.Receiver >= _receivers.Count)
                throw new ArgumentException($"Combat: hit names receiver {hit.Receiver}, which was never added.", nameof(hit));
            if (float.IsNaN(hit.Damage) || hit.Damage < 0f)
                throw new ArgumentException($"Combat: hit damage must be 0 or more, got {hit.Damage}.", nameof(hit));
            _incoming.Add(hit);
        }

        /// <summary>Drops hits waiting for the next tick (a new run, or the battle was cleared).</summary>
        public void ClearPending() => _incoming.Clear();

        public void Run(float stepSeconds)
        {
            // 1. Move and aim, 2. strike: every actor works from the state at the start of the tick.
            for (var i = 0; i < _actors.Count; i++) _actors[i].Aim(stepSeconds);
            for (var i = 0; i < _actors.Count; i++) _actors[i].Strike(stepSeconds);

            // 3. Gather. Hits dealt while settling (for example by a fall) belong to the next tick.
            (_settling, _incoming) = (_incoming, _settling);

            // 4. Filter on the state right before any health of the tick is taken, 5. sum per side.
            for (var i = 0; i < _settling.Count; i++)
            {
                var hit = _settling[i];
                if (!_receivers[hit.Receiver].CanBeHit(hit.TargetKey)) continue;
                var id = (hit.Receiver, hit.TargetKey);
                if (_totalIndex.TryGetValue(id, out var index))
                {
                    _totals[index] = _totals[index].Add(hit.Damage);
                }
                else
                {
                    _totalIndex.Add(id, _totals.Count);
                    _totals.Add(new Total(hit.Receiver, hit.TargetKey, hit.Damage));
                }
            }
            _settling.Clear();

            // 6. Each owner takes its totals, once per side.
            for (var i = 0; i < _totals.Count; i++)
            {
                var total = _totals[i];
                _receivers[total.Receiver].TakeTotal(total.Key, total.Damage);
                if (_hitReceiverSet.Add(total.Receiver)) _hitReceivers.Add(total.Receiver);
            }

            // 7. Only once every owner took its health, owners report what fell.
            for (var i = 0; i < _hitReceivers.Count; i++) _receivers[_hitReceivers[i]].ReportFallen();

            _totals.Clear();
            _totalIndex.Clear();
            _hitReceivers.Clear();
            _hitReceiverSet.Clear();

            // 8. Tick done.
            TickDone?.Invoke();
        }

        private readonly struct Total
        {
            public Total(int receiver, int key, float damage)
            {
                Receiver = receiver;
                Key = key;
                Damage = damage;
            }

            public int Receiver { get; }
            public int Key { get; }
            public float Damage { get; }

            public Total Add(float damage) => new Total(Receiver, Key, Damage + damage);
        }
    }
}
