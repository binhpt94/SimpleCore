using System;
using Game.Castle;
using Game.Core;
using Game.Run;
using Game.Wallet;
using Game.Wave;
using UnityEngine;

namespace Game.Hud
{
    /// <summary>
    /// Wires the battle HUD: reads Castle, Wallet, Wave and Run through their contracts and joins the scene session.
    /// Install after SessionScene, Run, Castle, Wallet and Wave. Registers nothing (Hud has no contracts).
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Hud/Installer", fileName = "HudInstaller")]
    public sealed class HudInstaller : InstallerAsset
    {
        [SerializeField] private HudConfig config;
        [SerializeField] private GameObject viewPrefab;

        public override void Install(IServiceRegistry registry)
        {
            if (registry == null) throw new ArgumentNullException(nameof(registry));
            if (config == null) throw new InvalidOperationException("HudInstaller: missing HudConfig (config).");
            if (viewPrefab == null) throw new InvalidOperationException("HudInstaller: missing HUD view prefab (viewPrefab).");
            if (viewPrefab.GetComponent<IHudView>() == null)
                throw new InvalidOperationException($"HudInstaller: prefab '{viewPrefab.name}' has no IHudView component on its root.");

            var session = registry.Resolve<ISessionScene>();
            var run = registry.Resolve<IRunFlow>();
            var castle = registry.Resolve<ICastle>();
            var skills = registry.Resolve<ICastleSkills>();
            var wallet = registry.Resolve<IWalletBalance>();
            var waves = registry.Resolve<IWaveProgress>();

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            var cheat = new HudCheat(run, wallet, registry.Resolve<IWalletIncome>(), registry.Resolve<IWalletService>());
            session.Join(new HudSession(new PrefabHudViewSpawner(viewPrefab), new UnityHudClock(),
                castle, skills, wallet, waves, run, config.Timings, cheat));
#else
            session.Join(new HudSession(new PrefabHudViewSpawner(viewPrefab), new UnityHudClock(),
                castle, skills, wallet, waves, run, config.Timings));
#endif
        }
    }
}
