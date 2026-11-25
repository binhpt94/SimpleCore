using System.Collections.Generic;
using Game.Run;
using NUnit.Framework;
using UnityEngine;

namespace Game.Hero.Tests
{
    /// <summary>
    /// Edge cases of the fixed hero design (live: hero-roster-values, respawn-values, hero-level-values,
    /// targeting-values), with the real HeroConfig numbers. Hero box 1u wide; fake enemies 0.8u wide unless said.
    /// </summary>
    public sealed class HeroEdgeCaseTests
    {
        private HeroFakes.EnemyField _field;
        private HeroSquad _squad;
        private HeroConfig _changedConfig;

        [SetUp]
        public void SetUp()
        {
            _field = new HeroFakes.EnemyField();
            _squad = new HeroSquad(HeroFakes.LoadConfig(), _field);
        }

        [TearDown]
        public void TearDown()
        {
            if (_changedConfig != null) Object.DestroyImmediate(_changedConfig);
            _changedConfig = null;
        }

        private void Start(params string[] heroCodes) => _squad.Begin(new RunTeam(heroCodes));

        private HeroUnit Hero(int slot) => _squad.Units[slot];

        private void Run(float seconds, float step = 0.05f)
        {
            for (var t = 0f; t < seconds - 1e-4f; t += step) _squad.Tick(step);
        }

        private List<Game.Enemy.EnemyFoe> Foes()
        {
            var foes = new List<Game.Enemy.EnemyFoe>();
            _squad.GetFoes(foes);
            return foes;
        }

        // ------------------------------------------------------------------ distance, hit, numbers

        [Test]
        public void A_hero_overlapping_an_enemy_hits_it_without_stepping_back()
        {
            Start("knight");                       // x = -1.5
            var enemy = _field.Add(-1.5f);

            Run(0.05f);

            Assert.That(_field.Taken(enemy), Is.EqualTo(12f));
            Assert.That(Hero(0).X, Is.EqualTo(-1.5f));
        }

        [Test]
        public void An_archer_hit_takes_health_at_once_at_full_range()
        {
            Start("archer");                       // x = -1.5, front edge -1.0, range 7
            var enemy = _field.Add(6.4f);          // left edge 6.0: gap exactly 7u

            _squad.Tick(0.05f);

            Assert.That(_field.Taken(enemy), Is.EqualTo(14f), "no flight time before the damage");
            Assert.That(Hero(0).X, Is.EqualTo(-1.5f));
        }

        [Test]
        public void A_hero_with_less_than_one_health_left_still_stands_and_numbers_stay_real()
        {
            Start("knight");

            _squad.Hit(0, 299.6f);
            _squad.ApplyLevel(0, 2);

            Assert.That(Hero(0).IsStanding, Is.True);
            Assert.That(Hero(0).Health, Is.EqualTo(0.4f + 45f).Within(1e-3f));
            Assert.That(Hero(0).Stats.Damage, Is.EqualTo(13.8f).Within(1e-4f), "12 × 1.15, not rounded");
        }

        [Test]
        public void Damage_beyond_the_health_left_leaves_the_hero_at_zero()
        {
            Start("knight");

            _squad.Hit(0, 1000f);

            Assert.That(Hero(0).Health, Is.EqualTo(0f));
            Assert.That(Hero(0).IsStanding, Is.False);
        }

        [Test]
        public void Levelling_while_waiting_for_the_next_hit_keeps_the_wait_then_uses_the_new_rhythm()
        {
            Start("knight");
            var enemy = _field.Add(0f);
            _squad.Tick(0.05f);                    // first hit, wait 1.0s
            _squad.Tick(0.5f);                     // 0.5s left

            _squad.ApplyLevel(0, 21);              // 48 damage, 0.625s per hit

            Assert.That(Hero(0).Cooldown, Is.EqualTo(0.5f).Within(1e-4f));
            _squad.Tick(0.5f);
            Assert.That(_field.Taken(enemy), Is.EqualTo(12f + 48f).Within(1e-3f));
            Assert.That(Hero(0).Cooldown, Is.EqualTo(0.625f).Within(1e-4f));
        }

        // ------------------------------------------------------------------ fall and respawn

        [Test]
        public void A_hero_that_falls_before_its_turn_does_not_hit_that_frame()
        {
            Start("knight");
            var enemy = _field.Add(0f);

            _squad.Hit(0, 300f);
            _squad.Tick(0.05f);

            Assert.That(_field.Taken(enemy), Is.EqualTo(0f));
        }

