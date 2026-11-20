namespace Game.Wallet
{
    /// <summary>Where this feature's own View finds the running logic. Set by the installer; null before boot.</summary>
    internal static class WalletRuntime
    {
        internal static WalletLogic Logic { get; set; }
    }
}
