namespace Game.Ads
{
    /// <summary>Release builds until a real ad network is chosen: there is never an ad.</summary>
    internal sealed class NoAdProvider : IAdProvider
    {
        public void Begin(IAdProviderEvents events) => events?.Failed(AdFailReason.NoFill);

        public void Abort()
        {
        }
    }
}
