using System;
using System.Collections.Generic;
using Game.Combat;
using Game.Run;
using Game.SimClock;
using Game.Wallet;
using NUnit.Framework;
using UnityEditor;

namespace Game.Enemy.Tests
{
    /// <summary>Walking, stopping at foes, hitting on rhythm, Frost (live/feature-enemy/enemy-behavior/enemy-move-values).</summary>
    public sealed class EnemyCombatTests
    {
        private const string ConfigPath = "Assets/Features/Enemy/FeatureAssets/Private/EnemyConfig.asset";
        // One step of the shared clock (live/feature-sim-clock/clock-step/clock-values: 40 steps a second).
        private const float Step = 0.025f;

        private EnemyConfig _config;
        private EnemyField _field;
        private FakeFoes _heroes;
        private FakeFoes _castle;

        [SetUp]
        public void SetUp()
        {
            // The rules are the same for every type, also those not in the game at this stage: test them all.
            _config = AllTypesConfig();
            _field = new EnemyField(_config, new FakeIncome());
            _heroes = new FakeFoes();
            _castle = new FakeFoes();
            _castle.Foes.Add(new EnemyFoe(1, 0f, 1.5f));
            _field.AddFoes(_heroes);
            _field.AddFoes(_castle);
        }

        [TearDown]
        public void TearDown()
        {
            if (_config != null) UnityEngine.Object.DestroyImmediate(_config);
        }

        [Test]
        public void A_boss_walks_and_hits_by_the_same_rules_as_any_enemy()
        {
            _field.Spawn(new EnemySpawn(EnemyType.Boss, EnemySide.Right, 1f));

            Run(25f);

            Assert.That(X(), Is.EqualTo(4.5f).Within(0.05f), "stops at its range from the castle edge");
            Assert.That(_castle.Hits.Count, Is.GreaterThan(0));
            Assert.That(_castle.Hits.TrueForAll(h => Math.Abs(h.Damage - 40f) < 1e-4f), Is.True, "plain hits only");

            var hitsBefore = _castle.Hits.Count;
            Run(4f);
            Assert.That(_castle.Hits.Count - hitsBefore, Is.EqualTo(2), "one hit each 2.0s");
        }

        [Test]
        public void A_soldier_walks_toward_the_castle_at_its_speed()
        {
            _field.Spawn(new EnemySpawn(EnemyType.Soldier, EnemySide.Right, 1f));

            Run(2f);

            Assert.That(X(), Is.EqualTo(17f - 1.5f * 2f).Within(0.05f));
        }

        [Test]
        public void An_enemy_stops_at_the_castle_edge_by_its_range_and_hits_the_castle()
        {
            _field.Spawn(new EnemySpawn(EnemyType.Soldier, EnemySide.Right, 1f));

            Run(15f);

            Assert.That(X(), Is.EqualTo(3.0f).Within(0.05f), "soldier stops with its centre at 3.0u");
            Assert.That(_castle.Hits.Count, Is.GreaterThan(0));
            Assert.That(_castle.Hits[0].Damage, Is.EqualTo(5f));
        }

        [Test]
        public void An_enemy_from_the_left_stops_on_the_left_side()
        {
            _field.Spawn(new EnemySpawn(EnemyType.Giant, EnemySide.Left, 1f));

            Run(30f);

            Assert.That(X(), Is.EqualTo(-3.7f).Within(0.05f));
        }

        [Test]
        public void A_hero_in_range_blocks_the_enemy_before_the_castle()
        {
            _heroes.Foes.Add(new EnemyFoe(7, 10f, 0.5f));
            _field.Spawn(new EnemySpawn(EnemyType.Soldier, EnemySide.Right, 1f));

            Run(10f);

            Assert.That(X(), Is.EqualTo(10f + 0.5f + 1f + 0.5f).Within(0.05f));
            Assert.That(_heroes.Hits.Count, Is.GreaterThan(0));
            Assert.That(_heroes.Hits[0].Key, Is.EqualTo(7));
            Assert.That(_castle.Hits, Is.Empty);
        }

        [Test]
        public void An_enemy_hitting_the_castle_turns_to_a_hero_that_appears_in_range()
        {
            _field.Spawn(new EnemySpawn(EnemyType.Soldier, EnemySide.Right, 1f));
            Run(15f);
            Assert.That(_castle.Hits.Count, Is.GreaterThan(0));
            var castleHits = _castle.Hits.Count;

            _heroes.Foes.Add(new EnemyFoe(3, 2.0f, 0.5f));
            Run(3f);

            Assert.That(_heroes.Hits.Count, Is.GreaterThan(0));
            Assert.That(_castle.Hits.Count, Is.EqualTo(castleHits));
        }

        [Test]
        public void An_enemy_keeps_hitting_the_same_hero_while_it_stays_in_range()
        {
            _heroes.Foes.Add(new EnemyFoe(1, 15.5f, 0.5f));
            _field.Spawn(new EnemySpawn(EnemyType.Soldier, EnemySide.Right, 1f));
            _field.Tick(Step);

            _heroes.Foes.Add(new EnemyFoe(2, 16.2f, 0.5f));
            Run(2.5f);

            Assert.That(_heroes.Hits.TrueForAll(h => h.Key == 1), Is.True);
        }

        [Test]
        public void When_the_blocking_hero_leaves_the_enemy_walks_on()
        {
            _heroes.Foes.Add(new EnemyFoe(7, 10f, 0.5f));
            _field.Spawn(new EnemySpawn(EnemyType.Soldier, EnemySide.Right, 1f));
            Run(10f);

            _heroes.Foes.Clear();
            Run(10f);

            Assert.That(X(), Is.EqualTo(3.0f).Within(0.05f));
        }

