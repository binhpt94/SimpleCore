using System;

namespace Game.Upgrade
{
    /// <summary>The items on sale in this run, in bar order (heroes by slot, then the castle).</summary>
    public interface IUpgradeShop
    {
        int Count { get; }

        UpgradeItem Get(int index);

        /// <summary>Buys one level of the item at <paramref name="index"/>; pays through the wallet.</summary>
        PurchaseResult Buy(int index);

        /// <summary>Raised in the same frame any item's level, price or CanBuy changed, or the list was rebuilt.</summary>
        event Action ItemsChanged;
    }
}
