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

        private EnemyField _field;
        private FakeFoes _heroes;
        private FakeFoes _castle;

        [SetUp]
        public void SetUp()
        {
            var config = AssetDatabase.LoadAssetAtPath<EnemyConfig>(ConfigPath);
            Assert.That(config, Is.Not.Null, $"Missing {ConfigPath}");
            _field = new EnemyField(config, new FakeIncome());
            _heroes = new FakeFoes();
            _castle = new FakeFoes();
            _castle.Foes.Add(new EnemyFoe(1, 0f, 1.5f));
            _field.AddFoes(_heroes);
            _field.AddFoes(_castle);
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
            public void SetPaused(bool paused) { }
            public void Close() { }
        }

        private sealed class FakeRunFlow : IRunFlow
        {
            public bool Paused;
            public RunStep Step => Paused ? RunStep.Paused : RunStep.Playing;
            public RunTeam Team => default;
            public bool IsPaused => Paused;
#pragma warning disable 67
            public event Action<RunStep> StepChanged;
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
