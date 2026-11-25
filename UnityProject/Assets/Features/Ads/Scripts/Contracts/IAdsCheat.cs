#if UNITY_EDITOR || DEVELOPMENT_BUILD
namespace Game.Ads
{
    /// <summary>
    /// Dev-only control of the fake ad provider (feature-ads/ads-request/ads-values), for cheat states. Not in release builds.
    /// </summary>
    public interface IAdsCheat
    {
        /// <summary>
        /// The next ad request stays waiting: no reply and no time limit, until it is cancelled or the scene session
        /// ends. Requests after that one behave as usual.
        /// </summary>
        void HoldNextAd();
    }
}
#endif
