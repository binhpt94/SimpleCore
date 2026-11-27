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
    /// Dev builds also need Core's ICheatSelection, and, only while building the mid-battle cheat state,
    /// IWalletIncome, IWalletService, ICastleCheat, IWaveCheat and the run's IRunCheatStart (the state is shown in
    /// battle, so it asks for the scene to open straight in battle). The cheat list half of this class is in
    /// Cheat/HudInstaller.Cheat.cs.
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Hud/Installer", fileName = "HudInstaller")]
    public sealed partial class HudInstaller : InstallerAsset
    {
        [SerializeField] private HudConfig config;
        [SerializeField] private GameObject viewPrefab;

        [Tooltip("Dev builds only: build this cheat state once the battle is being played (node feature-hud/cheat). " +
                 "Ignored once a state or normal play was picked in this run of the game.")]
        [SerializeField] private HudCheatState _cheat = HudCheatState.None;

        internal HudCheatState Cheat
        {
            get => _cheat;
            set => _cheat = value;
        }

        public override void Install(IServiceRegistry registry)
        {
            if (registry == null) throw new ArgumentNullException(nameof(registry));
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            var cheats = NeedCheatSelection(registry);
#endif
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
            HudCheat cheat = null;
            var state = CheatToBuild(cheats);
            if (state != HudCheatState.None)
            {
                cheat = new HudCheat(run, wallet, registry.Resolve<IWalletIncome>(), registry.Resolve<IWalletService>(),
                    skills, registry.Resolve<ICastleCheat>(), registry.Resolve<IWaveCheat>());
                // Shown in battle: open straight in battle (run's default team), not on the start screen.
                registry.Resolve<IRunCheatStart>().RequestBattle(default);
                cheats.ReportRunning(HudCheats.KeyOf(state));
            }
            session.Join(new HudSession(new PrefabHudViewSpawner(viewPrefab), new UnityHudClock(),
                castle, skills, wallet, waves, run, config.Timings, cheat));
#else
            session.Join(new HudSession(new PrefabHudViewSpawner(viewPrefab), new UnityHudClock(),
                castle, skills, wallet, waves, run, config.Timings));
#endif
        }
    }
}
