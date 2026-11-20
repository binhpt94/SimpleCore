using System;

namespace Game.Wallet
{
    /// <summary>The only place the coin count lives and changes.</summary>
    internal sealed class WalletLogic : IWalletService
    {
        internal WalletLogic(int startCoins)
        {
            Coins = Math.Max(0, startCoins);
        }

        internal int Coins { get; private set; }

        internal event Action<CoinsChanged> CoinsChanged;

        public SpendResult TrySpend(int amount)
        {
            if (amount <= 0) return SpendResult.InvalidAmount;
            if (amount > Coins) return SpendResult.NotEnoughCoins;

            SetCoins(Coins - amount);
            return SpendResult.Ok;
        }

        /// <summary>Adds coins. Returns false and changes nothing when the amount is not positive.</summary>
        internal bool Grant(int amount)
        {
            if (amount <= 0) return false;

            SetCoins(Coins + amount);
            return true;
        }

        private void SetCoins(int value)
        {
            var previous = Coins;
            Coins = value;
            CoinsChanged?.Invoke(new CoinsChanged(previous, value));
        }
    }
}