        [Test]
        public void An_enemy_hits_once_per_its_seconds_per_hit_and_the_first_hit_is_at_once()
        {
            _heroes.Foes.Add(new EnemyFoe(7, 15.5f, 0.5f));
            _field.Spawn(new EnemySpawn(EnemyType.Soldier, EnemySide.Right, 1f));

            _field.Tick(Step);
            Assert.That(_heroes.Hits.Count, Is.EqualTo(1), "first hit as soon as the foe is in range");

            Run(2.5f);
            Assert.That(_heroes.Hits.Count, Is.EqualTo(3), "then one hit each 1.0s");
        }

        [Test]
        public void Damage_dealt_scales_with_the_spawn_stat_scale()
        {
            _heroes.Foes.Add(new EnemyFoe(7, 15.5f, 0.5f));
            _field.Spawn(new EnemySpawn(EnemyType.Soldier, EnemySide.Right, 1.9f));

            _field.Tick(Step);

            Assert.That(_heroes.Hits[0].Damage, Is.EqualTo(9.5f).Within(0.001f));
        }

        [Test]
        public void Frost_halves_walking_speed_while_it_lasts()
        {
            _field.Spawn(new EnemySpawn(EnemyType.Soldier, EnemySide.Right, 1f));

            _field.SlowAll(2f);
            Run(2f);
            Assert.That(X(), Is.EqualTo(17f - 1.5f).Within(0.05f), "2s at half speed");

            Run(1f);
            Assert.That(X(), Is.EqualTo(17f - 1.5f - 1.5f).Within(0.05f), "back to full speed");
        }

        [Test]
        public void Frost_doubles_the_wait_between_hits()
        {
            _heroes.Foes.Add(new EnemyFoe(7, 15.5f, 0.5f));
            _field.Spawn(new EnemySpawn(EnemyType.Soldier, EnemySide.Right, 1f));
            _field.Tick(Step);

            _field.SlowAll(10f);
            Run(3f);

            Assert.That(_heroes.Hits.Count, Is.EqualTo(1 + 1), "one hit each 2.0s while slowed (3 unslowed)");
        }

        [Test]
        public void An_enemy_spawned_after_Frost_is_not_slowed()
        {
            _field.SlowAll(5f);
            _field.Spawn(new EnemySpawn(EnemyType.Soldier, EnemySide.Right, 1f));

            Run(2f);

            Assert.That(X(), Is.EqualTo(14f).Within(0.05f));
        }

        [Test]
        public void After_the_battle_stopped_enemies_stand_still_and_stop_hitting()
        {
            _heroes.Foes.Add(new EnemyFoe(7, 15.5f, 0.5f));
            _field.Spawn(new EnemySpawn(EnemyType.Soldier, EnemySide.Right, 1f));
            _field.Tick(Step);

            _field.Stop();
            Run(5f);

            Assert.That(_heroes.Hits.Count, Is.EqualTo(1));
        }

        [Test]
        public void The_stage_shows_spawned_enemies_flashes_hurt_ones_and_shrinks_defeated_ones()
        {
            var stage = new FakeStage();
            using var presenter = Present(new FakeRunFlow(), new FakeClock(), stage);

            _field.Spawn(new EnemySpawn(EnemyType.Wolf, EnemySide.Left, 1f));
            var handle = Handle();
            _field.Damage(handle, 5f);
            _field.Damage(handle, 100f);

            Assert.That(stage.Shown, Is.EqualTo(new[] { handle }));
            Assert.That(stage.Flashed, Is.EqualTo(new[] { handle }));
            Assert.That(stage.Defeated, Is.EqualTo(new[] { handle }));
        }

        [Test]
        public void The_stage_lunges_the_enemy_toward_its_foe_when_it_hits()
        {
            var stage = new FakeStage();
            var clock = new FakeClock();
            using var presenter = Present(new FakeRunFlow(), clock, stage);
            _heroes.Foes.Add(new EnemyFoe(7, 15.5f, 0.5f));
            _field.Spawn(new EnemySpawn(EnemyType.Soldier, EnemySide.Right, 1f));

            clock.StepOnce();

            Assert.That(stage.Lunges, Is.EqualTo(new[] { -1 }));
        }

        [Test]
        public void An_enemy_archer_shows_a_shot_flying_to_its_foe_on_every_hit_while_the_damage_lands_at_once()
        {
            var stage = new FakeStage();
            var clock = new FakeClock();
            using var presenter = Present(new FakeRunFlow(), clock, stage);
            _heroes.Foes.Add(new EnemyFoe(7, 12f, 0.5f));        // gap 17 - 0.5 - 0.5 - 12 = 4u, within 5u
            _field.Spawn(new EnemySpawn(EnemyType.Archer, EnemySide.Right, 1f));

            clock.StepOnce();

            Assert.That(_heroes.Hits.Count, Is.EqualTo(1), "damage lands in the step of the hit");
            Assert.That(stage.Shots.Count, Is.EqualTo(1));
            Assert.That(stage.Shots[0].FromX, Is.EqualTo(17f).Within(1e-4f));
            Assert.That(stage.Shots[0].ToX, Is.EqualTo(12f).Within(1e-4f));
            Assert.That(stage.Shots[0].Y, Is.EqualTo(0.6f).Within(1e-4f), "from the middle of its 1.2u box");

            clock.Run(1.2f);                                     // 1.2s later: next hit
            Assert.That(stage.Shots.Count, Is.EqualTo(_heroes.Hits.Count));
        }

        [TestCase(EnemyType.Soldier)]
        [TestCase(EnemyType.Wolf)]
        [TestCase(EnemyType.Giant)]
        [TestCase(EnemyType.Boss)]
        public void A_melee_enemy_hits_without_a_shot(EnemyType type)
        {
            var stage = new FakeStage();
            var clock = new FakeClock();
            using var presenter = Present(new FakeRunFlow(), clock, stage);
            _heroes.Foes.Add(new EnemyFoe(7, 15f, 0.5f));
            _field.Spawn(new EnemySpawn(type, EnemySide.Right, 1f));

            clock.Run(5f);

            Assert.That(_heroes.Hits.Count, Is.GreaterThan(0));
            Assert.That(stage.Shots, Is.Empty);
        }

