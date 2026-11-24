using System;
using Game.Core;
using Game.Enemy;
using Game.Run;
using UnityEngine;

namespace Game.Hero
{
    /// <summary>
    /// Wires the hero feature: publishes IHeroRoster and IHeroSquad, adds the heroes to the enemies' foes and
    /// joins the session. Installed after Enemy and before Castle. No game rules here.
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

            var roster = new HeroRoster(config);
            var squad = new HeroSquad(config, enemyField);
            registry.Register<IHeroRoster>(roster);
            registry.Register<IHeroSquad>(squad);
            enemyField.AddFoes(squad);

            var presenter = new HeroPresenter(squad, config, new PrefabHeroDisplayFactory(config.HeroPrefab));
            session.Join(new HeroParticipant(run, squad, presenter, new UnityFrameClock()));
        }
    }
}
