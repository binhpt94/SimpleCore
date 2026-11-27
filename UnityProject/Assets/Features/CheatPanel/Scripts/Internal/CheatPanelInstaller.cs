using System;
using Game.Core;
using Game.SceneFlow;
using UnityEngine;

namespace Game.CheatPanel
{
    /// <summary>
    /// Wires the cheat button and panel into every scene. Reads <c>cheat_enabled</c> from CheatPanelConfig:
    /// off = joins nothing, resolves nothing, creates nothing, blocks no touch. On = joins the scene session; the
    /// overlay is created when the session runs. Missing config fails at boot, naming the config.
    /// Same code in the Editor and every kind of build: only the config decides. Install after SessionScene.
    /// </summary>
    [CreateAssetMenu(menuName = "Game/CheatPanel/Installer", fileName = "CheatPanelInstaller")]
    public sealed partial class CheatPanelInstaller : InstallerAsset
    {
        internal const string MissingConfigMessage =
            "CheatPanel: the installer has no CheatPanelConfig (FeatureAssets/Private/CheatPanelConfig).";

        [Tooltip("AccessParams (FeatureAssets/Private/CheatPanelConfig).")]
        [SerializeField] private CheatPanelConfig _config;

        [Tooltip("Cheat button, shade and panel; created only when cheat_enabled is on.")]
        [SerializeField] private GameObject _overlayPrefab;

        [Tooltip("Dev builds only: cheat state of node feature-cheat-panel/cheat to build when the scene runs. Ignored once a state or normal play was picked in this run of the game.")]
        [SerializeField] private CheatPanelCheatState _cheat = CheatPanelCheatState.None;

        internal CheatPanelConfig Config { get => _config; set => _config = value; }
        internal GameObject OverlayPrefab { get => _overlayPrefab; set => _overlayPrefab = value; }

        /// <summary>Test seam: replaces the prefab spawner.</summary>
        internal ICheatOverlaySpawner SpawnerOverride { get; set; }

        internal CheatPanelCheatState Cheat { get => _cheat; set => _cheat = value; }

        public override void Install(IServiceRegistry registry)
        {
            if (registry == null) throw new ArgumentNullException(nameof(registry));
            if (_config == null) throw new InvalidOperationException(MissingConfigMessage);
            if (!_config.CheatEnabled) return;

            if (!registry.TryResolve<ISessionScene>(out var session))
                throw new InvalidOperationException("CheatPanel: ISessionScene is missing. Install SessionScene before CheatPanel.");
            var spawner = SpawnerOverride ?? new PrefabCheatOverlaySpawner(_overlayPrefab);
            ICheatStates states = new NoCheatStates();
            var openAtStart = false;
            ReadCheats(registry, ref states, ref openAtStart);
            // SceneFlow installs last in every scene; its ISceneSwitch is looked up once the session runs.
            ISceneSwitch FindSwitch() => registry.TryResolve<ISceneSwitch>(out var found) ? found : null;
            session.Join(new CheatOverlayParticipant(spawner, _config, states, FindSwitch, openAtStart));
        }

        /// <summary>Dev builds fill in the build's cheat states and the panel-open state; release builds keep none.</summary>
        partial void ReadCheats(IServiceRegistry registry, ref ICheatStates states, ref bool openAtStart);
    }
}
