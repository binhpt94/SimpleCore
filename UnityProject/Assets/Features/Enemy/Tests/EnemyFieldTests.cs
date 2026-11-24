using System.Collections.Generic;
using Game.Wallet;
using NUnit.Framework;
using UnityEditor;

namespace Game.Enemy.Tests
{
    public sealed class EnemyFieldTests
    {
        private const string ConfigPath = "Assets/Features/Enemy/FeatureAssets/Private/EnemyConfig.asset";

        private FakeIncome _income;
        private EnemyField _field;
        private List<EnemyDefeat> _defeats;

        [SetUp]
        public void SetUp()
        {
            var config = AssetDatabase.LoadAssetAtPath<EnemyConfig>(ConfigPath);
            Assert.That(config, Is.Not.Null, $"Missing {ConfigPath}");
            _income = new FakeIncome();
            _field = new EnemyField(config, _income);
            _defeats = new List<EnemyDefeat>();
            _field.Defeated += d => _defeats.Add(d);
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
