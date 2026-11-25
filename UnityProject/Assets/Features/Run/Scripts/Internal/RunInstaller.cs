using System;
using System.Linq;
using Game.Core;
using Game.Services.Popup;
using UnityEngine;

namespace Game.Run
{
    /// <summary>Wires the run: publishes IRunFlow, IRunRevive (same object) and IRunScore and joins the scene session. Depends on ISessionScene and IPopupStack (installed before the run).</summary>
    [CreateAssetMenu(menuName = "Game/Run/Run Installer", fileName = "RunInstaller")]
    public sealed class RunInstaller : InstallerAsset
    {
        [SerializeField] private RunConfig config;

        public override void Install(IServiceRegistry registry)
        {
            if (registry == null) throw new ArgumentNullException(nameof(registry));
            if (config == null)
                throw new InvalidOperationException($"{nameof(RunInstaller)} '{name}' has no {nameof(RunConfig)}. Assign FeatureAssets/Private/RunConfig.asset.");
            if (config.AppSignalsPrefab == null)
                throw new InvalidOperationException($"{nameof(RunConfig)} '{config.name}' has no app signals prefab. Assign FeatureAssets/Private/RunAppSignals.prefab.");
            var session = registry.Resolve<ISessionScene>();
            var popups = registry.Resolve<IPopupStack>();

            var score = new RunScore(new PlayerPrefsBestScoreStore());
            var delay = new UniTaskRunDelay();
            var flow = new RunFlow(score, delay, config.LoseDelaySeconds, config.TeamSize,
                config.SkipMenus, config.DefaultTeam, config.ReviveOnce, config.PauseEnabled);

            registry.Register<IRunFlow>(flow);
            registry.Register<IRunRevive>(flow);
            registry.Register<IRunScore>(score);
            // The device signals are a Unity object: made when the session runs (Play), not here.
            var signalsPrefab = config.AppSignalsPrefab;
            session.Join(new RunSession(flow, score, delay, () => UnityAppSignals.Create(signalsPrefab),
                config.PauseOnLeavingApp, config.BackKeyPauses));
            session.Join(new LeftoverPopupCheck(flow, () => popups.OpenIds.Select(id => id.ToString()).ToList(),
                delay, new UnityRunLog()));
        }
    }
}
