namespace Game.Ads
{
    /// <summary>The only two results of an ad request. Why it failed goes to the log, not to the caller.</summary>
    public enum AdResult
    {
        /// <summary>The provider confirmed the ad was watched enough to earn the reward.</summary>
        Watched,

        /// <summary>No fill, no network, closed early, timed out or busy: no reward.</summary>
        NotWatched,
    }
}
