using System;
using System.Collections.Generic;
using Game.Enemy;

namespace Game.Wave
{
    /// <summary>Random numbers for shuffling; behind an interface so tests can fix the order.</summary>
    internal interface IWaveRandom
    {
        /// <summary>A number from 0 up to, not including, <paramref name="maxExclusive"/>.</summary>
        int Next(int maxExclusive);
    }

    internal sealed class SystemWaveRandom : IWaveRandom
    {
        private readonly Random _random;

        public SystemWaveRandom(int? seed = null)
        {
            _random = seed.HasValue ? new Random(seed.Value) : new Random();
        }

        public int Next(int maxExclusive) => _random.Next(maxExclusive);
    }

    /// <summary>One entry of a wave's send list.</summary>
    internal readonly struct SpawnEntry
    {
        public SpawnEntry(EnemyType type, EnemySide side)
        {
            Type = type;
            Side = side;
        }

        public EnemyType Type { get; }
        public EnemySide Side { get; }
    }

    /// <summary>
    /// spawn-order: shuffles the kinds (Boss always last), then gives exactly half to each side,
    /// never more than <see cref="WaveRules.MaxSameSideRun"/> in a row on one side.
    /// </summary>
    internal sealed class SpawnOrder
    {
        private readonly WaveRules _rules;
        private readonly IWaveRandom _random;

        public SpawnOrder(WaveRules rules, IWaveRandom random)
        {
            _rules = rules ?? throw new ArgumentNullException(nameof(rules));
            _random = random ?? throw new ArgumentNullException(nameof(random));
        }

        public List<SpawnEntry> Build(WavePlan plan)
        {
            var kinds = new List<EnemyType>(plan.Count);
            Add(kinds, EnemyType.Soldier, plan.Soldiers);
            Add(kinds, EnemyType.Wolf, plan.Wolves);
            Add(kinds, EnemyType.Giant, plan.Giants);
            Add(kinds, EnemyType.Archer, plan.Archers);
            for (var i = kinds.Count - 1; i > 0; i--)
            {
                var j = _random.Next(i + 1);
                (kinds[i], kinds[j]) = (kinds[j], kinds[i]);
            }
            Add(kinds, EnemyType.Boss, plan.Bosses);

            var sides = Sides(kinds.Count);
            var list = new List<SpawnEntry>(kinds.Count);
            for (var i = 0; i < kinds.Count; i++) list.Add(new SpawnEntry(kinds[i], sides[i]));
            return list;
        }

        private List<EnemySide> Sides(int count)
        {
            var max = _rules.MaxSameSideRun;
            var left = count / 2;
            var right = count - left;
            var sides = new List<EnemySide>(count);
            var last = EnemySide.Left;
            var run = 0;
            while (left + right > 0)
            {
                var canLeft = left > 0 && Fits(EnemySide.Left, left, right, last, run, max);
                var canRight = right > 0 && Fits(EnemySide.Right, left, right, last, run, max);
                EnemySide pick;
                if (canLeft && canRight) pick = _random.Next(left + right) < left ? EnemySide.Left : EnemySide.Right;
                else if (canLeft) pick = EnemySide.Left;
                else if (canRight) pick = EnemySide.Right;
                else pick = left > 0 ? EnemySide.Left : EnemySide.Right; // only with an odd count or a run limit that cannot be met

                if (pick == EnemySide.Left) left--;
                else right--;
                run = sides.Count > 0 && pick == last ? run + 1 : 1;
                last = pick;
                sides.Add(pick);
            }
            return sides;
        }

        /// <summary>True when taking <paramref name="pick"/> now still leaves a way to place the rest within the run limit.</summary>
        private static bool Fits(EnemySide pick, int left, int right, EnemySide last, int run, int max)
        {
            var newRun = run > 0 && pick == last ? run + 1 : 1;
            if (newRun > max) return false;
            var same = pick == EnemySide.Left ? left - 1 : right - 1;
            var other = pick == EnemySide.Left ? right : left;
            // The side just used can still take (max - newRun) now, then max after each item of the other side;
            // the other side can take max before each item of this side and once more at the end.
            return same <= (max - newRun) + max * other && other <= max * (same + 1);
        }

        private static void Add(List<EnemyType> into, EnemyType type, int count)
        {
            for (var i = 0; i < count; i++) into.Add(type);
        }
    }
}
