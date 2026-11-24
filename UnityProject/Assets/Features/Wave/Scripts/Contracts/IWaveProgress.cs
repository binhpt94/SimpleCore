using System;

namespace Game.Wave
{
    /// <summary>Which wave the run is on, and when waves start and are cleared. Waves are numbered from 1.</summary>
    public interface IWaveProgress
    {
        /// <summary>The wave being fought, or the next one during the short break after a clear.</summary>
        int CurrentWave { get; }

        /// <summary>Raised after <see cref="CurrentWave"/> changed (at a clear, and when a new run starts), with the new number.</summary>
        event Action<int> CurrentWaveChanged;

        /// <summary>Raised when a wave begins sending enemies, with its number.</summary>
        event Action<int> WaveStarted;

        /// <summary>Raised once per wave when its last enemy fell, after the clear reward was paid, with its number.</summary>
        event Action<int> WaveCleared;
    }
}
