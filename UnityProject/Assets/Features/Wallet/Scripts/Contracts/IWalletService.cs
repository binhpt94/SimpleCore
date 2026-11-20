namespace Game.Wallet
{
    /// <summary>Requests that change the player's coins. The wallet decides; callers get a result, never an exception.</summary>
    public interface IWalletService
    {
        /// <summary>Takes <paramref name="amount"/> coins when the player has enough. Nothing changes unless the result is Ok.</summary>
        SpendResult TrySpend(int amount);
    }
}
