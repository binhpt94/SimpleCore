using System;
using Game.Combat;
using Game.Core;
using Game.Enemy;
using Game.Hero;
using Game.Run;
using Game.SimClock;
using UnityEngine;

namespace Game.Castle
{
    /// <summary>
    /// Wires the castle: publishes ICastle, ICastleSkills, ICastleUpgrades, adds the castle body to the enemies'
    /// foes, gives the heroes their start points at the castle edges and joins the scene session.
    /// Install after SessionScene, SimClock, Combat, Run, Enemy and Hero. The castle's shots and Thunder, and enemy
    /// hits on the castle, go through the shared combat.
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
            var runRevive = registry.Resolve<IRunRevive>();
            var enemyField = registry.Resolve<IEnemyField>();
            var heroSquad = registry.Resolve<IHeroSquad>();
            var heroSpawnPoints = registry.Resolve<IHeroSpawnPoints>();
            var clock = registry.Resolve<ISimClock>();
            var combat = registry.Resolve<ICombat>();
            var enemyCombat = registry.Resolve<IEnemyCombat>();

            var body = new CastleBody(config, runFlow);
            var hits = new CombatEnemyHits(combat, enemyCombat);
            var skills = new CastleSkills(config, body, runFlow, enemyField, heroSquad, hits);
            var upgrades = new CastleUpgrades(body, skills);
            var autoShot = new CastleAutoShot(config, body, enemyField, upgrades, hits);
            var castleSession = new CastleSession(
                config, runFlow, runRevive, enemyField, body, skills, upgrades, autoShot,
                new CastleViewSpawner(config.ViewPrefab), clock);

            registry.Register<ICastle>(body);
            registry.Register<ICastleSkills>(skills);
            registry.Register<ICastleUpgrades>(upgrades);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            registry.Register<ICastleCheat>(new CastleCheat(body, skills));
            registry.Register<ICastleFallCheat>(new CastleFallCheat(body, () => castleSession.InBattle));
#endif

            // Enemies hit the castle through combat; combat hands the castle one total per tick.
            enemyCombat.AddCombatFoes(body, combat.AddReceiver(body));
            heroSpawnPoints.SetSpawnX(config.HeroSlotX);
            session.Join(castleSession);
        }
    }
}
