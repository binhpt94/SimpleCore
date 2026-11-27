using System;
using Game.Core;
using Game.Services.SessionScene;
using UnityEngine;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System.Collections.Generic;
#endif

namespace Game.SceneFlow
{
    /// <summary>One asset per scene: the Loading scene's asset has side Loading, the Game scene's has side Game.</summary>
    [CreateAssetMenu(menuName = "Game/Installers/SceneFlow")]
    public sealed class SceneFlowInstaller : InstallerAsset
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        , ICheatStateSource
#endif
    {
        [SerializeField] private SceneFlowSide side;
        [SerializeField] private SceneFlowConfig config;
        [SerializeField] private GameObject loadingScreenPrefab;
        [SerializeField] private GameObject gateCarrierPrefab;
        [SerializeField] private GameObject switchHostPrefab;
        [Tooltip("Dev builds only (editor, development build): show a cheat state instead of the real flow. Loading side. " +
                 "Ignored once a state or normal play was picked in this run of the game.")]
        [SerializeField] private SceneFlowCheatState cheat;

        public override void Install(IServiceRegistry registry)
        {
            var session = registry.Resolve<ISessionScene>();
            if (config == null) throw Missing("config");
            if (loadingScreenPrefab == null) throw Missing("loadingScreenPrefab");
            if (gateCarrierPrefab == null) throw Missing("gateCarrierPrefab");
            if (switchHostPrefab == null) throw Missing("switchHostPrefab");
            if (!registry.TryResolve<ISessionLoadProgress>(out var progress))
            {
                throw new InvalidOperationException(
                    $"{name}: no ISessionLoadProgress registered; install SessionScene before SceneFlow (side {side}).");
            }

            var settings = config.Settings;
            if (string.IsNullOrEmpty(settings.LoadingScene) || string.IsNullOrEmpty(settings.GameScene))
            {
                throw new InvalidOperationException($"{name}: SceneFlowConfig must name both the loading and the game scene.");
            }

            var thisScene = side == SceneFlowSide.Loading ? settings.LoadingScene : settings.GameScene;
            var switcher = SharedSwitcher(settings, thisScene);
            registry.Register<ISceneSwitch>(new SceneSwitchService(switcher, session, settings.LoadingScene));

            // Every scene has a gate: a switch into it (also a reload or a restart) ends once it opens.
            var gate = new SceneGate(progress);

            // The carrier must exist before the switch looks for it, so it cannot wait for Run.
            // Outside Play mode (EditMode tests run installers) it would be left behind in the open scene.
            if (Application.isPlaying)
            {
                new PrefabSceneGateCarrierSource(gateCarrierPrefab).Create().Carry(gate);
            }

            if (side == SceneFlowSide.Loading) InstallLoadingSide(registry, session, switcher, gate, settings);
            else InstallGameSide(registry, session, gate, settings);
        }

        private void InstallLoadingSide(IServiceRegistry registry, ISessionScene session, SceneSwitcher switcher,
            SceneGate gate, SceneFlowSettings settings)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (CheatToBuild(registry, out var selection) == SceneFlowCheatState.LoadError)
            {
                selection?.ReportRunning(SceneFlowCheats.LoadErrorKey);
                session.Join(new LoadErrorCheat(new PrefabLoadingScreenSource(loadingScreenPrefab, false), settings, gate));
                return;
            }
#endif

            session.Join(new LoadingToGameFlow(session, switcher, gate, settings.GameScene));
        }

        private static void InstallGameSide(IServiceRegistry registry, ISessionScene session, SceneGate gate,
            SceneFlowSettings settings)
        {
            var ready = new GameReadySignal(session, gate, new UnitySceneLoader(), settings.GameScene);
            // SceneFlow is installed last, so features installed before it must resolve this later (in Run), not in Install.
            registry.Register<IGameSceneReady>(ready);
            session.Join(ready);
        }

        /// <summary>
        /// The switcher of this run of the game, made by the first scene that boots and kept across scenes, so a
        /// switch started in one scene is still going in the next until that one is ready.
        /// </summary>
        private SceneSwitcher SharedSwitcher(SceneFlowSettings settings, string thisScene)
        {
            // EditMode tests run installers outside Play: no object is made, the switcher is this scene's own.
            if (!Application.isPlaying) return NewSwitcher(settings, thisScene);

            var hosts = new PrefabSceneSwitchHostSource(switchHostPrefab);
            var host = hosts.Find();
            if (host != null) return host.Switcher;

            var switcher = NewSwitcher(settings, thisScene);
            hosts.Create().Hold(switcher);
            return switcher;
        }

        private SceneSwitcher NewSwitcher(SceneFlowSettings settings, string thisScene)
        {
            // The cover must outlive the scene it was opened in, which is removed while the next one loads.
            var covers = new PrefabLoadingScreenSource(loadingScreenPrefab, true);
            return new SceneSwitcher(thisScene, new UnitySceneLoader(), covers, new UnityTimer(), settings,
                new UnitySceneFlowLog());
        }

        private InvalidOperationException Missing(string field)
        {
            return new InvalidOperationException($"{name}: '{field}' is not assigned (side {side}).");
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        /// <summary>The feature's title in the design (node feature-scene-flow).</summary>
        public string CheatFeatureTitle => SceneFlowCheats.FeatureTitle;

        /// <summary>The Loading scene builds this feature's states; the Game scene's asset builds none.</summary>
        public IReadOnlyList<CheatState> CheatStates =>
            side == SceneFlowSide.Loading ? SceneFlowCheats.States : Array.Empty<CheatState>();

        /// <summary>
        /// The state this scene builds: once something was picked in this run of the game, the pick alone (only when
        /// it is this feature's); before that, the state chosen on the asset.
        /// </summary>
        private SceneFlowCheatState CheatToBuild(IServiceRegistry registry, out ICheatSelection selection)
        {
            if (!registry.TryResolve(out selection) || !selection.HasPick) return cheat;
            return SceneFlowCheats.StateOf(selection.PickedKey);
        }
#endif
    }

    internal enum SceneFlowSide
    {
        Loading = 0,
        Game = 1
    }
}
