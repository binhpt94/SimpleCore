using System;
using Game.Core;
using Game.Services.SessionScene;
using UnityEngine;

namespace Game.Wallet
{
    [CreateAssetMenu(menuName = "Game/Installers/Wallet")]
    public sealed class WalletInstaller : InstallerAsset
    {
        [SerializeField] private WalletConfig config;
        [SerializeField] private GameObject coinsLabelPrefab;

        public override void Install(IServiceRegistry registry)
        {
            if (config == null) throw Missing("config");
            if (coinsLabelPrefab == null) throw Missing("coinsLabelPrefab");

            var session = registry.Resolve<ISessionScene>();

            var logic = new WalletLogic(config.StartCoins);
            registry.Register<IWalletService>(logic);
            session.Join(new WalletCoinsPresenter(logic, new PrefabCoinsDisplaySource(coinsLabelPrefab)));
        }

        private InvalidOperationException Missing(string field)
        {
            return new InvalidOperationException($"{name}: '{field}' is not assigned.");
        }
    }
}
