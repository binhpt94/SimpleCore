using System;

namespace Game.Upgrade
{
    /// <summary>
    /// The player's buy taps (feature-sim-clock clock-values#input-next-step): a tap is queued and applied at the start
    /// of the next step of the shared clock, in the order tapped, and checked again then by the shop's own rules.
    /// <see cref="IUpgradeShop.Buy"/> stays the immediate purchase (dev cheat state, tests); player input uses this.
    /// </summary>
    public interface IUpgradeOrders
    {
        /// <summary>Queues one level of the item at <paramref name="index"/>; nothing changes until the next step.</summary>
        void Order(int index);

        /// <summary>Raised when a queued order was applied: the item's index and the outcome of the purchase.</summary>
        event Action<int, PurchaseResult> OrderApplied;
    }
}
