using System;

namespace Game.Wallet
{
    /// <summary>
    /// The only place the coin count lives and changes. Requests are handled one at a time, in the order they
    /// arrive, and every change is announced before the next request is looked at.
    /// </summary>
    internal sealed class WalletLogic : IWalletService, IWalletBalance, IWalletIncome
    {
        /// <summary>Upper bound of the balance; only there to keep the arithmetic safe.</summary>
        internal const int MaxCoins = 999_999_999;

        private readonly int startCoins;
        private readonly IWalletLog log;

        private Action<int> balanceChanged;

        internal WalletLogic(int startCoins, IWalletLog log)
        {
            this.startCoins = Math.Min(MaxCoins, Math.Max(0, startCoins));
            this.log = log ?? throw new ArgumentNullException(nameof(log));
            Coins = this.startCoins;
        }

        public int Coins { get; private set; }

        /// <summary>Raised after the coin count changed, with both values.</summary>
        internal event Action<CoinsChanged> CoinsChanged;

        event Action<int> IWalletBalance.CoinsChanged
        {
            add => balanceChanged += value;
            remove => balanceChanged -= value;
        }

        public SpendResult TrySpend(int amount)
        {
            if (amount < 0)
            {
                log.Refused($"Wallet: spending a negative amount ({amount}) was refused; coins stay at {Coins}.");
                return SpendResult.InvalidAmount;
            }

            if (amount > Coins) return SpendResult.NotEnoughCoins;
            if (amount == 0) return SpendResult.Ok;

            SetCoins(Coins - amount);
            return SpendResult.Ok;
        }

        public void Add(int amount)
        {
            if (amount < 0)
            {
                log.Refused($"Wallet: adding a negative amount ({amount}) was refused; coins stay at {Coins}.");
                return;
            }

            var next = amount > MaxCoins - Coins ? MaxCoins : Coins + amount;
            if (next == Coins) return;

            SetCoins(next);
        }

        /// <summary>Puts the balance back to the start coins of a run. Always announces, even when unchanged.</summary>
        internal void ResetToStart()
        {
            SetCoins(startCoins);
        }

        private void SetCoins(int value)
        {
            var previous = Coins;
            Coins = value;
            CoinsChanged?.Invoke(new CoinsChanged(previous, value));
            balanceChanged?.Invoke(value);
        }
    }
}
