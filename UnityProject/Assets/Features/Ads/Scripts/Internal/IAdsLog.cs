namespace Game.Ads
{
    /// <summary>Where the feature writes why ads failed. The player never sees it.</summary>
    internal interface IAdsLog
    {
        /// <summary>A request ended as NotWatched.</summary>
        void NotWatched(int offerId, AdFailReason reason);

        /// <summary>A result came for an offer the caller had cancelled; nobody receives it.</summary>
        void Dropped(int offerId, AdResult result);
    }
}
