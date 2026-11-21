using System;
using Game.Core;
using Game.Services.Popup;
using Game.Services.SessionScene;
using Game.Wallet;
using UnityEngine;

namespace Game.Shop
{
    /// <summary>Needs SessionScene, Wallet and Popup installed first.</summary>
    [CreateAssetMenu(menuName = "Game/Installers/Shop")]
    public sealed class ShopInstaller : InstallerAsset
    {
        [SerializeField] private ShopConfig config;

        public override void Install(IServiceRegistry registry)
        {
            if (config == null)
                throw new InvalidOperationException($"{name}: ShopConfig is not assigned (field 'config').");
            if (config.BuyViewPrefab == null)
                throw new InvalidOperationException($"{config.name}: buyViewPrefab is not assigned.");
            if (!PrefabShopBuyViewSource.HasView(config.BuyViewPrefab))
                throw new InvalidOperationException(
                    $"{config.name}: buyViewPrefab '{config.BuyViewPrefab.name}' has no {nameof(IShopBuyView)} component on its root.");

            var session = registry.Resolve<ISessionScene>();
            var logic = new ShopLogic(
                registry.Resolve<IWalletService>(),
                registry.Resolve<IPopupService>(),
                config.Items);

            session.Join(new ShopBuyPresenter(logic, new PrefabShopBuyViewSource(config.BuyViewPrefab)));
        }
    }
}