        [Test]
        public void When_all_three_heroes_fall_they_all_come_back_after_8s()
        {
            Start("knight", "axeman", "archer");
            _squad.Hit(0, 1000f);
            _squad.Hit(1, 1000f);
            _squad.Hit(2, 1000f);
            Assert.That(Foes(), Is.Empty);

            Run(8.05f);
            Assert.That(_squad.Units, Has.All.Matches<HeroUnit>(u => u.IsStanding));

            Run(0.35f);
            Assert.That(Foes(), Has.Count.EqualTo(3));
        }

        [Test]
        public void A_growing_hero_does_not_hit_until_the_grow_ends()
        {
            Start("knight");
            var enemy = _field.Add(0f);
            _squad.Hit(0, 300f);

            Run(8.05f);
            Assert.That(Hero(0).IsStanding, Is.True);
            Assert.That(_field.Taken(enemy), Is.EqualTo(0f));

            Run(0.2f);
            Assert.That(_field.Taken(enemy), Is.EqualTo(0f), "still growing");

            Run(0.3f);
            Assert.That(_field.Taken(enemy), Is.EqualTo(12f));
        }

        [Test]
        public void Levelling_a_fallen_hero_raises_its_level_at_once_and_keeps_its_wait()
        {
            Start("knight");
            _squad.Hit(0, 300f);
            Run(3f);

            _squad.ApplyLevel(0, 2);

            Assert.That(Hero(0).Level, Is.EqualTo(2));
            Assert.That(Hero(0).IsStanding, Is.False);
            Assert.That(Hero(0).RespawnLeft, Is.EqualTo(5f).Within(0.01f));
        }

        [Test]
        public void Cheering_does_not_heal_a_fallen_hero_or_shorten_its_wait()
        {
            Start("knight");
            _squad.Hit(0, 300f);
            Run(3f);

            _squad.HealAll(1f);

            Assert.That(Hero(0).Health, Is.EqualTo(0f));
            Assert.That(Hero(0).RespawnLeft, Is.EqualTo(5f).Within(0.01f));
        }

        [Test]
        public void A_hero_respawning_with_no_enemy_around_waits_at_its_spawn_point()
        {
            Start("knight");
            var enemy = _field.Add(10f);
            Run(2f);                               // walked out to x = 2.5
            _squad.Hit(0, 300f);
            _field.Remove(enemy);

            Run(8.5f);

            Assert.That(Hero(0).IsStanding, Is.True);
            Assert.That(Hero(0).X, Is.EqualTo(-1.5f));
            Assert.That(Hero(0).Mode, Is.EqualTo(HeroMode.Waiting));
            Assert.That(Hero(0).IsMoving, Is.False);
        }

        [Test]
        public void A_new_run_brings_the_team_back_at_level_1_and_full_health()
        {
            Start("knight");
            _squad.ApplyLevel(0, 5);
            _squad.Hit(0, 1000f);
            _squad.Clear();

            Start("knight");

            Assert.That(Hero(0).Level, Is.EqualTo(1));
            Assert.That(Hero(0).IsStanding, Is.True);
            Assert.That(Hero(0).Health, Is.EqualTo(300f));
            Assert.That(Hero(0).X, Is.EqualTo(-1.5f));
        }

        // ------------------------------------------------------------------ targeting

        [Test]
        public void A_walking_hero_turns_to_a_nearer_enemy_as_soon_as_it_appears()
        {
            Start("knight");
            _field.Add(10f);
            Run(1f);                               // x = 0.5, walking right
            Assert.That(Hero(0).Facing, Is.EqualTo(1));

            _field.Add(-4f);                       // gap 3.6u, the first is 8.6u away
            _squad.Tick(0.05f);

            Assert.That(Hero(0).Facing, Is.EqualTo(-1));
            Assert.That(Hero(0).X, Is.LessThan(0.5f));
        }

        [Test]
        public void A_hero_walking_home_turns_out_at_once_for_a_new_enemy()
        {
            Start("knight");
            var enemy = _field.Add(15f);
            Run(2f);                               // x = 2.5
            _field.Remove(enemy);
            Run(0.5f);                             // walking home, x = 1.5
            Assert.That(Hero(0).Facing, Is.EqualTo(-1));

            _field.Add(12f);
            _squad.Tick(0.05f);

            Assert.That(Hero(0).Facing, Is.EqualTo(1));
            Assert.That(Hero(0).Mode, Is.EqualTo(HeroMode.Approaching));
            Assert.That(Hero(0).X, Is.GreaterThan(1.5f));
        }

