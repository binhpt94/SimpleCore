using System;
using Game.Combat;
using Game.Core;
using Game.Enemy;
using Game.Run;
using UnityEngine;

namespace Game.Hero
{
    /// <summary>
    /// Wires the hero feature: publishes IHeroRoster, IHeroSquad, IHeroSpawnPoints and IHeroDownState, joins the shared
    /// combat as an actor and a receiver (heroes aim and strike in combat's tick; enemies hit the heroes through combat)
    /// and the session. Installed after Combat and Enemy, before Castle. No game rules here.
    /// </summary>
    public sealed class HeroInstaller : InstallerAsset
    {
        [SerializeField] private HeroConfig config;

        public override void Install(IServiceRegistry registry)
        {
            if (registry == null) throw new ArgumentNullException(nameof(registry));
            if (config == null)
                throw new InvalidOperationException($"{nameof(HeroInstaller)} '{name}': {nameof(HeroConfig)} is not assigned.");
            var problem = config.FindProblem();
            if (problem != null)
                throw new InvalidOperationException($"{nameof(HeroInstaller)} '{name}': {nameof(HeroConfig)} '{config.name}' is not usable: {problem}.");
            if (config.HeroPrefab == null)
                throw new InvalidOperationException($"{nameof(HeroInstaller)} '{name}': {nameof(HeroConfig)} '{config.name}' has no hero prefab.");

            var session = registry.Resolve<ISessionScene>();
            var run = registry.Resolve<IRunFlow>();
            var enemyField = registry.Resolve<IEnemyField>();
            var enemyCombat = registry.Resolve<IEnemyCombat>();
            var combat = registry.Resolve<ICombat>();

            var roster = new HeroRoster(config);
            var squad = new HeroSquad(config, enemyField, new CombatHeroHits(combat, enemyCombat));
            registry.Register<IHeroRoster>(roster);
            registry.Register<IHeroSquad>(squad);
            registry.Register<IHeroSpawnPoints>(squad);
            registry.Register<IHeroDownState>(squad);
            registry.Register<IHeroRespawnTime>(squad);
            enemyCombat.AddCombatFoes(squad, combat.AddReceiver(squad));

            var presenter = new HeroPresenter(squad, config, new PrefabHeroDisplayFactory(config.HeroPrefab));
            session.Join(new HeroParticipant(run, squad, presenter, combat, new UnityFrameClock()));
        }
    }
}
