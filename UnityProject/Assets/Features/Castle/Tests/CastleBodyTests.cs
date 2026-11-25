using System.Collections.Generic;
using Game.Enemy;
using NUnit.Framework;
using UnityEngine;

namespace Game.Castle.Tests
{
    public class CastleBodyTests
    {
        private CastleConfig _config;
        private FakeRunFlow _run;
        private CastleBody _body;

        [SetUp]
        public void SetUp()
        {
            _config = ScriptableObject.CreateInstance<CastleConfig>();
            _run = new FakeRunFlow();
            _body = new CastleBody(_config, _run);
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(_config);

        [Test]
        public void Castle_starts_at_full_health()
        {
            Assert.That(_body.Health, Is.EqualTo(1000f));
            Assert.That(_body.MaxHealth, Is.EqualTo(1000f));
            Assert.That(_body.IsDestroyed, Is.False);
        }

        [Test]
        public void Enemies_see_the_castle_centred_at_zero_with_its_half_width()
        {
            var foes = new List<EnemyFoe>();
            _body.GetFoes(foes);

            Assert.That(foes, Has.Count.EqualTo(1));
            Assert.That(foes[0].X, Is.EqualTo(0f));
            Assert.That(foes[0].HalfWidth, Is.EqualTo(1.5f));
        }

        [Test]
        public void Enemy_hit_takes_castle_health()
        {
            _body.Hit(0, 5f);

            Assert.That(_body.Health, Is.EqualTo(995f));
        }

        [Test]
        public void Castle_falling_tells_the_run_it_is_lost_once_before_destroyed()
        {
            var order = new List<string>();
            _run.OnLose = () => order.Add("lose");
            _body.Destroyed += () => order.Add("destroyed");

            _body.Hit(0, 600f);
            _body.Hit(0, 600f);
            _body.Hit(0, 600f);

            Assert.That(order, Is.EqualTo(new[] { "lose", "destroyed" }));
            Assert.That(_run.LoseCalls, Is.EqualTo(1));
            Assert.That(_body.Health, Is.EqualTo(0f));
            Assert.That(_body.IsDestroyed, Is.True);
        }

        [Test]
        public void Destroyed_castle_is_no_longer_a_foe()
        {
            _body.Hit(0, 2000f);
            var foes = new List<EnemyFoe>();
            _body.GetFoes(foes);

            Assert.That(foes, Is.Empty);
        }

        [Test]
        public void Health_upgrade_adds_to_max_and_current_without_healing_what_was_lost()
        {
            _body.Hit(0, 600f);

            _body.ApplyHealthLevel(2);

            Assert.That(_body.MaxHealth, Is.EqualTo(1150f));
            Assert.That(_body.Health, Is.EqualTo(550f));
        }

        [Test]
        public void Fraction_of_health_left_keeps_the_castle_standing()
        {
            _body.Hit(0, 999.6f);

            Assert.That(_body.Health, Is.EqualTo(0.4f).Within(1e-3f));
            Assert.That(_body.IsDestroyed, Is.False);
            Assert.That(_run.LoseCalls, Is.EqualTo(0));
        }

        [Test]
        public void Health_bought_in_the_frame_the_castle_fell_is_not_applied()
        {
            _body.Hit(0, 1000f);

            _body.ApplyHealthLevel(2);

            Assert.That(_body.IsDestroyed, Is.True);
            Assert.That(_body.Health, Is.EqualTo(0f));
            Assert.That(_body.MaxHealth, Is.EqualTo(1000f));
        }

        [Test]
        public void Health_level_bought_at_one_health_leaves_the_castle_at_151()
        {
            _body.Hit(0, 999f);

            _body.ApplyHealthLevel(2);

            Assert.That(_body.Health, Is.EqualTo(151f).Within(1e-3f));
            Assert.That(_body.MaxHealth, Is.EqualTo(1150f));
        }

        [Test]
        public void Health_level_five_gives_1600_max_health()
        {
            _body.ApplyHealthLevel(5);

            Assert.That(_body.MaxHealth, Is.EqualTo(1600f));
            Assert.That(_body.Health, Is.EqualTo(1600f));
        }

        [Test]
        public void Revive_stands_the_castle_up_with_thirty_percent_of_max_health()
        {
            _body.Hit(0, 5000f);

            var revived = _body.Revive();

            Assert.That(revived, Is.True);
            Assert.That(_body.IsDestroyed, Is.False);
            Assert.That(_body.Health, Is.EqualTo(300f));
            Assert.That(_body.MaxHealth, Is.EqualTo(1000f));
        }

        [Test]
        public void Revive_takes_thirty_percent_of_the_upgraded_max_health_and_keeps_it()
        {
            _body.ApplyHealthLevel(3); // 1300 max
            _body.Hit(0, 5000f);

            _body.Revive();

            Assert.That(_body.MaxHealth, Is.EqualTo(1300f));
            Assert.That(_body.Health, Is.EqualTo(390f));
        }

        [Test]
        public void Revive_rounds_up_without_float_drift_adding_a_point()
        {
            // Every max health is 1000 + 150k, so 30% is whole (300 + 45k); 1150 * 0.3 in floats is 345.00003,
            // which must stay 345 and not round up to 346.
            _body.ApplyHealthLevel(2);
            _body.Hit(0, 5000f);

            _body.Revive();

            Assert.That(_body.Health, Is.EqualTo(345f));
        }

        [Test]
        public void Revive_does_nothing_to_a_standing_castle()
        {
            _body.Hit(0, 100f);

            var revived = _body.Revive();

            Assert.That(revived, Is.False);
            Assert.That(_body.Health, Is.EqualTo(900f));
        }

        [Test]
        public void Revived_castle_takes_damage_at_once_with_no_grace_time()
        {
            _body.Hit(0, 5000f);
            _body.Revive();

            _body.Hit(0, 5f);

            Assert.That(_body.Health, Is.EqualTo(295f));
            var foes = new List<EnemyFoe>();
            _body.GetFoes(foes);
            Assert.That(foes, Has.Count.EqualTo(1), "enemies at the wall see the castle again");
        }

        [Test]
        public void Revived_castle_falling_again_tells_the_run_once_more()
        {
            var destroyed = 0;
            _body.Destroyed += () => destroyed++;
            _body.Hit(0, 5000f);
            _body.Revive();

            _body.Hit(0, 200f);
            _body.Hit(0, 200f);
            _body.Hit(0, 200f);

            Assert.That(_run.LoseCalls, Is.EqualTo(2));
            Assert.That(destroyed, Is.EqualTo(2));
            Assert.That(_body.IsDestroyed, Is.True);
            Assert.That(_body.Health, Is.EqualTo(0f));
        }

        [Test]
        public void Heroes_start_and_revive_at_the_castle_edges_two_left_one_right()
        {
            Assert.That(_body.HeroStartX(0), Is.EqualTo(-1.5f), "slot 1");
            Assert.That(_body.HeroStartX(1), Is.EqualTo(1.5f), "slot 2");
            Assert.That(_body.HeroStartX(2), Is.EqualTo(-1.5f), "slot 3");
        }

        [Test]
        public void New_run_puts_the_castle_back_to_full_health_standing()
        {
            _body.ApplyHealthLevel(3);
            _body.Hit(0, 5000f);

            _body.Reset();

            Assert.That(_body.IsDestroyed, Is.False);
            Assert.That(_body.MaxHealth, Is.EqualTo(1000f));
            Assert.That(_body.Health, Is.EqualTo(1000f));
        }
    }
}
