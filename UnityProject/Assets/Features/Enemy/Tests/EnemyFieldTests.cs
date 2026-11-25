using System.Collections.Generic;
using Game.Wallet;
using NUnit.Framework;
using UnityEditor;

namespace Game.Enemy.Tests
{
    public sealed class EnemyFieldTests
    {
        private const string ConfigPath = "Assets/Features/Enemy/FeatureAssets/Private/EnemyConfig.asset";

        private EnemyConfig _config;
        private FakeIncome _income;
        private EnemyField _field;
        private List<EnemyDefeat> _defeats;

        [SetUp]
        public void SetUp()
        {
            // The rules are the same for every type, also those not in the game at this stage: test them all.
            var real = AssetDatabase.LoadAssetAtPath<EnemyConfig>(ConfigPath);
            Assert.That(real, Is.Not.Null, $"Missing {ConfigPath}");
            _config = UnityEngine.Object.Instantiate(real);
            var so = new SerializedObject(_config);
            var kinds = so.FindProperty("_kinds");
            for (var i = 0; i < kinds.arraySize; i++)
                kinds.GetArrayElementAtIndex(i).FindPropertyRelative("_available").boolValue = true;
            so.ApplyModifiedPropertiesWithoutUndo();

            _income = new FakeIncome();
            _field = new EnemyField(_config, _income);
            _defeats = new List<EnemyDefeat>();
            _field.Defeated += d => _defeats.Add(d);
        }

        [TearDown]
        public void TearDown()
        {
            if (_config != null) UnityEngine.Object.DestroyImmediate(_config);
        }

        [TestCase(EnemyType.Archer)]
        [TestCase(EnemyType.Boss)]
        public void Asking_for_a_type_not_in_the_game_yet_fails_naming_it_and_spawns_nothing(EnemyType type)
        {
            // Every type is in the game from PROTOTYPE on (live: enemy-type-values), so take a config of a stage
            // where this type is not yet in the game, as at IDEA.
            var kinds = new SerializedObject(_config).FindProperty("_kinds");
            for (var i = 0; i < kinds.arraySize; i++)
            {
                var kind = kinds.GetArrayElementAtIndex(i);
                if (kind.FindPropertyRelative("_type").intValue == (int)type)
                    kind.FindPropertyRelative("_available").boolValue = false;
            }
            kinds.serializedObject.ApplyModifiedPropertiesWithoutUndo();
            var field = new EnemyField(_config, new FakeIncome());

            var error = Assert.Throws<System.InvalidOperationException>(
                () => field.Spawn(new EnemySpawn(type, EnemySide.Left, 1f)));

            StringAssert.Contains(type.ToString(), error.Message);
            Assert.That(field.AliveCount, Is.Zero);
        }

        // PROTOTYPE: all five types are in the game (live: enemy-type-values, Thông số theo giai đoạn).
        [TestCase(EnemyType.Soldier)]
        [TestCase(EnemyType.Wolf)]
        [TestCase(EnemyType.Giant)]
        [TestCase(EnemyType.Archer)]
        [TestCase(EnemyType.Boss)]
        public void Every_enemy_type_can_be_spawned_in_this_stage(EnemyType type)
        {
            var real = AssetDatabase.LoadAssetAtPath<EnemyConfig>(ConfigPath);
            var field = new EnemyField(real, new FakeIncome());

            field.Spawn(new EnemySpawn(type, EnemySide.Left, 1f));

            Assert.That(field.AliveCount, Is.EqualTo(1));
        }

        [Test]
        public void A_spawned_enemy_appears_at_the_edge_of_its_side()
        {
            _field.Spawn(new EnemySpawn(EnemyType.Soldier, EnemySide.Left, 1f));
            _field.Spawn(new EnemySpawn(EnemyType.Soldier, EnemySide.Right, 1f));

            var alive = new List<EnemyTarget>();
            _field.GetAlive(alive);

            Assert.That(_field.AliveCount, Is.EqualTo(2));
            Assert.That(alive[0].X, Is.EqualTo(-17f));
            Assert.That(alive[1].X, Is.EqualTo(17f));
            Assert.That(alive[0].SpawnOrder, Is.LessThan(alive[1].SpawnOrder));
        }

