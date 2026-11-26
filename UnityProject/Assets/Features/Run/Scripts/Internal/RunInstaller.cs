using System;
using System.Linq;
using Game.Core;
using Game.Services.Popup;
using Game.SimClock;
using UnityEngine;

namespace Game.Run
{
    /// <summary>Wires the run: publishes IRunFlow, IRunRevive, IRunBattle (same object) and IRunScore and joins the scene session. Depends on ISessionScene, IPopupStack, ISimClock and ISimRunControl (installed before the run).</summary>
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
            var simClock = registry.Resolve<ISimClock>();
            var simRun = registry.Resolve<ISimRunControl>();

            var score = new RunScore(new PlayerPrefsBestScoreStore());
            // Frame waits: the session begins on the frame after Run, the leftover popup check on the frame after the battle starts.
            var frames = new UniTaskRunDelay();
            // Gameplay waits (before the lose and revive popups, counting cleared waves) on the shared clock, owner "Run".
            var clockWaits = new SimClockRunDelay(simClock);
            var flow = new RunFlow(score, clockWaits, config.LoseDelaySeconds, config.TeamSize,
                config.SkipMenus, config.DefaultTeam, config.ReviveOnce, config.PauseEnabled,
                new SimRunClock(simRun, simClock));

            registry.Register<IRunFlow>(flow);
            registry.Register<IRunRevive>(flow);
            registry.Register<IRunBattle>(flow);
            registry.Register<IRunScore>(score);
            // The device signals are a Unity object: made when the session runs (Play), not here.
            var signalsPrefab = config.AppSignalsPrefab;
            session.Join(new RunSession(flow, score, frames, () => UnityAppSignals.Create(signalsPrefab),
                config.PauseOnLeavingApp, config.BackKeyPauses, clockWaits));
            session.Join(new LeftoverPopupCheck(flow, () => popups.OpenIds.Select(id => id.ToString()).ToList(),
                frames, new UnityRunLog()));
        }
    }
}