        [Test]
        public void A_defeated_enemy_shows_the_coins_it_paid_where_it_stood()
        {
            var stage = new FakeStage();
            using var presenter = Present(new FakeRunFlow(), new FakeClock(), stage);
            _field.Spawn(new EnemySpawn(EnemyType.Giant, EnemySide.Left, 1f));
            var handle = Handle();

            _field.Damage(handle, 10f);
            Assert.That(stage.Coins, Is.Empty, "a hurt enemy pays nothing");

            _field.Damage(handle, 1000f);
            Assert.That(stage.Coins.Count, Is.EqualTo(1));
            Assert.That(stage.Coins[0].Coins, Is.EqualTo(15));
            Assert.That(stage.Coins[0].X, Is.EqualTo(-17f).Within(1e-4f));
            Assert.That(stage.Coins[0].Top, Is.EqualTo(2.5f).Within(1e-4f), "starts at the top of its box");

            _field.Damage(handle, 1000f);
            Assert.That(stage.Coins.Count, Is.EqualTo(1), "shown once");
        }

        [Test]
        public void Leaving_the_battle_shows_no_coins_for_the_enemies_removed()
        {
            var stage = new FakeStage();
            using var presenter = Present(new FakeRunFlow(), new FakeClock(), stage);
            _field.Spawn(new EnemySpawn(EnemyType.Soldier, EnemySide.Right, 1f));

            _field.Clear();

            Assert.That(stage.Coins, Is.Empty);
        }

        [Test]
        public void An_enemy_keeps_hitting_a_hero_that_walked_past_it_toward_the_edge()
        {
            _heroes.Foes.Add(new EnemyFoe(7, 17.6f, 0.5f));
            _field.Spawn(new EnemySpawn(EnemyType.Soldier, EnemySide.Right, 1f));

            Run(2f);

            Assert.That(X(), Is.EqualTo(17f), "does not turn around or walk on");
            Assert.That(_heroes.Hits.Count, Is.GreaterThan(0));
            Assert.That(_heroes.Hits.TrueForAll(h => h.Key == 7), Is.True);
        }

        [Test]
        public void When_the_hero_being_hit_falls_the_enemy_turns_to_another_hero_in_range()
        {
            _heroes.Foes.Add(new EnemyFoe(1, 16.0f, 0.5f));
            _heroes.Foes.Add(new EnemyFoe(2, 15.3f, 0.5f));
            _field.Spawn(new EnemySpawn(EnemyType.Soldier, EnemySide.Right, 1f));
            _field.Tick(Step);
            Assert.That(_heroes.Hits[0].Key, Is.EqualTo(1), "nearest hero first");

            _heroes.Foes.RemoveAt(0);
            Run(1.5f);

            Assert.That(_heroes.Hits.FindAll(h => h.Key == 2).Count, Is.GreaterThan(0));
            Assert.That(X(), Is.EqualTo(17f), "stays and hits, does not walk on");
        }

        [Test]
        public void The_hit_wait_is_not_reset_when_the_hero_being_hit_falls()
        {
            _heroes.Foes.Add(new EnemyFoe(1, 16.0f, 0.5f));
            _heroes.Foes.Add(new EnemyFoe(2, 15.3f, 0.5f));
            _field.Spawn(new EnemySpawn(EnemyType.Soldier, EnemySide.Right, 1f));
            _field.Tick(Step);
            Run(0.5f);

            _heroes.Foes.RemoveAt(0);
            _field.Tick(Step);
            Assert.That(_heroes.Hits.FindAll(h => h.Key == 2).Count, Is.Zero, "no fresh hit when switching");

            Run(0.5f);
            Assert.That(_heroes.Hits.FindAll(h => h.Key == 2).Count, Is.EqualTo(1), "next hit on the old rhythm");
        }

        [Test]
        public void Enemies_may_overlap_and_do_not_push_each_other()
        {
            _field.Spawn(new EnemySpawn(EnemyType.Soldier, EnemySide.Right, 1f));
            _field.Spawn(new EnemySpawn(EnemyType.Soldier, EnemySide.Right, 1f));

            Run(15f);

            var alive = new List<EnemyTarget>();
            _field.GetAlive(alive);
            Assert.That(alive[0].X, Is.EqualTo(3.0f).Within(0.05f));
            Assert.That(alive[1].X, Is.EqualTo(3.0f).Within(0.05f));
        }

        [Test]
        public void An_enemy_archer_stops_at_7u_to_shoot_the_castle()
        {
            _field.Spawn(new EnemySpawn(EnemyType.Archer, EnemySide.Right, 1f));

            Run(15f);

            Assert.That(X(), Is.EqualTo(7.0f).Within(0.05f));
            Assert.That(_castle.Hits.Count, Is.GreaterThan(0));
        }

        [Test]
        public void Two_bosses_can_be_on_the_field_at_once()
        {
            _field.Spawn(new EnemySpawn(EnemyType.Boss, EnemySide.Right, 1f));
            _field.Spawn(new EnemySpawn(EnemyType.Boss, EnemySide.Left, 1f));

            Run(25f);

            var alive = new List<EnemyTarget>();
            _field.GetAlive(alive);
            Assert.That(alive.Count, Is.EqualTo(2));
            Assert.That(alive[0].X, Is.EqualTo(4.5f).Within(0.05f));
            Assert.That(alive[1].X, Is.EqualTo(-4.5f).Within(0.05f));
        }

        [Test]
        public void Wave_scale_does_not_change_walking_speed_or_range()
        {
            _field.Spawn(new EnemySpawn(EnemyType.Soldier, EnemySide.Right, 1.9f));

            Run(2f);
            Assert.That(X(), Is.EqualTo(14f).Within(0.05f));

            Run(13f);
            Assert.That(X(), Is.EqualTo(3.0f).Within(0.05f));
        }

