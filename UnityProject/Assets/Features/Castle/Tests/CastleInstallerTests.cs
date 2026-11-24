using System;
using Game.Core;
using Game.Enemy;
using Game.Hero;
using Game.Run;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Game.Castle.Tests
{
    public class CastleInstallerTests
    {
        private const string InstallerPath = "Assets/Features/Castle/FeatureAssets/Public/CastleInstaller.asset";

        private static ServiceRegistry RegistryWithDependencies(out FakeSessionScene session, out FakeEnemyField enemies)
        {
            var registry = new ServiceRegistry();
            session = new FakeSessionScene();
            enemies = new FakeEnemyField();
            registry.Register<ISessionScene>(session);
            registry.Register<IRunFlow>(new FakeRunFlow());
            registry.Register<IEnemyField>(enemies);
            registry.Register<IHeroSquad>(new FakeHeroSquad());
            return registry;
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
            Assert.That(skills.Skills, Is.EqualTo(new[] { CastleSkill.Thunder }), "IDEA: Thunder only");
            Assert.That(registry.TryResolve<ICastleUpgrades>(out _), Is.True);
            Assert.That(enemies.Foes, Has.Count.EqualTo(1));
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
            registry.Register<ISessionScene>(new FakeSessionScene());
            registry.Register<IRunFlow>(new FakeRunFlow());
            registry.Register<IHeroSquad>(new FakeHeroSquad());

            var error = Assert.Throws<InvalidOperationException>(() => installer.Install(registry));
            StringAssert.Contains(nameof(IEnemyField), error.Message);
        }
    }
}
