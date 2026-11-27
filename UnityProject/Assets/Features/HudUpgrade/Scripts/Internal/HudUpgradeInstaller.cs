using System;
using Game.Core;
using Game.Hero;
using Game.Run;
using Game.Upgrade;
using Game.Wallet;
using UnityEngine;

namespace Game.HudUpgrade
{
    /// <summary>
    /// Wires the upgrade bar: joins the scene session with a participant that opens the bar.
    /// Install after SessionScene, Run, Wallet, Hero and Upgrade.
    /// </summary>
    [CreateAssetMenu(menuName = "Game/HudUpgrade/Installer", fileName = "HudUpgradeInstaller")]
    public sealed partial class HudUpgradeInstaller : InstallerAsset
    {
        [SerializeField] private HudUpgradeConfig config;
        [SerializeField] private GameObject barPrefab;

        public override void Install(IServiceRegistry registry)
        {
            if (registry == null) throw new ArgumentNullException(nameof(registry));
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            var cheats = NeedCheatSelection(registry);
#endif
            if (config == null) throw new InvalidOperationException("HudUpgradeInstaller: missing config (HudUpgradeConfig).");
            if (barPrefab == null) throw new InvalidOperationException("HudUpgradeInstaller: missing barPrefab (UpgradeBar).");
            if (barPrefab.GetComponent<IUpgradeBarView>() == null)
                throw new InvalidOperationException($"HudUpgradeInstaller: barPrefab '{barPrefab.name}' has no IUpgradeBarView component.");

            var session = registry.Resolve<ISessionScene>();
            var shop = registry.Resolve<IUpgradeShop>();
            var orders = registry.Resolve<IUpgradeOrders>();
            var roster = registry.Resolve<IHeroRoster>();
            var run = registry.Resolve<IRunFlow>();
            var down = registry.Resolve<IHeroDownState>();

            var cheat = default(HudUpgradeParticipant.CheatSetup);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            var cheatState = CheatToBuild(cheats);
            if (cheatState != HudUpgradeCheatState.None)
            {
                cheat = new HudUpgradeParticipant.CheatSetup(cheatState, config.MixedButtonsCoins,
                    config.MixedButtonsLevels, registry.Resolve<IWalletBalance>(), registry.Resolve<IWalletIncome>(),
                    registry.Resolve<IWalletService>(), config.MixedButtonsTeam);
                // Shown in battle: open straight in battle with the state's team, not on the start screen.
                registry.Resolve<IRunCheatStart>().RequestBattle(new RunTeam(config.MixedButtonsTeam ?? Array.Empty<string>()));
                cheats.ReportRunning(HudUpgradeCheats.KeyOf(cheatState));
            }
#endif

            session.Join(new HudUpgradeParticipant(shop, orders, roster, run, down, new PrefabBarViewFactory(barPrefab), cheat));
        }
    }
}