        [Test]
        public void Frost_landing_mid_wait_doubles_the_time_left_before_the_next_hit()
        {
            _heroes.Foes.Add(new EnemyFoe(7, 15.5f, 0.5f));
            _field.Spawn(new EnemySpawn(EnemyType.Soldier, EnemySide.Right, 1f));
            _field.Tick(Step);                     // first hit at once, next due after 1.0s
            Run(0.5f);                             // 0.5s gone, 0.5s left

            _field.SlowAll(10f);                   // 0.5s left becomes 1.0s

            Run(0.95f);
            Assert.That(_heroes.Hits.Count, Is.EqualTo(1), "not yet 0.95s after Frost");
            Run(0.05f);
            Assert.That(_heroes.Hits.Count, Is.EqualTo(2), "second hit 1.0s after Frost");
        }

        [Test]
        public void When_Frost_ends_mid_wait_the_time_left_halves_and_the_rhythm_is_back_at_once()
        {
            _heroes.Foes.Add(new EnemyFoe(7, 15.5f, 0.5f));
            _field.Spawn(new EnemySpawn(EnemyType.Soldier, EnemySide.Right, 1f));
            _field.Tick(Step);                     // first hit, next due after 1.0s
            Run(0.5f);                             // 0.5s gone, 0.5s left

            _field.SlowAll(0.4f);                  // 0.5s left -> 1.0s; after 0.4s slowed 0.6s left -> halves to 0.3s
            Run(0.65f);
            Assert.That(_heroes.Hits.Count, Is.EqualTo(1), "not yet 0.65s after Frost");
            Run(0.05f);
            Assert.That(_heroes.Hits.Count, Is.EqualTo(2), "second hit 0.7s after Frost");

            Run(0.95f);
            Assert.That(_heroes.Hits.Count, Is.EqualTo(2), "not yet 0.95s after the second");
            Run(0.05f);
            Assert.That(_heroes.Hits.Count, Is.EqualTo(3), "third hit 1.0s after the second");
        }

        [Test]
        public void A_hero_standing_behind_the_enemy_toward_the_edge_but_in_range_is_still_hit()
        {
            // Enemy centre 17, half width 0.5; hero centre 18.6, half width 0.5: a clear 0.6u gap behind it.
            _heroes.Foes.Add(new EnemyFoe(7, 18.6f, 0.5f));
            _field.Spawn(new EnemySpawn(EnemyType.Soldier, EnemySide.Right, 1f));

            Run(2f);

            Assert.That(X(), Is.EqualTo(17f), "does not turn around or walk on");
            Assert.That(_heroes.Hits.Count, Is.GreaterThan(0));
        }

        [Test]
        public void A_hero_behind_the_enemy_out_of_range_does_not_stop_it()
        {
            // Gap 1.5u behind, more than the soldier's 1.0u range.
            _heroes.Foes.Add(new EnemyFoe(7, 19.5f, 0.5f));
            _field.Spawn(new EnemySpawn(EnemyType.Soldier, EnemySide.Right, 1f));

            Run(2f);

            Assert.That(X(), Is.EqualTo(14f).Within(0.05f), "walks on toward the castle");
            Assert.That(_heroes.Hits, Is.Empty);
        }

        [Test]
        public void When_the_castle_falls_a_walking_enemy_stands_still()
        {
            _field.Spawn(new EnemySpawn(EnemyType.Soldier, EnemySide.Right, 1f));
            Run(1f);
            var x = X();

            _field.Stop();
            Run(5f);

            Assert.That(X(), Is.EqualTo(x));
        }

        // Centre where each type stops before the castle (right side; left is the negative).
        [TestCase(EnemyType.Soldier, 3.0f)]
        [TestCase(EnemyType.Wolf, 3.1f)]
        [TestCase(EnemyType.Giant, 3.7f)]
        [TestCase(EnemyType.Archer, 7.0f)]
        [TestCase(EnemyType.Boss, 4.5f)]
        public void Each_type_stops_at_its_range_from_the_castle_edge_on_both_sides(EnemyType type, float centre)
        {
            _field.Spawn(new EnemySpawn(type, EnemySide.Right, 1f));
            _field.Spawn(new EnemySpawn(type, EnemySide.Left, 1f));

            Run(30f);

            // Steps are 0.025s: an enemy may step into its range by up to one step of walking.
            var tolerance = _config.Kind(type).MoveSpeed * Step + 0.001f;
            var alive = new List<EnemyTarget>();
            _field.GetAlive(alive);
            Assert.That(alive[0].X, Is.EqualTo(centre).Within(tolerance));
            Assert.That(alive[1].X, Is.EqualTo(-centre).Within(tolerance));
            Assert.That(_castle.Hits.Count, Is.GreaterThan(0));
        }

        [Test]
        public void After_losing_and_replaying_the_stage_is_not_left_frozen_and_defeated_enemies_shrink_away()
        {
            var runFlow = new FakeRunFlow();
            var stage = new FakeStage();
            var clock = new FakeClock();
            using var presenter = Present(runFlow, clock, stage);
            _field.Spawn(new EnemySpawn(EnemyType.Soldier, EnemySide.Right, 1f));
            clock.StepOnce();

            // Run 1 is lost: the castle fell. EnemyParticipant answers BattleStopped with Stop.
            _field.Stop();
            runFlow.RaiseStepChanged(RunStep.Ending);
            runFlow.RaiseStepChanged(RunStep.Lost);
            Assert.That(stage.Frozen, Is.True, "frozen while the run is over");

            // Replay, in RunFlow's order: StepChanged(Playing) comes before RunStarted.
            runFlow.RaiseStepChanged(RunStep.Playing);
            _field.ResetForRun();                  // EnemyParticipant answers RunStarted with ResetForRun
            clock.StepOnce();

            Assert.That(stage.Frozen, Is.False, "run 2 is playing");
            _field.Spawn(new EnemySpawn(EnemyType.Soldier, EnemySide.Right, 1f));
            _field.Damage(Handle(), 1000f);
            clock.StepOnce();
            Assert.That(stage.Defeated.Count, Is.EqualTo(1), "the enemy defeated in run 2 is shrunk away");
            Assert.That(stage.Frozen, Is.False);
        }

