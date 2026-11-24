using System;
using Game.Core;
using UnityEngine;

namespace Game.Run
{
    /// <summary>Wires the run: publishes IRunFlow and IRunScore and joins the scene session. Depends only on ISessionScene.</summary>
    [CreateAssetMenu(menuName = "Game/Run/Run Installer", fileName = "RunInstaller")]
    public sealed class RunInstaller : InstallerAsset
    {
        [SerializeField] private RunConfig config;

        public override void Install(IServiceRegistry registry)
        {
            if (registry == null) throw new ArgumentNullException(nameof(registry));
            if (config == null)
                throw new InvalidOperationException($"{nameof(RunInstaller)} '{name}' has no {nameof(RunConfig)}. Assign FeatureAssets/Private/RunConfig.asset.");
            var session = registry.Resolve<ISessionScene>();

            var score = new RunScore(new PlayerPrefsBestScoreStore());
            var delay = new UniTaskRunDelay();
            var flow = new RunFlow(score, delay, config.LoseDelaySeconds, config.TeamSize,
                config.SkipMenus, config.DefaultTeam);

            registry.Register<IRunFlow>(flow);
            registry.Register<IRunScore>(score);
            session.Join(new RunSession(flow, score, delay));
        }
    }
}
