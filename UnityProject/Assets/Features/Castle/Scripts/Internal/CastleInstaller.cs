using System;
using Game.Core;
using Game.Enemy;
using Game.Hero;
using Game.Run;
using UnityEngine;

namespace Game.Castle
{
    /// <summary>
    /// Wires the castle: publishes ICastle, ICastleSkills, ICastleUpgrades, adds the castle body to the enemies'
    /// foes and joins the scene session. Install after SessionScene, Run, Enemy and Hero.
    /// </summary>
    public sealed class CastleInstaller : InstallerAsset
    {
        [SerializeField] private CastleConfig config;

        public override void Install(IServiceRegistry registry)
        {
            if (registry == null) throw new ArgumentNullException(nameof(registry));
            if (config == null)
                throw new InvalidOperationException($"{nameof(CastleInstaller)} '{name}' has no CastleConfig assigned.");
            if (config.ViewPrefab == null)
                throw new InvalidOperationException($"CastleConfig '{config.name}' has no view prefab assigned.");

            var session = registry.Resolve<ISessionScene>();
            var runFlow = registry.Resolve<IRunFlow>();
            var enemyField = registry.Resolve<IEnemyField>();
            var heroSquad = registry.Resolve<IHeroSquad>();

            var body = new CastleBody(config, runFlow);
            var skills = new CastleSkills(config, body, runFlow, enemyField, heroSquad);
            var upgrades = new CastleUpgrades(body, skills);
            var autoShot = new CastleAutoShot(config, body, enemyField, upgrades);

            registry.Register<ICastle>(body);
            registry.Register<ICastleSkills>(skills);
            registry.Register<ICastleUpgrades>(upgrades);

            enemyField.AddFoes(body);
            session.Join(new CastleSession(
                config, runFlow, enemyField, body, skills, upgrades, autoShot,
                new CastleViewSpawner(config.ViewPrefab)));
        }
    }
}