        [Test]
        public void Enemies_walk_and_hit_only_on_steps_of_the_shared_clock_as_the_Enemy_owner()
        {
            var stage = new FakeStage();
            var clock = new FakeClock();
            using var presenter = Present(new FakeRunFlow(), clock, stage);
            _field.Spawn(new EnemySpawn(EnemyType.Soldier, EnemySide.Right, 1f));

            Assert.That(clock.Owners, Is.EqualTo(new[] { "Enemy" }), "takes its place in the step order by name");
            Assert.That(X(), Is.EqualTo(17f), "nothing moves until the clock steps");

            clock.StepOnce();
            Assert.That(X(), Is.EqualTo(17f - 1.5f * Step).Within(1e-5f), "one step walks 1.5u/s x 0.025s");
            Assert.That(stage.Moves, Is.EqualTo(1), "the stage gets the state after the step");

            clock.Run(1f);
            Assert.That(X(), Is.EqualTo(17f - 1.5f * (1f + Step)).Within(1e-3f));
        }

        [Test]
        public void A_step_that_is_not_gameplay_moves_no_enemy_and_freezes_the_stage()
        {
            // The clock steps, but the run does not let the battle play.
            var stage = new FakeStage();
            var clock = new FakeClock();
            using var presenter = Present(new FakeRunFlow(), clock, stage);
            _heroes.Foes.Add(new EnemyFoe(7, 15.5f, 0.5f));
            _field.Spawn(new EnemySpawn(EnemyType.Soldier, EnemySide.Right, 1f));

            clock.Run(3f, gameplay: false);

            Assert.That(_heroes.Hits, Is.Empty);
            Assert.That(stage.Frozen, Is.True);

            clock.StepOnce();
            Assert.That(_heroes.Hits.Count, Is.EqualTo(1));
            Assert.That(stage.Frozen, Is.False);
        }

        [TestCase(EnemyType.Wolf, 32)]      // 0.8s per hit
        [TestCase(EnemyType.Soldier, 40)]   // 1.0s
        [TestCase(EnemyType.Archer, 48)]    // 1.2s
        [TestCase(EnemyType.Giant, 60)]     // 1.5s
        [TestCase(EnemyType.Boss, 80)]      // 2.0s
        public void The_next_hit_lands_on_the_first_step_where_its_seconds_per_hit_are_reached(EnemyType type,
            int steps)
        {
            var clock = new FakeClock();
            using var presenter = Present(new FakeRunFlow(), clock, new FakeStage());
            _heroes.Foes.Add(new EnemyFoe(7, 15.5f, 0.5f));
            _field.Spawn(new EnemySpawn(type, EnemySide.Right, 1f));
            clock.StepOnce();
            Assert.That(_heroes.Hits.Count, Is.EqualTo(1), "first hit at once");

            for (var i = 0; i < steps - 1; i++) clock.StepOnce();
            Assert.That(_heroes.Hits.Count, Is.EqualTo(1), $"not yet after {steps - 1} steps");

            clock.StepOnce();
            Assert.That(_heroes.Hits.Count, Is.EqualTo(2), $"next hit after exactly {steps} steps");
        }

        [Test]
        public void After_the_battle_view_is_closed_it_no_longer_follows_the_clock_or_the_combat_ticks()
        {
            var clock = new FakeClock();
            var stage = new FakeStage();
            var presenter = Present(new FakeRunFlow(), clock, stage);
            _field.Spawn(new EnemySpawn(EnemyType.Soldier, EnemySide.Right, 1f));

            presenter.Dispose();
            clock.Run(1f);

            Assert.That(clock.Subscribers, Is.Zero);
            Assert.That(clock.TickDoneListeners, Is.Zero);
            Assert.That(stage.Moves, Is.Zero);
        }

        // ---- Through the shared combat (live/feature-combat/combat-tick/tick-values) ----

        [Test]
        public void An_enemy_hits_its_combat_foes_through_combat_with_their_receiver_and_key()
        {
            var clock = new FakeClock();
            _field.JoinCombat(clock);
            var heroes = new FakeFoes();
            heroes.Foes.Add(new EnemyFoe(7, 15.5f, 0.5f));
            var heroReceiver = clock.AddReceiver(new FakeReceiver());
            _field.AddCombatFoes(heroes, heroReceiver);
            _field.Spawn(new EnemySpawn(EnemyType.Soldier, EnemySide.Right, 1.9f));

            clock.StepOnce();

            Assert.That(heroes.Hits, Is.Empty, "no health taken by the enemy itself");
            Assert.That(clock.Hits.Count, Is.EqualTo(1));
            Assert.That(clock.Hits[0].Receiver, Is.EqualTo(heroReceiver));
            Assert.That(clock.Hits[0].TargetKey, Is.EqualTo(7));
            Assert.That(clock.Hits[0].Damage, Is.EqualTo(9.5f).Within(1e-4f), "damage fixed when dealt, scaled");
            Assert.That(clock.Hits[0].Source, Is.EqualTo(CombatSource.Enemy));
        }

