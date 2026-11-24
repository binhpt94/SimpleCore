using System;
using Game.Core;
using Game.Run;
using UnityEngine;

namespace Game.Wallet
{
    [CreateAssetMenu(menuName = "Game/Installers/Wallet")]
    public sealed class WalletInstaller : InstallerAsset
    {
        [SerializeField] private WalletConfig config;

        public override void Install(IServiceRegistry registry)
        {
            if (config == null) throw Missing("config");

            var session = registry.Resolve<ISessionScene>();
            var run = registry.Resolve<IRunFlow>();

            var logic = new WalletLogic(config.StartCoins, new UnityWalletLog());
            registry.Register<IWalletService>(logic);
            registry.Register<IWalletBalance>(logic);
            registry.Register<IWalletIncome>(logic);

            session.Join(new WalletRunReset(logic, run));
        }

        private InvalidOperationException Missing(string field)
        {
            return new InvalidOperationException($"{name}: '{field}' is not assigned.");
        }
    }
}
