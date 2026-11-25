using Game.Core;

namespace Game.Services.SessionScene
{
    /// <summary>
    /// Reads what a participant still holds from <see cref="ISessionParticipantUsage"/>, which every
    /// <see cref="SessionParticipantBase"/> implements. A participant that does not implement it holds nothing
    /// as far as the session can tell. The scene-transition cover is never tracked, so it is never counted.
    /// </summary>
    internal sealed class ParticipantUsageCounter : IParticipantUsage
    {
        public ParticipantUsage CountFor(ISessionParticipant participant)
        {
            return participant is ISessionParticipantUsage usage
                ? new ParticipantUsage(usage.HeldViews, usage.RunningJobs, usage.OpensViewsOnRun)
                : new ParticipantUsage(0, 0);
        }
    }
}
