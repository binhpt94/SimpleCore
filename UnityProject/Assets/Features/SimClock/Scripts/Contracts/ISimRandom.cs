namespace Game.SimClock
{
    /// <summary>Seeded random numbers of the run (feature-sim-clock/seeded-random). One stream per name, each made from the run's seed and its name.</summary>
    public interface ISimRandom
    {
        /// <summary>
        /// The stream called <paramref name="name"/>. The same object stays valid across runs: it is reset from each new run's seed.
        /// A name missing from this feature's stream list is an error raised here.
        /// </summary>
        ISimRandomStream Stream(string name);
    }

    /// <summary>One named random stream; each draw takes the next number of the stream.</summary>
    public interface ISimRandomStream
    {
        /// <summary>A whole number from 0 up to, not including, <paramref name="maxExclusive"/> (must be above 0).</summary>
        int Next(int maxExclusive);
    }
}
