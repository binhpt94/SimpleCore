using Game.Core;
using Game.Services.Popup;
using Game.Wallet;
using UnityEngine;

namespace Game.Shop
{
    /// <summary>Needs Wallet and Popup installed first.</summary>
    [CreateAssetMenu(menuName = "Game/Installers/Shop")]
    public sealed class ShopInstaller : InstallerAsset
    {
        [SerializeField] private ShopConfig config;

        public override void Install(IServiceRegistry registry)
        {
            ShopRuntime.Logic = new ShopLogic(
                registry.Resolve<IWalletService>(),
                registry.Resolve<IPopupService>(),
                config != null ? config.Items : new ShopItem[0],
                config != null ? config.PurchasedPopupPrefab : null);
        }
    }
}
