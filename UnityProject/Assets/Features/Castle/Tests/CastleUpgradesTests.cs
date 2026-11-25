using NUnit.Framework;
using UnityEngine;

namespace Game.Castle.Tests
{
    public class CastleUpgradesTests
    {
        private CastleConfig _config;
        private FakeEnemyField _enemies;
        private FakeHeroSquad _heroes;
        private CastleBody _body;
        private CastleSkills _skills;
        private CastleUpgrades _upgrades;
        private CastleAutoShot _shot;

        [SetUp]
        public void SetUp()
        {
            _config = ScriptableObject.CreateInstance<CastleConfig>();
            var run = new FakeRunFlow();
            _enemies = new FakeEnemyField();
            _heroes = new FakeHeroSquad();
            _body = new CastleBody(_config, run);
            _skills = new CastleSkills(_config, _body, run, _enemies, _heroes);
            _upgrades = new CastleUpgrades(_body, _skills);
            _shot = new CastleAutoShot(_config, _body, _enemies, _upgrades);
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(_config);

        [Test]
        public void Levels_have_no_cap()
        {
            _upgrades.ApplyLevel(CastleUpgrade.Health, 40);
            _upgrades.ApplyLevel(CastleUpgrade.Attack, 40);
            _upgrades.ApplyLevel(CastleUpgrade.Skills, 40);
            _enemies.AddEnemy(1, 3f);

            _shot.Tick(1f);
            _skills.TryUse(CastleSkill.Thunder);

            Assert.That(_body.MaxHealth, Is.EqualTo(1000f + 150f * 39f));
            Assert.That(_enemies.Damaged[0].amount, Is.EqualTo(10f * (1f + 0.2f * 39f)).Within(1e-3f));
            Assert.That(_enemies.Damaged[1].amount, Is.EqualTo(80f * (1f + 0.2f * 39f)).Within(1e-3f));
        }

        [Test]
        public void Every_item_starts_a_run_at_level_one()
        {
            Assert.That(_upgrades.AttackLevel, Is.EqualTo(1));
            Assert.That(_body.MaxHealth, Is.EqualTo(1000f));
            _skills.TryUse(CastleSkill.Rally);
            Assert.That(_heroes.Heals[0], Is.EqualTo(0.3f).Within(1e-5f));
        }

        [Test]
        public void Items_level_independently()
        {
            _upgrades.ApplyLevel(CastleUpgrade.Attack, 3);

            Assert.That(_body.MaxHealth, Is.EqualTo(1000f));
            _skills.TryUse(CastleSkill.Frost);
            Assert.That(_enemies.Slows[0], Is.EqualTo(5f).Within(1e-5f));
        }

        [Test]
        public void A_lower_or_same_level_never_takes_strength_away()
        {
            _upgrades.ApplyLevel(CastleUpgrade.Health, 3);
            _upgrades.ApplyLevel(CastleUpgrade.Attack, 3);
            _upgrades.ApplyLevel(CastleUpgrade.Skills, 3);

            _upgrades.ApplyLevel(CastleUpgrade.Health, 2);
            _upgrades.ApplyLevel(CastleUpgrade.Health, 3);
            _upgrades.ApplyLevel(CastleUpgrade.Attack, 1);
            _upgrades.ApplyLevel(CastleUpgrade.Skills, 2);
            _enemies.AddEnemy(1, 3f);
            _skills.TryUse(CastleSkill.Thunder);

            Assert.That(_body.MaxHealth, Is.EqualTo(1300f));
            Assert.That(_body.Health, Is.EqualTo(1300f));
            Assert.That(_upgrades.AttackLevel, Is.EqualTo(3));
            Assert.That(_enemies.Damaged[0].amount, Is.EqualTo(112f).Within(1e-3f));
        }

        [Test]
        public void Fallen_castle_takes_no_level()
        {
            _body.Hit(0, 5000f);

            _upgrades.ApplyLevel(CastleUpgrade.Health, 2);
            _upgrades.ApplyLevel(CastleUpgrade.Attack, 2);

            Assert.That(_body.MaxHealth, Is.EqualTo(1000f));
            Assert.That(_upgrades.AttackLevel, Is.EqualTo(1));
        }
    }
}
