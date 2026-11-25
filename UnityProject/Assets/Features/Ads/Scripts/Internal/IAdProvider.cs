namespace Game.Ads
{
    /// <summary>
    /// The ad network behind the feature (the dev fake now, a real SDK later). Only <see cref="AdRequests"/> talks to it.
    /// </summary>
    internal interface IAdProvider
    {
        /// <summary>Starts loading and showing one ad. Every event of that ad goes to <paramref name="events"/>, maybe synchronously.</summary>
        void Begin(IAdProviderEvents events);

        /// <summary>Stops the current ad if it can; events that still arrive are ignored by the caller.</summary>
        void Abort();

        /// <summary>
        /// The caller cancelled the current ad. A real ad keeps playing (its result is dropped when it comes): return
        /// false. Return true only when this provider ended the ad here (an ad that would never end by itself); the
        /// request is then dropped at once.
        /// </summary>
        bool EndOnCancel();
    }

    /// <summary>What a provider reports about the one ad it was asked to show.</summary>
    internal interface IAdProviderEvents
    {
        /// <summary>The ad started playing.</summary>
        void Started();

        /// <summary>The provider gives up or reports a problem (no fill, no network, closed early).</summary>
        void Failed(AdFailReason reason);

        /// <summary>The provider confirmed the ad was watched enough to earn the reward.</summary>
        void Rewarded();

        /// <summary>The ad closed.</summary>
        void Closed();
    }
}
