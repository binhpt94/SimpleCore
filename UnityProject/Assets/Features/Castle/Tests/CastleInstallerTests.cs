using System;
using Game.Combat;
using Game.Core;
using Game.Enemy;
using Game.Hero;
using Game.Run;
using Game.SimClock;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Game.Castle.Tests
{
    public class CastleInstallerTests
    {
        private const string InstallerPath = "Assets/Features/Castle/FeatureAssets/Public/CastleInstaller.asset";

        private static ServiceRegistry RegistryWithDependencies(out FakeSessionScene session, out FakeEnemyField enemies)
            => RegistryWithDependencies(out session, out enemies, out _);

        private static ServiceRegistry RegistryWithDependencies(
            out FakeSessionScene session, out FakeEnemyField enemies, out FakeHeroSpawnPoints spawnPoints)
        {
            var registry = new ServiceRegistry();
            session = new FakeSessionScene();
            enemies = new FakeEnemyField();
            spawnPoints = new FakeHeroSpawnPoints();
            var run = new FakeRunFlow();
            registry.Register<ISessionScene>(session);
            registry.Register<IRunFlow>(run);
            registry.Register<IRunRevive>(run);
            registry.Register<IEnemyField>(enemies);
            registry.Register<IHeroSquad>(new FakeHeroSquad());
            registry.Register<IHeroSpawnPoints>(spawnPoints);
            registry.Register<ISimClock>(new FakeSimClock());
            registry.Register<ICombat>(new FakeCombat());
            registry.Register<IEnemyCombat>(new FakeEnemyCombat());
            return registry;
        }

        [Test]
        public void Installer_adds_the_castle_to_combat_and_gives_the_enemies_its_receiver_number()
        {
            var installer = AssetDatabase.LoadAssetAtPath<InstallerAsset>(InstallerPath);
            var registry = RegistryWithDependencies(out _, out var enemies);
            registry.TryResolve<ICombat>(out var combatService);
            registry.TryResolve<IEnemyCombat>(out var enemyCombatService);
            var combat = (FakeCombat)combatService;
            var enemyCombat = (FakeEnemyCombat)enemyCombatService;

            installer.Install(registry);

            Assert.That(combat.Receivers, Has.Count.EqualTo(1), "the castle is one receiver");
            Assert.That(enemyCombat.Foes, Is.SameAs(combat.Receivers[0]), "the same castle is the enemies' foe");
            Assert.That(enemyCombat.FoesReceiver, Is.EqualTo(10), "with the number combat gave it");
            Assert.That(enemies.Foes, Is.Empty, "no more direct hits from the enemies");
        }

        [Test]
        public void Installer_fails_when_combat_was_not_installed_first()
        {
            var installer = AssetDatabase.LoadAssetAtPath<InstallerAsset>(InstallerPath);
            var registry = new ServiceRegistry();
            var run = new FakeRunFlow();
            registry.Register<ISessionScene>(new FakeSessionScene());
            registry.Register<IRunFlow>(run);
            registry.Register<IRunRevive>(run);
            registry.Register<IEnemyField>(new FakeEnemyField());
            registry.Register<IHeroSquad>(new FakeHeroSquad());
            registry.Register<IHeroSpawnPoints>(new FakeHeroSpawnPoints());
            registry.Register<ISimClock>(new FakeSimClock());

            var error = Assert.Throws<InvalidOperationException>(() => installer.Install(registry));
            StringAssert.Contains(nameof(ICombat), error.Message);
        }

        [Test]
        public void Installer_fails_when_the_shared_clock_was_not_installed_first()
        {
            var installer = AssetDatabase.LoadAssetAtPath<InstallerAsset>(InstallerPath);
            var registry = new ServiceRegistry();
            var run = new FakeRunFlow();
            registry.Register<ISessionScene>(new FakeSessionScene());
            registry.Register<IRunFlow>(run);
            registry.Register<IRunRevive>(run);
            registry.Register<IEnemyField>(new FakeEnemyField());
            registry.Register<IHeroSquad>(new FakeHeroSquad());
            registry.Register<IHeroSpawnPoints>(new FakeHeroSpawnPoints());

            var error = Assert.Throws<InvalidOperationException>(() => installer.Install(registry));
            StringAssert.Contains(nameof(ISimClock), error.Message);
        }

        [Test]
        public void Installer_gives_the_heroes_their_start_points_at_the_castle_edges()
        {
            var installer = AssetDatabase.LoadAssetAtPath<InstallerAsset>(InstallerPath);
            var registry = RegistryWithDependencies(out _, out _, out var spawnPoints);

            installer.Install(registry);

            Assert.That(spawnPoints.SpawnX, Is.EqualTo(new[] { -1.5f, 1.5f, -1.5f }), "slot 1 and 3 left edge, slot 2 right edge");
        }

        [Test]
        public void Installer_fails_when_the_hero_spawn_points_were_not_installed_first()
        {
            var installer = AssetDatabase.LoadAssetAtPath<InstallerAsset>(InstallerPath);
            var registry = new ServiceRegistry();
            var run = new FakeRunFlow();
            registry.Register<ISessionScene>(new FakeSessionScene());
            registry.Register<IRunFlow>(run);
            registry.Register<IRunRevive>(run);
            registry.Register<IEnemyField>(new FakeEnemyField());
            registry.Register<IHeroSquad>(new FakeHeroSquad());

            var error = Assert.Throws<InvalidOperationException>(() => installer.Install(registry));
            StringAssert.Contains(nameof(IHeroSpawnPoints), error.Message);
        }

        [Test]
        public void Installer_asset_publishes_the_castle_and_joins_the_session()
        {
            var installer = AssetDatabase.LoadAssetAtPath<InstallerAsset>(InstallerPath);
            Assert.That(installer, Is.Not.Null, $"Missing {InstallerPath}");
            var registry = RegistryWithDependencies(out var session, out var enemies);

            installer.Install(registry);

            Assert.That(registry.TryResolve<ICastle>(out var castle), Is.True);
            Assert.That(castle.Health, Is.EqualTo(1000f));
            Assert.That(registry.TryResolve<ICastleSkills>(out var skills), Is.True);
            Assert.That(skills.Skills, Is.EqualTo(new[] { CastleSkill.Thunder, CastleSkill.Frost, CastleSkill.Rally }), "PROTOTYPE: all three");
            Assert.That(registry.TryResolve<ICastleUpgrades>(out _), Is.True);
            Assert.That(registry.TryResolve<ICastleCheat>(out _), Is.True, "dev builds publish the cheat");
            Assert.That(registry.TryResolve<ICastleFallCheat>(out _), Is.True, "dev builds publish the fall cheat");
            Assert.That(session.Joined, Has.Count.EqualTo(1));
        }

        [Test]
        public void Installer_without_config_fails_naming_the_config()
        {
            var installer = ScriptableObject.CreateInstance<CastleInstaller>();
            try
            {
                var registry = RegistryWithDependencies(out _, out _);
                var error = Assert.Throws<InvalidOperationException>(() => installer.Install(registry));
                StringAssert.Contains("CastleConfig", error.Message);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(installer);
            }
        }

        [Test]
        public void Installer_fails_when_the_enemy_field_was_not_installed_first()
        {
            var installer = AssetDatabase.LoadAssetAtPath<InstallerAsset>(InstallerPath);
            var registry = new ServiceRegistry();
            var run = new FakeRunFlow();
            registry.Register<ISessionScene>(new FakeSessionScene());
            registry.Register<IRunFlow>(run);
            registry.Register<IRunRevive>(run);
            registry.Register<IHeroSquad>(new FakeHeroSquad());
            registry.Register<IHeroSpawnPoints>(new FakeHeroSpawnPoints());

            var error = Assert.Throws<InvalidOperationException>(() => installer.Install(registry));
            StringAssert.Contains(nameof(IEnemyField), error.Message);
        }
    }
}
