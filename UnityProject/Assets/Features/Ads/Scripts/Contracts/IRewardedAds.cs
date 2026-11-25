using System;

namespace Game.Ads
{
    /// <summary>
    /// Shows rewarded ads. The only way the game shows an ad; callers never talk to the ad provider. Knows nothing
    /// about the reward: the caller decides what a watched ad gives.
    /// </summary>
    public interface IRewardedAds
    {
        /// <summary>
        /// Asks for one ad for the given offer. <paramref name="onReply"/> is called exactly once with the offer id,
        /// unless the request is cancelled first. While another request runs, the reply is NotWatched in the same
        /// frame and the running request is not affected. Otherwise the reply comes only after the ad closed and the
        /// game is back on screen.
        /// </summary>
        void Show(int offerId, Action<AdReply> onReply);

        /// <summary>The caller gives up on the offer (left the battle, offer settled): a later result is dropped.</summary>
        void Cancel(int offerId);
    }
}