        [Test]
        public void Foes_moved_to_combat_keep_their_place_in_the_order_enemies_try_them()
        {
            // Heroes were added first the old way, then moved to combat: a hero in range still wins over the castle.
            var clock = new FakeClock();
            _field.JoinCombat(clock);
            _heroes.Foes.Add(new EnemyFoe(7, 2.5f, 0.5f));
            var heroReceiver = clock.AddReceiver(new FakeReceiver());
            _field.AddCombatFoes(_heroes, heroReceiver);
            _field.Spawn(new EnemySpawn(EnemyType.Soldier, EnemySide.Right, 1f));

            clock.Run(15f);

            Assert.That(clock.Hits.Count, Is.GreaterThan(0));
            Assert.That(clock.Hits.TrueForAll(h => h.Receiver == heroReceiver), Is.True);
            Assert.That(_castle.Hits, Is.Empty);
        }

        [Test]
        public void Every_enemy_aims_before_any_enemy_strikes_in_a_combat_tick()
        {
            var clock = new FakeClock();
            _field.JoinCombat(clock);
            _heroes.Foes.Add(new EnemyFoe(7, 15.5f, 0.5f));
            _field.AddCombatFoes(_heroes, clock.AddReceiver(new FakeReceiver()));
            _field.Spawn(new EnemySpawn(EnemyType.Soldier, EnemySide.Right, 1f));
            _field.Spawn(new EnemySpawn(EnemyType.Soldier, EnemySide.Right, 1f));

            clock.StepOnce();

            Assert.That(clock.Calls, Is.EqualTo(new[] { "aim", "strike" }), "aim then strike, once each per tick");
            Assert.That(clock.Hits.Count, Is.EqualTo(2), "both hit in the tick they reached the hero");
        }

        [Test]
        public void Hits_on_enemies_go_through_combat_and_an_enemy_takes_one_total_per_tick()
        {
            var income = new FakeIncome();
            var field = new EnemyField(_config, income);
            var clock = new FakeClock();
            field.JoinCombat(clock);
            var defeats = new List<EnemyDefeat>();
            field.Defeated += d => defeats.Add(d);
            field.Spawn(new EnemySpawn(EnemyType.Soldier, EnemySide.Right, 1f));   // 40 health
            var alive = new List<EnemyTarget>();
            field.GetAlive(alive);
            var handle = alive[0].Handle;

            // A hero and the castle each hit for 25 in the same tick: one total of 50.
            clock.Hit(new CombatHit(field.CombatReceiver, handle, 25f, CombatSource.Hero));
            clock.Hit(new CombatHit(field.CombatReceiver, handle, 25f, CombatSource.Castle));
            clock.StepOnce();

            Assert.That(field.AliveCount, Is.Zero);
            Assert.That(defeats.Count, Is.EqualTo(1), "defeated once");
            Assert.That(income.Added, Is.EqualTo(new[] { 5 }), "coins once, not split by side");
        }

        [Test]
        public void An_enemy_hit_below_its_health_by_combat_is_hurt_and_stays()
        {
            var clock = new FakeClock();
            _field.JoinCombat(clock);
            var stage = new FakeStage();
            using var presenter = new EnemyPresenter(_field, new FakeRunFlow(), clock, clock, stage);
            _field.Spawn(new EnemySpawn(EnemyType.Soldier, EnemySide.Right, 1f));

            clock.Hit(new CombatHit(_field.CombatReceiver, Handle(), 39.5f, CombatSource.Hero));
            clock.StepOnce();

            Assert.That(_field.AliveCount, Is.EqualTo(1), "0.5 health left keeps it on the field");
            Assert.That(stage.Flashed, Is.EqualTo(new[] { Handle() }));
            Assert.That(stage.Defeated, Is.Empty);
        }

        [Test]
        public void A_defeated_enemy_takes_no_more_hits_and_after_the_battle_stopped_no_enemy_can_be_hit()
        {
            var clock = new FakeClock();
            _field.JoinCombat(clock);
            _field.Spawn(new EnemySpawn(EnemyType.Soldier, EnemySide.Right, 1f));
            _field.Spawn(new EnemySpawn(EnemyType.Wolf, EnemySide.Left, 1f));
            var alive = new List<EnemyTarget>();
            _field.GetAlive(alive);

            clock.Hit(new CombatHit(_field.CombatReceiver, alive[0].Handle, 100f, CombatSource.Hero));
            clock.StepOnce();
            Assert.That(_field.CanBeHit(alive[0].Handle), Is.False, "defeated");
            Assert.That(_field.CanBeHit(alive[1].Handle), Is.True);

            _field.Stop();
            Assert.That(_field.CanBeHit(alive[1].Handle), Is.False, "battle stopped");
        }

        [Test]
        public void Enemy_and_hero_that_finish_each_other_in_one_tick_both_fall()
        {
            // The enemy strikes from the state at the start of the tick, so it hits even though it falls in that tick.
            var clock = new FakeClock();
            _field.JoinCombat(clock);
            var heroSide = new FakeReceiver();
            var heroes = new FakeFoes();
            heroes.Foes.Add(new EnemyFoe(7, 15.5f, 0.5f));
            _field.AddCombatFoes(heroes, clock.AddReceiver(heroSide));
            _field.Spawn(new EnemySpawn(EnemyType.Soldier, EnemySide.Right, 1f));

            clock.Hit(new CombatHit(_field.CombatReceiver, Handle(), 100f, CombatSource.Hero));
            clock.StepOnce();

            Assert.That(heroSide.Totals, Is.EqualTo(new[] { (7, 5f) }), "the hero took the enemy's hit");
            Assert.That(_field.AliveCount, Is.Zero, "the enemy fell too");
        }

        [Test]
        public void Joining_combat_adds_the_enemies_once_as_receiver_and_actor()
        {
            var clock = new FakeClock();
            _field.JoinCombat(clock);

            Assert.That(clock.Receivers, Is.EqualTo(new object[] { _field }));
            Assert.That(clock.Actors, Is.EqualTo(new object[] { _field }));
            Assert.That(_field.CombatReceiver, Is.EqualTo(0));
            Assert.Throws<InvalidOperationException>(() => _field.JoinCombat(clock));
        }

