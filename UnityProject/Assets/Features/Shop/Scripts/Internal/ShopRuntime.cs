namespace Game.Shop
{
    /// <summary>Where this feature's own View finds the running logic. Set by the installer; null before boot.</summary>
    internal static class ShopRuntime
    {
        internal static ShopLogic Logic { get; set; }
    }
}
