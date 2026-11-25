using System;

namespace Game.SimClock
{
    /// <summary>
    /// One named stream (SplitMix64). Its start comes only from the run's seed and the stream's id, so the order in
    /// which features ask for streams never shifts the numbers (random-values#stream-from-name).
    /// </summary>
    internal sealed class SimRandomStream : ISimRandomStream
    {
        private const ulong Golden = 0x9E3779B97F4A7C15UL;

        private readonly ulong _nameHash;
        private ulong _state;
        private bool _ready;

        internal SimRandomStream(string id)
        {
            Id = id;
            _nameHash = NameHash(id);
        }

        internal string Id { get; }

        internal bool Ready => _ready;

        /// <summary>Back to the first number of the stream for <paramref name="seed"/>.</summary>
        internal void Reset(int seed)
        {
            unchecked
            {
                _state = Mix(((ulong)(uint)seed * Golden) ^ _nameHash);
            }
            _ready = true;
        }

        /// <summary>The run is over: drawing throws until the next run resets the stream.</summary>
        internal void Clear()
        {
            _ready = false;
            _state = 0;
        }

        public int Next(int maxExclusive)
        {
            if (maxExclusive <= 0)
                throw new ArgumentOutOfRangeException(nameof(maxExclusive), maxExclusive, "Next needs a bound above 0.");
            if (!_ready)
                throw new InvalidOperationException(
                    $"Random stream '{Id}' has no run: draw only between SetUpRun and ClearRun.");
            unchecked
            {
                _state += Golden;
                var z = Mix(_state);
                return (int)(((z >> 32) * (ulong)maxExclusive) >> 32);
            }
        }

        private static ulong Mix(ulong z)
        {
            unchecked
            {
                z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
                z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
                return z ^ (z >> 31);
            }
        }

        /// <summary>FNV-1a over the lower-case id: stable across runs, machines and .NET versions.</summary>
        private static ulong NameHash(string id)
        {
            unchecked
            {
                var h = 0xCBF29CE484222325UL;
                foreach (var c in id.ToLowerInvariant())
                {
                    h ^= c;
                    h *= 0x100000001B3UL;
                }
                return h;
            }
        }
    }
}
