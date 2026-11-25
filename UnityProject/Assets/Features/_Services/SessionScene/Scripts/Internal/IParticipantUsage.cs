namespace Game.Services.SessionScene
{
    /// <summary>
    /// Counts what one session participant still holds: views and running jobs (timers, pending waits,
    /// per-frame loops). Dev builds only. Behind an interface so tests count with a fake.
    /// </summary>
    internal interface IParticipantUsage
    {
        ParticipantUsage CountFor(Game.Core.ISessionParticipant participant);
    }

    /// <summary>What one participant holds at the moment it was counted.</summary>
    internal readonly struct ParticipantUsage
    {
        public ParticipantUsage(int views, int runningJobs)
        {
            Views = views;
            RunningJobs = runningJobs;
        }

        public int Views { get; }

        public int RunningJobs { get; }

        public bool IsEmpty => Views == 0 && RunningJobs == 0;
    }
}
