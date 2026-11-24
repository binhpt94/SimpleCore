using NUnit.Framework;
using UnityEngine;

namespace Game.Castle.Tests
{
    public class CastleAutoShotTests
    {
        private CastleConfig _config;
        private FakeEnemyField _enemies;
        private CastleBody _body;
        private CastleUpgrades _upgrades;
        private CastleAutoShot _shot;

        [SetUp]
        public void SetUp()
        {
            _config = ScriptableObject.CreateInstance<CastleConfig>();
            var run = new FakeRunFlow();
            _enemies = new FakeEnemyField();
            _body = new CastleBody(_config, run);
            var skills = new CastleSkills(_config, _body, run, _enemies, new FakeHeroSquad());
            _upgrades = new CastleUpgrades(_body, skills);
            _shot = new CastleAutoShot(_config, _body, _enemies, _upgrades);
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(_config);

        [Test]
        public void First_enemy_in_range_is_shot_at_once_for_ten_damage()
        {
            _enemies.AddEnemy(1, 4f);

            _shot.Tick(0.016f);

            Assert.That(_enemies.Damaged, Is.EqualTo(new[] { (1, 10f) }));
        }

        [Test]
        public void Castle_shoots_once_per_second()
        {
            _enemies.AddEnemy(1, 4f);

            _shot.Tick(0.01f);
            _shot.Tick(0.5f);
            _shot.Tick(0.49f);
            Assert.That(_enemies.Damaged, Has.Count.EqualTo(1));
            _shot.Tick(0.02f);
            Assert.That(_enemies.Damaged, Has.Count.EqualTo(2));
        }

        [Test]
        public void Enemies_beyond_six_units_from_the_castle_edge_are_not_shot()
        {
            _enemies.AddEnemy(1, 8.5f); // gap 8.5 - 0.4 - 1.5 = 6.6

            _shot.Tick(1f);

            Assert.That(_enemies.Damaged, Is.Empty);
        }

        [Test]
        public void Castle_shoots_the_enemy_nearest_its_edge_on_either_side()
        {
            _enemies.AddEnemy(1, 6f);
            _enemies.AddEnemy(2, -3f);

            _shot.Tick(1f);

            Assert.That(_enemies.Damaged[0].handle, Is.EqualTo(2));
        }

        [Test]
        public void Equal_distance_goes_to_the_enemy_that_came_first()
        {
            _enemies.AddEnemy(5, 4f);
            _enemies.AddEnemy(3, -4f);

            _shot.Tick(1f);

            Assert.That(_enemies.Damaged[0].handle, Is.EqualTo(3));
        }

        [Test]
        public void Castle_does_not_shoot_after_it_fell()
        {
            _enemies.AddEnemy(1, 3f);
            _body.Hit(0, 5000f);

            _shot.Tick(1f);

            Assert.That(_enemies.Damaged, Is.Empty);
        }

        [Test]
        public void Attack_level_adds_a_fifth_of_the_base_damage_per_level()
        {
            _upgrades.ApplyLevel(CastleUpgrade.Attack, 5);
            _enemies.AddEnemy(1, 3f);

            _shot.Tick(1f);

            Assert.That(_enemies.Damaged[0].amount, Is.EqualTo(18f).Within(1e-4f));
        }

        [Test]
        public void Shot_flies_from_the_castle_edge_on_the_target_side()
        {
            float from = 0f, to = 0f;
            _shot.Fired += (handle, fromX, toX) => { from = fromX; to = toX; };
            _enemies.AddEnemy(1, -4f);

            _shot.Tick(1f);

            Assert.That(from, Is.EqualTo(-1.5f));
            Assert.That(to, Is.EqualTo(-4f));
        }
    }
}
