using UnityEngine;

namespace Game.Ads
{
    internal sealed class UnityAdsLog : IAdsLog
    {
        public void NotWatched(int offerId, AdFailReason reason) =>
            Debug.Log($"[Ads] offer {offerId}: not watched ({reason})");

        public void Dropped(int offerId, AdResult result) =>
            Debug.Log($"[Ads] offer {offerId}: result {result} dropped, the caller cancelled");
    }
}