        [Test]
        public void Defeating_an_enemy_pays_its_coins_once_then_reports_it()
        {
            _field.Spawn(new EnemySpawn(EnemyType.Soldier, EnemySide.Left, 1f));
            var handle = FirstHandle();
            _field.Defeated += _ => Assert.That(_income.Added, Is.EqualTo(new[] { 5 }), "coins come before Defeated");

            Assert.That(_field.Damage(handle, 39f), Is.False);
            Assert.That(_field.Damage(handle, 1f), Is.True);
            Assert.That(_field.Damage(handle, 10f), Is.False);

            Assert.That(_income.Added, Is.EqualTo(new[] { 5 }));
            Assert.That(_defeats.Count, Is.EqualTo(1));
            Assert.That(_field.AliveCount, Is.Zero);
        }

        [Test]
        public void Wave_scale_multiplies_enemy_health()
        {
            _field.Spawn(new EnemySpawn(EnemyType.Soldier, EnemySide.Right, 1.9f));
            var handle = FirstHandle();

            Assert.That(_field.Damage(handle, 75f), Is.False);
            Assert.That(_field.Damage(handle, 1f), Is.True);
        }

        [Test]
        public void After_the_battle_stopped_no_enemy_is_defeated_and_no_coins_are_paid()
        {
            _field.Spawn(new EnemySpawn(EnemyType.Soldier, EnemySide.Left, 1f));
            var handle = FirstHandle();

            _field.Stop();

            Assert.That(_field.Damage(handle, 1000f), Is.False);
            Assert.That(_income.Added, Is.Empty);
            Assert.That(_defeats, Is.Empty);
        }

        [Test]
        public void Leaving_the_battle_removes_enemies_without_paying()
        {
            _field.Spawn(new EnemySpawn(EnemyType.Giant, EnemySide.Left, 1f));

            _field.Clear();

            Assert.That(_field.AliveCount, Is.Zero);
            Assert.That(_income.Added, Is.Empty);
            Assert.That(_defeats, Is.Empty);
        }

        [TestCase(EnemyType.Soldier, 5)]
        [TestCase(EnemyType.Wolf, 4)]
        [TestCase(EnemyType.Giant, 15)]
        [TestCase(EnemyType.Archer, 6)]
        [TestCase(EnemyType.Boss, 100)]
        public void Defeating_an_enemy_pays_the_coins_of_its_type(EnemyType type, int coins)
        {
            _field.Spawn(new EnemySpawn(type, EnemySide.Left, 1f));

            _field.Damage(FirstHandle(), 100000f);

            Assert.That(_income.Added, Is.EqualTo(new[] { coins }));
        }

        [Test]
        public void Coins_for_an_enemy_do_not_grow_with_the_wave_scale()
        {
            _field.Spawn(new EnemySpawn(EnemyType.Soldier, EnemySide.Left, 1.9f));

            _field.Damage(FirstHandle(), 100000f);

            Assert.That(_income.Added, Is.EqualTo(new[] { 5 }));
        }

        [Test]
        public void An_area_hit_that_defeats_several_enemies_pays_each_of_them()
        {
            _field.Spawn(new EnemySpawn(EnemyType.Soldier, EnemySide.Left, 1f));
            _field.Spawn(new EnemySpawn(EnemyType.Wolf, EnemySide.Right, 1f));
            var alive = new List<EnemyTarget>();
            _field.GetAlive(alive);

            for (var i = 0; i < alive.Count; i++) _field.Damage(alive[i].Handle, 100f);

            Assert.That(_income.Added, Is.EqualTo(new[] { 5, 4 }));
            Assert.That(_defeats.Count, Is.EqualTo(2));
        }

        [Test]
        public void An_enemy_never_heals_over_time()
        {
            _field.Spawn(new EnemySpawn(EnemyType.Soldier, EnemySide.Right, 1f));
            var handle = FirstHandle();
            _field.Damage(handle, 39f);

            for (var i = 0; i < 500; i++) _field.Tick(0.02f);

            Assert.That(_field.Damage(handle, 1f), Is.True, "the 1 health left is all it has");
        }

        [Test]
        public void A_slowed_enemy_that_is_defeated_leaves_nothing_behind()
        {
            _field.Spawn(new EnemySpawn(EnemyType.Soldier, EnemySide.Right, 1f));
            _field.SlowAll(5f);

            _field.Damage(FirstHandle(), 100f);

            var looks = new List<EnemyLook>();
            _field.GetLooks(looks);
            Assert.That(looks, Is.Empty);
            Assert.That(_field.AliveCount, Is.Zero);
        }

        private int FirstHandle()
        {
            var alive = new List<EnemyTarget>();
            _field.GetAlive(alive);
            return alive[0].Handle;
        }

        private sealed class FakeIncome : IWalletIncome
        {
            public readonly List<int> Added = new List<int>();
            public void Add(int amount) => Added.Add(amount);
        }
    }
}
