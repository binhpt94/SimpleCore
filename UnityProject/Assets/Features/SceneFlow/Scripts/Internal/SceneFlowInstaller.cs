using System;
using Game.Core;
using Game.Services.SessionScene;
using UnityEngine;

namespace Game.SceneFlow
{
    /// <summary>One asset per scene: the Loading scene's asset has side Loading, the Game scene's has side Game.</summary>
    [CreateAssetMenu(menuName = "Game/Installers/SceneFlow")]
    public sealed class SceneFlowInstaller : InstallerAsset
    {
        [SerializeField] private SceneFlowSide side;
        [SerializeField] private SceneFlowConfig config;
        [SerializeField] private GameObject loadingScreenPrefab;
        [SerializeField] private GameObject gateCarrierPrefab;

        public override void Install(IServiceRegistry registry)
        {
            var session = registry.Resolve<ISessionScene>();

            if (side == SceneFlowSide.Loading) InstallLoadingSide(session);
            else InstallGameSide(session, registry);
        }

        private void InstallLoadingSide(ISessionScene session)
        {
            if (config == null) throw Missing("config");
            if (loadingScreenPrefab == null) throw Missing("loadingScreenPrefab");

            var settings = config.Settings;
            if (string.IsNullOrEmpty(settings.LoadingScene) || string.IsNullOrEmpty(settings.GameScene))
            {
                throw new InvalidOperationException($"{name}: SceneFlowConfig must name both the loading and the game scene.");
            }

            session.Join(new LoadingToGameFlow(session, new UnitySceneLoader(),
                new PrefabLoadingScreenSource(loadingScreenPrefab), new UnityTimer(), settings, Debug.LogException));
        }

        private void InstallGameSide(ISessionScene session, IServiceRegistry registry)
        {
            if (config == null) throw Missing("config");
            if (gateCarrierPrefab == null) throw Missing("gateCarrierPrefab");
            if (!registry.TryResolve<ISessionLoadProgress>(out var progress))
            {
                throw new InvalidOperationException(
                    $"{name}: no ISessionLoadProgress registered; install SessionScene before SceneFlow (side {side}).");
            }

            var gameScene = config.Settings.GameScene;
            if (string.IsNullOrEmpty(gameScene))
            {
                throw new InvalidOperationException($"{name}: SceneFlowConfig must name the game scene.");
            }

            var gate = new SceneGate(progress);

            // The carrier must exist before the Loading scene looks for it, so it cannot wait for Run.
            // Outside Play mode (EditMode tests run installers) it would be left behind in the open scene.
            if (Application.isPlaying)
            {
                new PrefabSceneGateCarrierSource(gateCarrierPrefab).Create().Carry(gate);
            }

            session.Join(new GameReadySignal(session, gate, new UnitySceneLoader(), gameScene));
        }

        private InvalidOperationException Missing(string field)
        {
            return new InvalidOperationException($"{name}: '{field}' is not assigned (side {side}).");
        }
    }

    internal enum SceneFlowSide
    {
        Loading = 0,
        Game = 1
    }
}
