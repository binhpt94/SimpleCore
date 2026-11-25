using System;
using System.Collections.Generic;
using Game.Run;
using Game.Wallet;
using NUnit.Framework;
using UnityEditor;

namespace Game.Enemy.Tests
{
    /// <summary>Walking, stopping at foes, hitting on rhythm, Frost and pause (live/feature-enemy/enemy-behavior/enemy-move-values).</summary>
    public sealed class EnemyCombatTests
    {
        private const string ConfigPath = "Assets/Features/Enemy/FeatureAssets/Private/EnemyConfig.asset";
        private const float Step = 0.02f;

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
        public void While_paused_enemies_stand_still()
        {
            var runFlow = new FakeRunFlow();
            var stage = new FakeStage();
            using var presenter = new EnemyPresenter(_field, runFlow, stage);
            _field.Spawn(new EnemySpawn(EnemyType.Soldier, EnemySide.Right, 1f));

            runFlow.Paused = true;
            stage.RaiseFrame(2f);
            Assert.That(X(), Is.EqualTo(17f));

            runFlow.Paused = false;
            stage.RaiseFrame(1f);
            Assert.That(X(), Is.EqualTo(15.5f).Within(0.001f));
        }

        [Test]
        public void The_stage_shows_spawned_enemies_flashes_hurt_ones_and_shrinks_defeated_ones()
        {
            var stage = new FakeStage();
            using var presenter = new EnemyPresenter(_field, new FakeRunFlow(), stage);

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
            using var presenter = new EnemyPresenter(_field, new FakeRunFlow(), stage);
            _heroes.Foes.Add(new EnemyFoe(7, 15.5f, 0.5f));
            _field.Spawn(new EnemySpawn(EnemyType.Soldier, EnemySide.Right, 1f));

            stage.RaiseFrame(Step);

            Assert.That(stage.Lunges, Is.EqualTo(new[] { -1 }));
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
        public void Pause_keeps_the_time_left_on_Frost()
        {
            var runFlow = new FakeRunFlow();
            var stage = new FakeStage();
            using var presenter = new EnemyPresenter(_field, runFlow, stage);
            _field.Spawn(new EnemySpawn(EnemyType.Soldier, EnemySide.Right, 1f));
            _field.SlowAll(2f);

            runFlow.Paused = true;
            stage.RaiseFrame(5f);
            runFlow.Paused = false;
            stage.RaiseFrame(1f);

            Assert.That(X(), Is.EqualTo(17f - 0.75f).Within(0.001f), "still slowed after the pause");
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
            Run(0.48f);                            // 0.5s gone, 0.5s left

            _field.SlowAll(10f);                   // 0.5s left becomes 1.0s

            Run(0.94f);
            Assert.That(_heroes.Hits.Count, Is.EqualTo(1), "not yet at 1.44s");
            Run(0.1f);
            Assert.That(_heroes.Hits.Count, Is.EqualTo(2), "second hit at 1.5s");
        }

        [Test]
        public void When_Frost_ends_mid_wait_the_time_left_halves_and_the_rhythm_is_back_at_once()
        {
            _heroes.Foes.Add(new EnemyFoe(7, 15.5f, 0.5f));
            _field.Spawn(new EnemySpawn(EnemyType.Soldier, EnemySide.Right, 1f));
            _field.Tick(Step);                     // hit at 0, next due at 1.0s
            Run(0.48f);                            // 0.5s gone, 0.5s left

            _field.SlowAll(0.4f);                  // 0.5s left -> 1.0s; after 0.4s slowed 0.6s left -> halves to 0.3s
            Run(0.64f);
            Assert.That(_heroes.Hits.Count, Is.EqualTo(1), "not yet at 1.14s");
            Run(0.1f);
            Assert.That(_heroes.Hits.Count, Is.EqualTo(2), "second hit at 1.2s");

            Run(0.9f);
            Assert.That(_heroes.Hits.Count, Is.EqualTo(2), "not yet at 2.14s");
            Run(0.1f);
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

        [Test]
        public void Pause_keeps_the_time_left_before_the_next_hit()
        {
            var runFlow = new FakeRunFlow();
            var stage = new FakeStage();
            using var presenter = new EnemyPresenter(_field, runFlow, stage);
            _heroes.Foes.Add(new EnemyFoe(7, 15.5f, 0.5f));
            _field.Spawn(new EnemySpawn(EnemyType.Soldier, EnemySide.Right, 1f));
            stage.RaiseFrame(Step);                // hit at once, next due after 1.0s
            stage.RaiseFrame(0.4f);                // 0.6s left
            Assert.That(_heroes.Hits.Count, Is.EqualTo(1));

            runFlow.Paused = true;
            stage.RaiseFrame(3f);
            Assert.That(_heroes.Hits.Count, Is.EqualTo(1), "no hit while paused, even past 1.0s");

            runFlow.Paused = false;
            stage.RaiseFrame(0.5f);
            Assert.That(_heroes.Hits.Count, Is.EqualTo(1), "0.1s still left");
            stage.RaiseFrame(0.1f);
            Assert.That(_heroes.Hits.Count, Is.EqualTo(2), "next hit when the time left runs out");
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

            // Frames are 0.02s: an enemy may step into its range by up to one frame of walking.
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
            using var presenter = new EnemyPresenter(_field, runFlow, stage);
            _field.Spawn(new EnemySpawn(EnemyType.Soldier, EnemySide.Right, 1f));
            stage.RaiseFrame(Step);

            // Run 1 is lost: the castle fell. EnemyParticipant answers BattleStopped with Stop.
            _field.Stop();
            runFlow.RaiseStepChanged(RunStep.Ending);
            runFlow.RaiseStepChanged(RunStep.Lost);
            Assert.That(stage.Paused, Is.True, "frozen while the run is over");

            // Replay, in RunFlow's order: StepChanged(Playing) comes before RunStarted.
            runFlow.RaiseStepChanged(RunStep.Playing);
            _field.ResetForRun();                  // EnemyParticipant answers RunStarted with ResetForRun
            stage.RaiseFrame(Step);

            Assert.That(stage.Paused, Is.False, "run 2 is playing");
            _field.Spawn(new EnemySpawn(EnemyType.Soldier, EnemySide.Right, 1f));
            _field.Damage(Handle(), 1000f);
            stage.RaiseFrame(Step);
            Assert.That(stage.Defeated.Count, Is.EqualTo(1), "the enemy defeated in run 2 is shrunk away");
            Assert.That(stage.Paused, Is.False);
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
            public void Add(int amount) { }
        }

        private sealed class FakeStage : IEnemyStage
        {
            public readonly List<int> Shown = new List<int>();
            public readonly List<int> Flashed = new List<int>();
            public readonly List<int> Defeated = new List<int>();
            public readonly List<int> Lunges = new List<int>();

            public event Action<float> Frame;
            public void RaiseFrame(float seconds) => Frame?.Invoke(seconds);
            public void Show(EnemyLook look) => Shown.Add(look.Handle);
            public void Move(EnemyLook look) { }
            public void Lunge(int handle, int direction) => Lunges.Add(direction);
            public void Flash(int handle) => Flashed.Add(handle);
            public void Defeat(int handle) => Defeated.Add(handle);
            public void RemoveAll() { }
            public bool Paused { get; private set; }
            public void SetPaused(bool paused) => Paused = paused;
            public void Close() { }
        }

        private sealed class FakeRunFlow : IRunFlow
        {
            public bool Paused;
            private RunStep _step = RunStep.Playing;
            public RunStep Step => Paused ? RunStep.Paused : _step;
            public RunTeam Team => default;
            public bool IsPaused => Paused;

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
            public void Pause() { }
            public void Resume() { }
            public void Abandon() { }
            public void Replay() { }
            public void GoHome() { }
            public void Lose() { }
            public void CountClearedWave(int wave) { }
        }
    }
}
