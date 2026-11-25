using System;
using Game.Core;
using Game.Hero;
using Game.Run;
using UnityEngine;

namespace Game.TeamSelect
{
    /// <summary>
    /// Wires the team select screen into the Game scene: needs IRunFlow (Run), IHeroRoster (Hero) and
    /// ISessionScene, installed earlier. Publishes no contract. Asset: FeatureAssets/Public/TeamSelectInstaller.asset.
    /// </summary>
    [CreateAssetMenu(menuName = "Game/TeamSelect/Installer", fileName = "TeamSelectInstaller")]
    public sealed class TeamSelectInstaller : InstallerAsset
    {
        [SerializeField] private TeamSelectConfig config;
        [SerializeField] private GameObject viewPrefab;

        public override void Install(IServiceRegistry registry)
        {
            if (registry == null) throw new ArgumentNullException(nameof(registry));
            if (config == null) throw new InvalidOperationException("TeamSelectInstaller: missing TeamSelectConfig.");
            if (viewPrefab == null) throw new InvalidOperationException("TeamSelectInstaller: missing view prefab.");

            var session = new TeamSelectSession(
                registry.Resolve<IRunFlow>(),
                registry.Resolve<IHeroRoster>(),
                config.ToRules(),
                new PrefabViewFactory(viewPrefab));
            registry.Resolve<ISessionScene>().Join(session);
        }
    }
}