        [Test]
        public void When_its_target_is_defeated_by_someone_else_a_hero_picks_another_without_resetting_its_wait()
        {
            Start("knight");                       // x = -1.5
            var first = _field.Add(0f);            // gap 0.6u
            var second = _field.Add(0.3f);         // gap 0.9u, also in range
            _squad.Tick(0.05f);
            _squad.Tick(0.3f);                     // 0.7s left
            Assert.That(_field.Taken(first), Is.EqualTo(12f));

            _field.Remove(first);                  // defeated by the castle or a skill
            _squad.Tick(0.05f);

            Assert.That(Hero(0).TargetHandle, Is.EqualTo(second));
            Assert.That(_field.Taken(second), Is.EqualTo(0f));
            Assert.That(Hero(0).Cooldown, Is.EqualTo(0.65f).Within(1e-3f));

            Run(0.7f);
            Assert.That(_field.Taken(second), Is.EqualTo(12f));
        }

        [Test]
        public void A_hero_lets_go_of_a_target_that_moved_out_of_range_and_takes_the_nearest()
        {
            Start("knight");                       // x = -1.5
            var first = _field.Add(0f);            // gap 0.6u
            _squad.Tick(0.05f);
            Assert.That(_field.Taken(first), Is.EqualTo(12f));
            var behind = _field.Add(-4f);          // gap 1.6u: out of range, ignored while hitting

            _field.Move(first, 3f);                // gap 3.6u: out of range now
            _squad.Tick(0.05f);

            Assert.That(Hero(0).TargetHandle, Is.EqualTo(behind));
            Assert.That(Hero(0).Facing, Is.EqualTo(-1));
            Assert.That(Hero(0).Mode, Is.EqualTo(HeroMode.Approaching));
        }

        [Test]
        public void The_attack_wait_keeps_running_while_the_hero_walks_to_a_new_enemy()
        {
            Start("knight");                       // x = -1.5
            var first = _field.Add(0f, health: 12f);
            _squad.Tick(0.05f);                    // first hit defeats it; 1.0s to wait
            Assert.That(_field.Taken(first), Is.EqualTo(12f));
            var next = _field.Add(1.6f);           // gap 2.2u: 1.0u past range, 0.5s of walking

            Run(0.7f);
            Assert.That(Hero(0).Mode, Is.EqualTo(HeroMode.Attacking), "arrived");
            Assert.That(_field.Taken(next), Is.EqualTo(0f), "the wait did not restart or end on arrival");

            Run(0.35f);
            Assert.That(_field.Taken(next), Is.EqualTo(12f), "hit 1.0s after the previous one");
        }

        [Test]
        public void The_mage_blast_hits_an_enemy_between_it_and_the_target_only_when_close_to_the_target()
        {
            Start("mage");                         // x = -1.5, range 6
            var target = _field.Add(4f);
            _squad.Tick(0.05f);                    // first blast; the mage keeps this target
            Assert.That(_field.Taken(target), Is.EqualTo(18f));
            var near = _field.Add(2.2f);           // edge 2.6: 1.4u from the target's centre
            var far = _field.Add(1.8f);            // edge 2.2: 1.8u from the target's centre

            Run(1.7f);

            Assert.That(_field.Taken(target), Is.EqualTo(36f));
            Assert.That(_field.Taken(near), Is.EqualTo(18f));
            Assert.That(_field.Taken(far), Is.EqualTo(0f));
        }

        [Test]
        public void Of_two_equally_near_enemies_the_one_closer_to_the_castle_is_chosen()
        {
            Start("knight");                       // x = -1.5
            _field.Add(-4f);                       // gap 1.6u, 4u from the castle
            _field.Add(1f);                        // gap 1.6u, 1u from the castle

            _squad.Tick(0.05f);

            Assert.That(Hero(0).Facing, Is.EqualTo(1));
            Assert.That(Hero(0).X, Is.GreaterThan(-1.5f));
        }

        [Test]
        public void Of_two_enemies_equal_in_every_way_the_first_out_is_chosen()
        {
            Start("knight");
            var first = _field.Add(0f);
            var second = _field.Add(0f);

            _squad.Tick(0.05f);

            Assert.That(_field.Taken(first), Is.EqualTo(12f));
            Assert.That(_field.Taken(second), Is.EqualTo(0f));
        }

