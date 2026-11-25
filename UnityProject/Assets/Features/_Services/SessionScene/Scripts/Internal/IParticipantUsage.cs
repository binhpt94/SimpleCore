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
        public ParticipantUsage(int views, int runningJobs, bool opensViewsOnRun = false)
        {
            Views = views;
            RunningJobs = runningJobs;
            OpensViewsOnRun = opensViewsOnRun;
        }

        public int Views { get; }

        public int RunningJobs { get; }

        /// <summary>
        /// The participant said it creates its views in the run step, so right after that step it must hold at
        /// least one. False for participants with no view and for those whose views only come later.
        /// </summary>
        public bool OpensViewsOnRun { get; }

        public bool IsEmpty => Views == 0 && RunningJobs == 0;
    }
}
