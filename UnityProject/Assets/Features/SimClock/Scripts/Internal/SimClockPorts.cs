using System.Security.Cryptography;
using UnityEngine;

namespace Game.SimClock
{
    /// <summary>Where a release run's seed comes from; tests replace it.</summary>
    internal interface ISeedSource
    {
        /// <summary>A whole number from <paramref name="min"/> to <paramref name="max"/>, both included.</summary>
        int Next(int min, int max);
    }

    /// <summary>Where the run's seed is written (release: the game log; dev: the console).</summary>
    internal interface ISeedLog
    {
        void Write(string line);
    }

    /// <summary>The length of the last frame in real seconds.</summary>
    internal interface IFrameTime
    {
        double FrameSeconds { get; }
    }

    /// <summary>Picks release seeds from the system's secure random, never from the gameplay streams.</summary>
    internal sealed class SystemSeedSource : ISeedSource
    {
        public int Next(int min, int max)
        {
            var bytes = new byte[8];
            using (var rng = RandomNumberGenerator.Create()) rng.GetBytes(bytes);
            var value = System.BitConverter.ToUInt64(bytes, 0);
            var span = (ulong)((long)max - min + 1);
            return (int)(min + (long)(value % span));
        }
    }

    /// <summary>Writes the seed line to Unity's log (console in the Editor, player log in a build).</summary>
    internal sealed class UnitySeedLog : ISeedLog
    {
        public void Write(string line) => Debug.Log(line);
    }

    /// <summary>Real frame time, not scaled, so the engine's time scale never slows the battle (clock-step).</summary>
    internal sealed class UnityFrameTime : IFrameTime
    {
        public double FrameSeconds => Time.unscaledDeltaTime;
    }
}
