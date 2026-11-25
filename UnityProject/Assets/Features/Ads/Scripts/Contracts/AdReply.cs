namespace Game.Ads
{
    /// <summary>The result of one ad request, carrying the offer id the caller sent with it.</summary>
    public readonly struct AdReply
    {
        public AdReply(int offerId, AdResult result)
        {
            OfferId = offerId;
            Result = result;
        }

        /// <summary>The id of the offer that made the request; the caller checks it is still the offer waiting.</summary>
        public int OfferId { get; }

        public AdResult Result { get; }
    }
}