        [TestCase(EnemySide.Right)]
        [TestCase(EnemySide.Left)]
        public void A_boss_plays_its_entrance_once_on_the_step_its_centre_reaches_16u(EnemySide side)
        {
            // From x=±17u at 0.6u/s, 0.015u a step: 66 steps leave it at 16.01u, the 67th at 15.995u.
            var stage = new FakeStage();
            var clock = new FakeClock();
            using var presenter = Present(new FakeRunFlow(), clock, stage);
            _field.Spawn(new EnemySpawn(EnemyType.Boss, side, 1f));

            for (var i = 0; i < 66; i++) clock.StepOnce();
            Assert.That(stage.Entered, Is.Empty, "still off screen");

            clock.StepOnce();
            Assert.That(stage.Entered, Is.EqualTo(new[] { Handle() }));

            clock.Run(10f);
            Assert.That(stage.Entered.Count, Is.EqualTo(1), "once per boss");
        }

        [TestCase(EnemyType.Soldier)]
        [TestCase(EnemyType.Wolf)]
        [TestCase(EnemyType.Giant)]
        [TestCase(EnemyType.Archer)]
        public void Other_enemy_types_come_on_screen_without_an_entrance(EnemyType type)
        {
            var stage = new FakeStage();
            var clock = new FakeClock();
            using var presenter = Present(new FakeRunFlow(), clock, stage);
            _field.Spawn(new EnemySpawn(type, EnemySide.Right, 1f));

            clock.Run(5f);

            Assert.That(X(), Is.LessThan(16f), "it is on screen");
            Assert.That(stage.Entered, Is.Empty);
        }

        [Test]
        public void A_boss_blocked_off_screen_plays_its_entrance_only_when_it_walks_in()
        {
            // A hero at x=18.8u (half width 0.5) is within the boss's 1.5u range: it stands at 17u and hits.
            var stage = new FakeStage();
            var clock = new FakeClock();
            using var presenter = Present(new FakeRunFlow(), clock, stage);
            _heroes.Foes.Add(new EnemyFoe(7, 18.8f, 0.5f));
            _field.Spawn(new EnemySpawn(EnemyType.Boss, EnemySide.Right, 1f));

            clock.Run(3f);
            Assert.That(stage.Entered, Is.Empty);

            _heroes.Foes.Clear();
            clock.Run(3f);
            Assert.That(stage.Entered.Count, Is.EqualTo(1));
        }

        [Test]
        public void A_slowed_enemy_shows_the_slow_mark_for_exactly_as_long_as_it_is_slowed()
        {
            // PROTOTYPE (enemy-move-values, Thông số theo giai đoạn): the slow mark shows all the time Frost lasts.
            _field.Spawn(new EnemySpawn(EnemyType.Soldier, EnemySide.Right, 1f));
            var looks = new List<EnemyLook>();

            _field.GetLooks(looks);
            Assert.That(looks[0].Slowed, Is.False, "not slowed yet");

            _field.SlowAll(1f);
            _field.GetLooks(looks);
            Assert.That(looks[0].Slowed, Is.True, "marked as soon as Frost lands");

            Run(1f - Step);
            _field.GetLooks(looks);
            Assert.That(looks[0].Slowed, Is.True, "still marked one step before Frost ends");

            Run(Step);
            _field.GetLooks(looks);
            Assert.That(looks[0].Slowed, Is.False, "the mark goes when Frost ends");
        }

        /// <summary>As the installer wires it: the enemies join combat (the fake clock plays combat too), then the presenter shows them.</summary>
        private EnemyPresenter Present(IRunFlow runFlow, FakeClock clock, IEnemyStage stage)
        {
            _field.JoinCombat(clock);
            return new EnemyPresenter(_field, runFlow, clock, clock, stage);
        }

        private static EnemyConfig AllTypesConfig()
        {
            var real = AssetDatabase.LoadAssetAtPath<EnemyConfig>(ConfigPath);
            Assert.That(real, Is.Not.Null, $"Missing {ConfigPath}");
            var copy = UnityEngine.Object.Instantiate(real);
            var so = new SerializedObject(copy);
            var kinds = so.FindProperty("_kinds");
            for (var i = 0; i < kinds.arraySize; i++)
                kinds.GetArrayElementAtIndex(i).FindPropertyRelative("_available").boolValue = true;
            so.ApplyModifiedPropertiesWithoutUndo();
            return copy;
        }

        private void Run(float seconds)
        {
            var steps = (int)Math.Round(seconds / Step);
            for (var i = 0; i < steps; i++) _field.Tick(Step);
        }

        private float X()
        {
            var alive = new List<EnemyTarget>();
            _field.GetAlive(alive);
            return alive[0].X;
        }

        private int Handle()
        {
            var alive = new List<EnemyTarget>();
            _field.GetAlive(alive);
            return alive[0].Handle;
        }

        private sealed class FakeFoes : IEnemyFoes
        {
            public readonly List<EnemyFoe> Foes = new List<EnemyFoe>();
            public readonly List<(int Key, float Damage)> Hits = new List<(int, float)>();

            public void GetFoes(List<EnemyFoe> into)
            {
                into.Clear();
                into.AddRange(Foes);
            }

            public void Hit(int key, float damage) => Hits.Add((key, damage));
        }

        private sealed class FakeIncome : IWalletIncome
        {
            public readonly List<int> Added = new List<int>();
            public void Add(int amount) => Added.Add(amount);
        }

        /// <summary>Another owner of sides combat can hit (a hero squad): every side can be hit, totals are recorded.</summary>
        private sealed class FakeReceiver : ICombatReceiver
        {
            public readonly List<(int Key, float Total)> Totals = new List<(int, float)>();
            public bool CanBeHit(int key) => true;
            public void TakeTotal(int key, float total) => Totals.Add((key, total));
            public void ReportFallen() { }
        }

