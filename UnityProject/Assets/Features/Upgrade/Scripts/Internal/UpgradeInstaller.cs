using System;
using Game.Castle;
using Game.Core;
using Game.Hero;
using Game.Run;
using Game.SimClock;
using Game.Wallet;
using UnityEngine;

namespace Game.Upgrade
{
    /// <summary>Wires the upgrade shop: publishes <see cref="IUpgradeShop"/> and <see cref="IUpgradeOrders"/> and joins the scene session.</summary>
    [CreateAssetMenu(menuName = "Game/Upgrade/Installer", fileName = "UpgradeInstaller")]
    public sealed class UpgradeInstaller : InstallerAsset
    {
        [SerializeField] private UpgradeConfig _config;

        public override void Install(IServiceRegistry registry)
        {
            if (registry == null) throw new ArgumentNullException(nameof(registry));
            if (_config == null) throw new InvalidOperationException("UpgradeInstaller: UpgradeConfig is missing.");

            var session = registry.Resolve<ISessionScene>();
            var shop = new UpgradeShopLogic(
                _config.ToPrices(),
                registry.Resolve<IRunFlow>(),
                registry.Resolve<IWalletService>(),
                registry.Resolve<IWalletBalance>(),
                registry.Resolve<IHeroSquad>(),
                registry.Resolve<ICastleUpgrades>(),
                registry.Resolve<ISimClock>());

            registry.Register<IUpgradeShop>(shop);
            registry.Register<IUpgradeOrders>(shop);
            session.Join(new UpgradeSession(shop));
        }
    }
}
