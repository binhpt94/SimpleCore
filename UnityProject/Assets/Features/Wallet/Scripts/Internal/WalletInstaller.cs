using Game.Core;
using UnityEngine;

namespace Game.Wallet
{
    [CreateAssetMenu(menuName = "Game/Installers/Wallet")]
    public sealed class WalletInstaller : InstallerAsset
    {
        [SerializeField] private WalletConfig config;

        public override void Install(IServiceRegistry registry)
        {
            var logic = new WalletLogic(config != null ? config.StartCoins : 0);
            WalletRuntime.Logic = logic;
            registry.Register<IWalletService>(logic);
        }
    }
}
