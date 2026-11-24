using System;
using Game.Core;

namespace Game.Upgrade
{
    /// <summary>Connects the shop to the run and the wallet while the scene session runs.</summary>
    internal sealed class UpgradeSession : SessionParticipantBase
    {
        private readonly UpgradeShopLogic _shop;

        public UpgradeSession(UpgradeShopLogic shop)
        {
            _shop = shop ?? throw new ArgumentNullException(nameof(shop));
        }

        protected override void OnRun()
        {
            _shop.Attach();
            WhenDisposed(_shop.Detach);
        }
    }
}
