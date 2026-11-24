using System;

namespace Game.Wallet
{
    /// <summary>The player's coins in this run, for those who show or compare them. Read only.</summary>
    public interface IWalletBalance
    {
        int Coins { get; }

        /// <summary>Raised in the same frame the coins changed, with the new amount. Not raised when nothing changed.</summary>
        event Action<int> CoinsChanged;
    }
}
