#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections.Generic;
using Game.Upgrade;
using Game.Wallet;

namespace Game.HudUpgrade
{
    /// <summary>
    /// Dev only. Builds a cheat state of the bar through the game's own paths: each level is bought
    /// from the shop (topping the wallet up first when short), then the wallet is brought to the wanted coins.
    /// Prices and lit/dim stay the shop's.
    /// </summary>
    internal sealed class UpgradeBarCheat
    {
        private const int MaxBuysPerItem = 1000;

        private readonly IUpgradeShop _shop;
        private readonly IWalletBalance _balance;
        private readonly IWalletIncome _income;
        private readonly IWalletService _wallet;

        public UpgradeBarCheat(IUpgradeShop shop, IWalletBalance balance, IWalletIncome income, IWalletService wallet)
        {
            _shop = shop ?? throw new ArgumentNullException(nameof(shop));
            _balance = balance ?? throw new ArgumentNullException(nameof(balance));
            _income = income ?? throw new ArgumentNullException(nameof(income));
            _wallet = wallet ?? throw new ArgumentNullException(nameof(wallet));
        }

        /// <summary>
        /// Raises item i to <paramref name="levels"/>[i] (never lowers it), then sets the coins to <paramref name="coins"/>.
        /// Returns false when the shop refused a purchase (for example the battle is not running).
        /// </summary>
        public bool Apply(int coins, IReadOnlyList<int> levels)
        {
            if (levels == null) throw new ArgumentNullException(nameof(levels));
            var ok = true;
            var count = Math.Min(_shop.Count, levels.Count);
            for (var i = 0; i < count; i++)
            {
                for (var n = 0; n < MaxBuysPerItem && _shop.Get(i).Level < levels[i]; n++)
                {
                    var price = _shop.Get(i).NextPrice;
                    if (_balance.Coins < price) _income.Add(price - _balance.Coins);
                    if (_shop.Buy(i) != PurchaseResult.Bought)
                    {
                        ok = false;
                        break;
                    }
                }
            }

            var diff = coins - _balance.Coins;
            if (diff > 0) _income.Add(diff);
            else if (diff < 0) _wallet.TrySpend(-diff);
            return ok;
        }

        /// <summary>
        /// Compares the shop's hero items, in bar order, with the team the state wants.
        /// Returns null when they match, otherwise a message naming both teams.
        /// </summary>
        public string FindTeamMismatch(IReadOnlyList<string> wantedTeam)
        {
            if (wantedTeam == null || wantedTeam.Count == 0) return null;
            var actual = new List<string>();
            for (var i = 0; i < _shop.Count; i++)
            {
                var item = _shop.Get(i);
                if (item.Kind == UpgradeKind.Hero) actual.Add(item.HeroCode);
            }

            var same = actual.Count == wantedTeam.Count;
            for (var i = 0; same && i < actual.Count; i++) same = actual[i] == wantedTeam[i];
            return same
                ? null
                : $"đội đang là [{string.Join(", ", actual)}], state cần [{string.Join(", ", wantedTeam)}]: chọn đúng đội ở màn chọn đội.";
        }
    }
}
#endif
