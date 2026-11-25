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
            var skills = new CastleSkills(_config, _body, run, _enemies, new FakeHeroSquad(), _enemies);
            _upgrades = new CastleUpgrades(_body, skills);
            _shot = new CastleAutoShot(_config, _body, _enemies, _upgrades, _enemies);
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
        public void Range_counts_the_gap_from_the_castle_edge_not_from_its_centre()
        {
            // Centre 8.1u away (beyond 6u from the centre), but the gap between edges is 8.1 - 1.0 - 1.5 = 5.6u.
            _enemies.AddEnemy(1, 8.1f, 1.0f);
            // Gap 8.0 - 0.4 - 1.5 = 6.1u: just out of range.
            _enemies.AddEnemy(2, -8.0f);

            _shot.Tick(1f);
            _shot.Tick(1f);

            Assert.That(_enemies.Damaged, Is.EqualTo(new[] { (1, 10f), (1, 10f) }));
        }

        [Test]
        public void Nearest_is_judged_by_the_gap_so_a_wider_enemy_with_a_farther_centre_can_be_nearer()
        {
            _enemies.AddEnemy(1, 5f, 0.4f);    // gap 5 - 0.4 - 1.5 = 3.1
            _enemies.AddEnemy(2, -5.5f, 1.5f); // centre farther, gap 5.5 - 1.5 - 1.5 = 2.5

            _shot.Tick(1f);

            Assert.That(_enemies.Damaged[0].handle, Is.EqualTo(2));
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
        public void Target_is_picked_again_for_every_shot_so_a_nearer_newcomer_is_shot_first()
        {
            _enemies.AddEnemy(1, 6f);
            _shot.Tick(1f);
            _enemies.AddEnemy(2, -2.5f);

            _shot.Tick(1f);

            Assert.That(_enemies.Damaged[0].handle, Is.EqualTo(1));
            Assert.That(_enemies.Damaged[1].handle, Is.EqualTo(2));
        }

        [Test]
        public void With_nothing_in_range_the_timer_waits_ready_and_fires_as_soon_as_an_enemy_comes_in()
        {
            _shot.Tick(1f);
            _shot.Tick(2.5f);
            Assert.That(_enemies.Damaged, Is.Empty);

            _enemies.AddEnemy(1, 4f);
            _shot.Tick(0.016f);

            Assert.That(_enemies.Damaged, Has.Count.EqualTo(1));
        }

        [Test]
        public void Slowed_enemies_change_nothing_for_the_auto_shot()
        {
            _enemies.AddEnemy(1, 4f);
            _enemies.SlowAll(5f);

            _shot.Tick(0.01f);
            _shot.Tick(1f);

            Assert.That(_enemies.Damaged, Is.EqualTo(new[] { (1, 10f), (1, 10f) }));
        }

        [Test]
        public void Attack_bought_after_a_shot_left_only_strengthens_the_next_shot()
        {
            _enemies.AddEnemy(1, 4f);
            _shot.Tick(0.01f);

            _upgrades.ApplyLevel(CastleUpgrade.Attack, 2);
            _shot.Tick(1f);

            Assert.That(_enemies.Damaged[0].amount, Is.EqualTo(10f));
            Assert.That(_enemies.Damaged[1].amount, Is.EqualTo(12f).Within(1e-4f));
        }

        [Test]
        public void Castle_shot_never_hurts_the_castle()
        {
            _enemies.AddEnemy(1, 4f);

            _shot.Tick(1f);

            Assert.That(_body.Health, Is.EqualTo(1000f));
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

        private void Steps(int count)
        {
            for (var i = 0; i < count; i++) _shot.Tick(0.025f);
        }

        [Test]
        public void Shot_damage_lv1_takes_ten_health_so_a_forty_health_soldier_falls_after_four_shots()
        {
            // ShotParams shot_damage_lv1 = 10
            _enemies.AddEnemy(1, 4f);

            Steps(1 + 3 * 40);

            Assert.That(_enemies.Damaged, Is.EqualTo(new[] { (1, 10f), (1, 10f), (1, 10f), (1, 10f) }));
        }

        [Test]
        public void Shot_interval_of_one_second_is_forty_steps_of_the_shared_clock()
        {
            // ShotParams shot_interval = 1.0 s: the next shot leaves on the 40th step after the last one, not the 39th.
            _enemies.AddEnemy(1, 4f);
            Steps(1);

            Steps(39);
            Assert.That(_enemies.Damaged, Has.Count.EqualTo(1));
            Steps(1);
            Assert.That(_enemies.Damaged, Has.Count.EqualTo(2));
        }

        [Test]
        public void Shot_range_reaches_six_units_beyond_each_castle_edge_and_no_further()
        {
            // ShotParams shot_range = 6 u from the edge: an enemy whose near edge is at x = 7.5 is in, at x = 7.625 it is out.
            _enemies.AddEnemy(1, 7.75f, 0.25f);   // near edge 7.5, gap 6.0
            _enemies.AddEnemy(2, -7.875f, 0.25f); // near edge -7.625, gap 6.125

            _shot.Tick(1f);
            _shot.Tick(1f);

            Assert.That(_enemies.Damaged, Is.EqualTo(new[] { (1, 10f), (1, 10f) }));
        }

        [Test]
        public void Targets_per_shot_is_one_so_only_the_nearest_of_several_in_range_is_hit()
        {
            // ShotParams targets_per_shot = 1
            _enemies.AddEnemy(1, 3f);
            _enemies.AddEnemy(2, -3.5f);
            _enemies.AddEnemy(3, 5f);

            _shot.Tick(1f);

            Assert.That(_enemies.Damaged, Is.EqualTo(new[] { (1, 10f) }));
        }

        [Test]
        public void Target_scope_skips_an_enemy_whose_centre_is_beyond_sixteen_units()
        {
            // ShotParams target_scope: centre |x| <= 16. A very wide enemy still off screen is in range but not shot.
            _enemies.AddEnemy(1, 16.5f, 9f); // gap 16.5 - 9 - 1.5 = 6.0, centre off screen
            _enemies.AddEnemy(2, -16f, 8.5f); // gap 16 - 8.5 - 1.5 = 6.0, centre on the screen edge

            _shot.Tick(1f);
            _shot.Tick(1f);

            Assert.That(_enemies.Damaged, Is.EqualTo(new[] { (2, 10f), (2, 10f) }));
        }
    }
}
