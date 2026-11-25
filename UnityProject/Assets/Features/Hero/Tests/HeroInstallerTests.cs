using System;
using System.Collections.Generic;
using Game.Core;
using Game.Enemy;
using Game.Run;
using NUnit.Framework;
using UnityEngine;

namespace Game.Hero.Tests
{
    public sealed class HeroInstallerTests
    {
        private const string InstallerPath = "Assets/Features/Hero/FeatureAssets/Public/HeroInstaller.asset";

        private ServiceRegistry _registry;
        private HeroFakes.Session _session;
        private HeroFakes.RunFlow _run;
        private HeroFakes.EnemyField _field;
        private HeroFakes.EnemyCombat _enemyCombat;
        private HeroFakes.Combat _combat;

        [SetUp]
        public void SetUp()
        {
            _registry = new ServiceRegistry();
            _session = new HeroFakes.Session();
            _run = new HeroFakes.RunFlow();
            _field = new HeroFakes.EnemyField();
            _enemyCombat = new HeroFakes.EnemyCombat();
            _combat = new HeroFakes.Combat();
        }

        private void RegisterAllDependencies()
        {
            _registry.Register<ISessionScene>(_session);
            _registry.Register<IRunFlow>(_run);
            _registry.Register<IEnemyField>(_field);
            _registry.Register<IEnemyCombat>(_enemyCombat);
            _registry.Register<Game.Combat.ICombat>(_combat);
        }

        private static HeroInstaller LoadInstaller()
        {
            var installer = UnityEditor.AssetDatabase.LoadAssetAtPath<HeroInstaller>(InstallerPath);
            Assert.That(installer, Is.Not.Null, $"Missing {InstallerPath}");
            return installer;
        }

        [Test]
        public void Installing_the_real_asset_publishes_the_roster_and_the_squad()
        {
            RegisterAllDependencies();

            LoadInstaller().Install(_registry);

            Assert.That(_registry.TryResolve<IHeroRoster>(out _), Is.True);
            Assert.That(_registry.TryResolve<IHeroSquad>(out _), Is.True);
        }

        [Test]
        public void Installing_lets_the_castle_set_where_heroes_start()
        {
            RegisterAllDependencies();

            LoadInstaller().Install(_registry);

            Assert.That(_registry.TryResolve<IHeroSpawnPoints>(out var points), Is.True);
            Assert.DoesNotThrow(() => points.SetSpawnX(new[] { -1.5f, 1.5f, -1.5f }));
        }

        [Test]
        public void Installing_lets_enemies_attack_the_heroes_and_joins_the_session()
        {
            RegisterAllDependencies();

            LoadInstaller().Install(_registry);

            // From PROTOTYPE enemies hit the heroes through the shared combat, not straight onto the squad.
            Assert.That(_field.Foes, Is.Empty);
            Assert.That(_combat.Receivers, Has.Count.EqualTo(1));
            Assert.That(_enemyCombat.CombatFoes, Has.Count.EqualTo(1));
            Assert.That(_enemyCombat.CombatFoes[0].Foes, Is.SameAs(_combat.Receivers[0]));
            Assert.That(_enemyCombat.CombatFoes[0].Receiver, Is.EqualTo(1), "enemy hits carry the number combat gave the heroes");
            Assert.That(_session.Participants, Has.Count.EqualTo(1));
        }

        [Test]
        public void Installing_publishes_which_heroes_are_down_from_the_squad()
        {
            RegisterAllDependencies();

            LoadInstaller().Install(_registry);

            Assert.That(_registry.TryResolve<IHeroDownState>(out var down), Is.True);
            Assert.That(down, Is.SameAs(_registry.Resolve<IHeroSquad>()), "one object answers both");
            Assert.That(down.IsDown(0), Is.False, "no run going: nobody is down");
        }

        [Test]
        public void Installing_publishes_the_respawn_wait_left_from_the_squad()
        {
            RegisterAllDependencies();

            LoadInstaller().Install(_registry);

            Assert.That(_registry.TryResolve<IHeroRespawnTime>(out var time), Is.True);
            Assert.That(time, Is.SameAs(_registry.Resolve<IHeroSquad>()), "one object answers both");
            Assert.That(time.RespawnSecondsLeft(0), Is.EqualTo(0f), "no run going");
        }

        [TestCase(typeof(Game.Combat.ICombat))]
        [TestCase(typeof(IEnemyCombat))]
        public void Missing_shared_combat_fails_install_naming_it(Type missing)
        {
            _registry.Register<ISessionScene>(_session);
            _registry.Register<IRunFlow>(_run);
            _registry.Register<IEnemyField>(_field);
            if (missing != typeof(IEnemyCombat)) _registry.Register<IEnemyCombat>(_enemyCombat);
            if (missing != typeof(Game.Combat.ICombat)) _registry.Register<Game.Combat.ICombat>(_combat);

            var error = Assert.Throws<InvalidOperationException>(() => LoadInstaller().Install(_registry));

            Assert.That(error.Message, Does.Contain(missing.Name));
        }

        [Test]
        public void The_roster_offers_the_five_heroes_in_order()
        {
            RegisterAllDependencies();
            LoadInstaller().Install(_registry);
            var roster = _registry.Resolve<IHeroRoster>();

            var codes = new List<string>();
            foreach (var hero in roster.Heroes) codes.Add(hero.Code);

            Assert.That(codes, Is.EqualTo(new[] { "knight", "axeman", "swordsman", "archer", "mage" }));
            Assert.That(roster.TryGet("axeman", out var axeman), Is.True);
            Assert.That(axeman.Group, Is.EqualTo(HeroGroup.Melee));
            Assert.That(axeman.Attack, Is.EqualTo(HeroAttack.FrontCleave));
            Assert.That(roster.TryGet("mage", out var mage), Is.True);
            Assert.That(mage.Group, Is.EqualTo(HeroGroup.Ranged));
            Assert.That(mage.Attack, Is.EqualTo(HeroAttack.Blast));
            Assert.That(roster.TryGet("nobody", out _), Is.False);
        }

        [Test]
        public void Missing_enemy_field_fails_install_naming_it()
        {
            _registry.Register<ISessionScene>(_session);
            _registry.Register<IRunFlow>(_run);

            var error = Assert.Throws<InvalidOperationException>(() => LoadInstaller().Install(_registry));

            Assert.That(error.Message, Does.Contain(nameof(IEnemyField)));
        }


        [Test]
        public void Installer_without_config_fails_install_naming_the_config()
        {
            RegisterAllDependencies();
            var installer = ScriptableObject.CreateInstance<HeroInstaller>();
            try
            {
                var error = Assert.Throws<InvalidOperationException>(() => installer.Install(_registry));
                Assert.That(error.Message, Does.Contain("HeroConfig"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(installer);
            }
        }
    }
}