        private sealed class FakeStage : IEnemyStage
        {
            public void UseArt(Game.Core.IArt art) { }
            public readonly List<int> Shown = new List<int>();
            public readonly List<int> Flashed = new List<int>();
            public readonly List<int> Defeated = new List<int>();
            public readonly List<int> Lunges = new List<int>();
            public readonly List<EnemyShot> Shots = new List<EnemyShot>();
            public readonly List<EnemyReward> Coins = new List<EnemyReward>();

            public int Moves;
            public void TrackWith(Func<UnityEngine.Object, IDisposable> trackView) { }
            public void Show(EnemyLook look) => Shown.Add(look.Handle);
            public void Move(EnemyLook look) => Moves++;
            public void Lunge(int handle, int direction) => Lunges.Add(direction);
            public void Shoot(EnemyShot shot) => Shots.Add(shot);
            public void ShowCoins(EnemyReward reward) => Coins.Add(reward);
            public void Flash(int handle) => Flashed.Add(handle);
            public readonly List<int> Entered = new List<int>();
            public void Enter(int handle) => Entered.Add(handle);
            public void Defeat(int handle) => Defeated.Add(handle);
            public void RemoveAll() { }
            public bool Frozen { get; private set; }
            public void SetFrozen(bool frozen) => Frozen = frozen;
            public void Close() { }
        }

        /// <summary>
        /// The shared clock, stepped by hand (0.025s steps, subscribers called in the order they joined), and the
        /// shared combat it runs after them on each gameplay step: aim, strike, filter, one total per side, report
        /// fallen, tick done (live/feature-combat/combat-tick/tick-values, tick-order).
        /// </summary>
        private sealed class FakeClock : ISimClock, ICombat
        {
            private readonly List<(string Owner, Action<SimStep> OnStep)> _subscribers =
                new List<(string, Action<SimStep>)>();
            private readonly List<CombatHit> _pending = new List<CombatHit>();

            public readonly List<ICombatReceiver> Receivers = new List<ICombatReceiver>();
            public readonly List<ICombatActor> Actors = new List<ICombatActor>();
            public readonly List<CombatHit> Hits = new List<CombatHit>();
            public readonly List<string> Calls = new List<string>();
            public event Action TickDone;
            public int TickDoneListeners => TickDone?.GetInvocationList().Length ?? 0;

            public int AddReceiver(ICombatReceiver receiver)
            {
                Receivers.Add(receiver);
                return Receivers.Count - 1;
            }

            public void AddActor(ICombatActor actor) => Actors.Add(actor);

            public void Hit(in CombatHit hit)
            {
                Hits.Add(hit);
                _pending.Add(hit);
            }

            private void RunCombatTick()
            {
                foreach (var actor in Actors) { Calls.Add("aim"); actor.Aim(Step); }
                foreach (var actor in Actors) { Calls.Add("strike"); actor.Strike(Step); }

                var totals = new Dictionary<(int, int), float>();
                foreach (var hit in _pending)
                {
                    if (!Receivers[hit.Receiver].CanBeHit(hit.TargetKey)) continue;
                    totals.TryGetValue((hit.Receiver, hit.TargetKey), out var sum);
                    totals[(hit.Receiver, hit.TargetKey)] = sum + hit.Damage;
                }

                _pending.Clear();
                var hitReceivers = new HashSet<int>();
                foreach (var pair in totals)
                {
                    Receivers[pair.Key.Item1].TakeTotal(pair.Key.Item2, pair.Value);
                    hitReceivers.Add(pair.Key.Item1);
                }

                foreach (var receiver in hitReceivers) Receivers[receiver].ReportFallen();
                TickDone?.Invoke();
            }

            public readonly List<string> Owners = new List<string>();
            public SimMode Mode => SimMode.FixedTick;
            public float StepSeconds => Step;
            public long StepCount { get; private set; }
            public long GameplayStepCount { get; private set; }
            public double GameplaySeconds => GameplayStepCount * (double)Step;
            public int Subscribers => _subscribers.Count;

            public IDisposable OnStep(string owner, Action<SimStep> onStep)
            {
                var entry = (owner, onStep);
                _subscribers.Add(entry);
                Owners.Add(owner);
                return new Handle(() => _subscribers.Remove(entry));
            }

            public void Enqueue(string owner, Action apply) => throw new NotSupportedException();

            public void StepOnce(bool gameplay = true)
            {
                StepCount++;
                if (gameplay) GameplayStepCount++;
                var step = new SimStep(StepCount, GameplayStepCount, Step, gameplay);
                foreach (var subscriber in _subscribers.ToArray()) subscriber.OnStep(step);
                if (gameplay) RunCombatTick();
            }

            public void Run(float seconds, bool gameplay = true)
            {
                var steps = (int)Math.Round(seconds / Step);
                for (var i = 0; i < steps; i++) StepOnce(gameplay);
            }

            private sealed class Handle : IDisposable
            {
                private Action _dispose;
                public Handle(Action dispose) => _dispose = dispose;

                public void Dispose()
                {
                    _dispose?.Invoke();
                    _dispose = null;
                }
            }
        }

        private sealed class FakeRunFlow : IRunFlow
        {
            private RunStep _step = RunStep.Playing;
            public RunStep Step => _step;
            public RunTeam Team => default;

            public void RaiseStepChanged(RunStep step)
            {
                _step = step;
                StepChanged?.Invoke(step);
            }

            public event Action<RunStep> StepChanged;
#pragma warning disable 67
            public event Action<RunTeam> RunStarted;
            public event Action BattleStarted;
            public event Action BattleStopped;
            public event Action BattleCleared;
#pragma warning restore 67
            public void Play() { }
            public void EnterBattle(RunTeam team) { }
            public void Replay() { }
            public void GoHome() { }
            public void Lose() { }
            public void CountClearedWave(int wave) { }
        }
    }
}
