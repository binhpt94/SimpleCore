namespace Game.Wallet
{
    /// <summary>Puts coins into the wallet. The caller decides how much; the wallet only adds.</summary>
    public interface IWalletIncome
    {
        /// <summary>Adds <paramref name="amount"/> coins. Zero is ignored; a negative amount is refused and logged.</summary>
        void Add(int amount);
    }
}
