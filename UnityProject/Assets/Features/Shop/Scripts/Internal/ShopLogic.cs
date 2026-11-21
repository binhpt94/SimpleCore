using System.Collections.Generic;
using Game.Services.Popup;
using Game.Wallet;

namespace Game.Shop
{
    /// <summary>Decides whether a purchase goes through. Coins are the wallet's; the shop only asks.</summary>
    internal sealed class ShopLogic
    {
        private readonly IWalletService wallet;
        private readonly IPopupService popups;
        private readonly IReadOnlyList<ShopItem> items;

        internal ShopLogic(IWalletService wallet, IPopupService popups, IReadOnlyList<ShopItem> items)
        {
            this.wallet = wallet;
            this.popups = popups;
            this.items = items;
        }

        internal PurchaseResult TryPurchase(string itemId)
        {
            if (!TryFind(itemId, out var item)) return PurchaseResult.UnknownItem;

            // A free or mispriced item makes the wallet answer InvalidAmount; treat every refusal as "not bought".
            if (wallet.TrySpend(item.Price) != SpendResult.Ok) return PurchaseResult.NotEnoughCoins;

            popups.Show(ShopPopups.Purchased);
            return PurchaseResult.Ok;
        }

        private bool TryFind(string itemId, out ShopItem found)
        {
            foreach (var item in items)
            {
                if (item.Id == itemId)
                {
                    found = item;
                    return true;
                }
            }
            found = default;
            return false;
        }
    }
}