        [Test]
        public void A_hero_at_the_walk_limit_waits_there_and_switches_to_a_nearer_enemy()
        {
            _changedConfig = Object.Instantiate(HeroFakes.LoadConfig());
            _changedConfig.Find("knight").range = 0f;      // so a target at 16u stays out of reach from 15u
            _squad = new HeroSquad(_changedConfig, _field);
            Start("knight");
            var far = _field.Add(16f);             // inner edge 15.6u; from x = 15 the gap is 0.1u

            Run(9f);                               // 16.5u at 2.0u/s

            Assert.That(Hero(0).X, Is.EqualTo(15f).Within(1e-4f));
            Assert.That(Hero(0).Mode, Is.EqualTo(HeroMode.Approaching));
            Assert.That(_field.Taken(far), Is.EqualTo(0f));

            var near = _field.Add(14.5f);          // overlaps the hero: gap 0
            _squad.Tick(0.05f);

            Assert.That(_field.Taken(near), Is.EqualTo(12f));
            Assert.That(Hero(0).X, Is.LessThanOrEqualTo(15f + 1e-4f));
        }

        // ------------------------------------------------------------------ run speed

        [TestCase("knight", 2.0f)]
        [TestCase("axeman", 2.4f)]
        [TestCase("mage", 2.6f)]
        [TestCase("archer", 3.0f)]
        [TestCase("swordsman", 3.5f)]
        public void Each_hero_walks_at_its_own_run_speed(string code, float speed)
        {
            Start(code);                           // x = -1.5
            _field.Add(15f);                       // far beyond every range

            Run(1f);

            Assert.That(Hero(0).X, Is.EqualTo(-1.5f + speed).Within(1e-3f));
        }

        [Test]
        public void Levelling_up_does_not_change_a_hero_run_speed()
        {
            Start("knight");
            _squad.ApplyLevel(0, 10);
            _field.Add(15f);

            Run(1f);

            Assert.That(Hero(0).X, Is.EqualTo(0.5f).Within(1e-3f), "still 2.0u/s at level 10");
        }

        // ------------------------------------------------------------------ area hits

        [Test]
        public void The_axeman_cleave_counts_overlapping_enemies_on_the_target_side_only()
        {
            Start("axeman");                       // x = -1.5
            var target = _field.Add(-1.2f);        // overlaps, nearest the castle: the target, right side
            var sameSide = _field.Add(-1.4f);      // overlaps, centre right of the axeman
            var behind = _field.Add(-1.7f);        // overlaps, centre left of the axeman

            _squad.Tick(0.05f);

            Assert.That(_field.Taken(target), Is.EqualTo(14f));
            Assert.That(_field.Taken(sameSide), Is.EqualTo(14f));
            Assert.That(_field.Taken(behind), Is.EqualTo(0f));
        }

        [Test]
        public void An_area_hit_that_defeats_several_enemies_takes_each_one_once()
        {
            Start("axeman");                       // stretch -1.5 → 0.5
            var a = _field.Add(0f, health: 10f);
            var b = _field.Add(0.3f, health: 10f);
            var c = _field.Add(0.6f, health: 10f);

            _squad.Tick(0.05f);

            Assert.That(_field.Taken(a), Is.EqualTo(14f));
            Assert.That(_field.Taken(b), Is.EqualTo(14f));
            Assert.That(_field.Taken(c), Is.EqualTo(14f));
            Assert.That(_field.AliveCount, Is.EqualTo(0));
        }

        [Test]
        public void An_area_hit_never_hurts_another_hero()
        {
            Start("mage", "knight");               // mage x = -1.5, knight x = +1.5
            _field.Add(1f);                        // the knight stands inside the 1.5u blast around it

            _squad.Tick(0.05f);

            Assert.That(Hero(0).Health, Is.EqualTo(100f));
            Assert.That(Hero(1).Health, Is.EqualTo(300f));
        }

        [Test]
        public void The_mage_picks_another_target_when_its_target_fell_earlier_that_frame()
        {
            Start("knight", "mage");               // knight x = -1.5 acts first, mage x = +1.5
            var weak = _field.Add(0.6f, health: 10f);   // in both heroes' range
            var other = _field.Add(6f);                 // gap 3.6u from the mage

            _squad.Tick(0.05f);

            Assert.That(_field.Taken(weak), Is.EqualTo(12f), "defeated by the knight, not blasted again");
            Assert.That(_field.Taken(other), Is.EqualTo(18f));
        }
    }
}
