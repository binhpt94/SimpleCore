using System;
using System.Collections.Generic;
using Game.Wallet;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Game.Enemy.Tests
{
    /// <summary>
    /// The IDEA column of "Thông số theo giai đoạn" of enemy-type-values, enemy-stat-values, reward-values and
    /// enemy-move-values. The real config holds the PROTOTYPE column (all five types, checked in
    /// EnemyInstallerTests); here a copy of it is set to IDEA: only Soldier, Wolf and Giant are in the game, and there
    /// is no Frost (nobody calls SlowAll).
    /// </summary>
    public sealed class EnemyIdeaStageTests
    {
        private const string ConfigPath = "Assets/Features/Enemy/FeatureAssets/Private/EnemyConfig.asset";
        private const string InstallerPath = "Assets/Features/Enemy/FeatureAssets/Public/EnemyInstaller.asset";
        private const float Step = 0.025f;

        private EnemyConfig _config;
        private FakeIncome _income;
        private EnemyField _field;
        private FakeFoes _castle;
        private List<EnemyDefeat> _defeats;

        [SetUp]
        public void SetUp()
        {
            _config = IdeaConfig();
            _income = new FakeIncome();
            _field = new EnemyField(_config, _income);
            _castle = new FakeFoes();
            _castle.Foes.Add(new EnemyFoe(1, 0f, 1.5f));
            _field.AddFoes(_castle);
            _defeats = new List<EnemyDefeat>();
            _field.Defeated += d => _defeats.Add(d);
        }

        [TearDown]
        public void TearDown()
        {
            if (_config != null) UnityEngine.Object.DestroyImmediate(_config);
            foreach (var made in _made) if (made != null) UnityEngine.Object.DestroyImmediate(made);
            _made.Clear();
        }

        // enemy-type-values IDEA: "Loại có trong game: Lính, Sói, Khổng lồ".
        [TestCase(EnemyType.Soldier)]
        [TestCase(EnemyType.Wolf)]
        [TestCase(EnemyType.Giant)]
        public void At_IDEA_soldier_wolf_and_giant_are_in_the_game_and_can_be_spawned(EnemyType type)
        {
            _field.Spawn(new EnemySpawn(type, EnemySide.Right, 1f));

            Assert.That(_field.AliveCount, Is.EqualTo(1));
        }

        [TestCase(EnemyType.Archer)]
        [TestCase(EnemyType.Boss)]
        public void At_IDEA_asking_for_an_archer_or_a_boss_fails_naming_it_and_spawns_nothing(EnemyType type)
        {
            var error = Assert.Throws<InvalidOperationException>(
                () => _field.Spawn(new EnemySpawn(type, EnemySide.Left, 1f)));

            StringAssert.Contains(type.ToString(), error.Message);
            Assert.That(_field.AliveCount, Is.Zero);
        }

        // enemy-stat-values IDEA: "Loại có chỉ số: Lính, Sói, Khổng lồ" (health / damage / seconds per hit /
        // range / walk speed: 40/5/1.0/1.0/1.5, 20/3/0.8/1.0/3.0, 150/15/1.5/1.2/0.8).
        [TestCase(EnemyType.Soldier, 40f, 5f, 1.0f, 1.0f, 1.5f)]
        [TestCase(EnemyType.Wolf, 20f, 3f, 0.8f, 1.0f, 3.0f)]
        [TestCase(EnemyType.Giant, 150f, 15f, 1.5f, 1.2f, 0.8f)]
        public void At_IDEA_each_type_in_the_game_has_its_base_stats(EnemyType type, float health, float damage,
            float secondsPerHit, float range, float moveSpeed)
        {
            var kind = _config.Kind(type);

            Assert.That(kind.Health, Is.EqualTo(health).Within(1e-4f), "health");
            Assert.That(kind.Damage, Is.EqualTo(damage).Within(1e-4f), "damage");
            Assert.That(kind.SecondsPerHit, Is.EqualTo(secondsPerHit).Within(1e-4f), "seconds per hit");
            Assert.That(kind.Range, Is.EqualTo(range).Within(1e-4f), "range");
            Assert.That(kind.MoveSpeed, Is.EqualTo(moveSpeed).Within(1e-4f), "walk speed");
        }

        [TestCase(EnemyType.Soldier, 40f)]
        [TestCase(EnemyType.Wolf, 20f)]
        [TestCase(EnemyType.Giant, 150f)]
        public void At_IDEA_an_enemy_spawns_with_full_base_health(EnemyType type, float health)
        {
            _field.Spawn(new EnemySpawn(type, EnemySide.Left, 1f));
            var handle = FirstHandle();

            Assert.That(_field.Damage(handle, health - 0.5f), Is.False, "still alive just under its health");
            Assert.That(_field.Damage(handle, 0.5f), Is.True, "defeated when the rest is gone");
        }

        // enemy-stat-values IDEA: "Hệ số đợt: 1 + 0.10×(đợt-1)", given by feature-wave; health and damage × scale.
        [TestCase(1, 40f, 5f)]
        [TestCase(5, 56f, 7f)]
        [TestCase(10, 76f, 9.5f)]
        public void At_IDEA_health_and_damage_grow_by_the_wave_scale(int wave, float health, float damage)
        {
            var scale = 1f + 0.10f * (wave - 1);
            _field.Spawn(new EnemySpawn(EnemyType.Soldier, EnemySide.Right, scale));
            var handle = FirstHandle();

            Run(10.5f);

            Assert.That(_castle.Hits.Count, Is.GreaterThan(0), "the soldier reached the castle");
            Assert.That(_castle.Hits.TrueForAll(h => Math.Abs(h.Damage - damage) < 1e-3f), Is.True,
                $"every hit deals {damage}");
            Assert.That(_field.Damage(handle, health - 0.01f), Is.False, $"max health {health}");
            // Float scale (1 + 0.10f × 9 is not exactly 1.9): max health is within 0.01 of the design number.
            Assert.That(_field.Damage(handle, 0.02f), Is.True, $"max health {health}");
        }

        // enemy-stat-values IDEA: "Bị hạ: thu nhỏ 0.3s"; at once no longer alive or targeted.
        [Test]
        public void At_IDEA_a_defeated_enemy_leaves_the_field_at_once_and_shrinks_away_in_0_3s()
        {
            _field.Spawn(new EnemySpawn(EnemyType.Giant, EnemySide.Left, 1f));

            _field.Damage(FirstHandle(), 1000f);

            var alive = new List<EnemyTarget>();
            _field.GetAlive(alive);
            Assert.That(alive, Is.Empty, "cannot be targeted");
            Assert.That(_defeats.Count, Is.EqualTo(1));
            Assert.That(StageFloat("_defeatSeconds"), Is.EqualTo(0.3f).Within(1e-4f));
        }

        // reward-values IDEA: "Loại có tiền: Lính 5, Sói 4, Khổng lồ 15".
        [TestCase(EnemyType.Soldier, 5)]
        [TestCase(EnemyType.Wolf, 4)]
        [TestCase(EnemyType.Giant, 15)]
        public void At_IDEA_defeating_an_enemy_pays_the_coins_of_its_type(EnemyType type, int coins)
        {
            _field.Spawn(new EnemySpawn(type, EnemySide.Right, 1.4f));

            _field.Damage(FirstHandle(), 100000f);

            Assert.That(_income.Added, Is.EqualTo(new[] { coins }), "once, not scaled by the wave");
        }

        // enemy-move-values IDEA: "Luật đi, dừng, đánh: như bảng"; "Chậm do Băng giá: không có"; "Dấu chậm: không có".
        [TestCase(EnemyType.Soldier, 3.0f)]
        [TestCase(EnemyType.Wolf, 3.1f)]
        [TestCase(EnemyType.Giant, 3.7f)]
        public void At_IDEA_without_Frost_an_enemy_walks_at_its_own_speed_to_its_stop_unslowed(EnemyType type,
            float stop)
        {
            _field.Spawn(new EnemySpawn(type, EnemySide.Right, 1f));
            var speed = _config.Kind(type).MoveSpeed;
            var looks = new List<EnemyLook>();

            Run(2f);
            Assert.That(FirstX(), Is.EqualTo(17f - 2f * speed).Within(0.05f), "full speed, never halved");
            _field.GetLooks(looks);
            Assert.That(looks[0].Slowed, Is.False, "no slow mark");

            Run(25f);
            Assert.That(FirstX(), Is.EqualTo(stop).Within(0.05f), "stops at its range from the castle edge");
            _field.GetLooks(looks);
            Assert.That(looks[0].Slowed, Is.False, "no slow mark");
        }

        // enemy-move-values IDEA: "Chuyển động: nhún khi đi, nhích khi đánh, nháy khi bị đánh".
        [TestCase("_bobHeight", 0.1f)]
        [TestCase("_bobPeriod", 0.4f)]
        [TestCase("_lungeDistance", 0.2f)]
        [TestCase("_lungeSeconds", 0.15f)]
        [TestCase("_flashSeconds", 0.1f)]
        public void At_IDEA_enemies_bob_lunge_and_flash_with_the_design_numbers(string field, float expected)
        {
            Assert.That(StageFloat(field), Is.EqualTo(expected).Within(1e-4f));
        }

        // enemy-type-values IDEA: "Hình: 1 sprite mỗi loại, anim bằng move/scale/nháy".
        [TestCase(EnemyType.Soldier)]
        [TestCase(EnemyType.Wolf)]
        [TestCase(EnemyType.Giant)]
        public void At_IDEA_each_type_in_the_game_is_drawn_with_its_own_sprite(EnemyType type)
        {

            var art = new EnemyFakeArt();
            var sprites = new Dictionary<EnemyType, Sprite>();
            foreach (var t in new[] { EnemyType.Soldier, EnemyType.Wolf, EnemyType.Giant })
            {
                var sprite = MakeSprite();
                sprites[t] = sprite;
                art.Simple["Enemy/" + t.ToString().ToLowerInvariant()] = sprite;
            }

            var go = UnityEngine.Object.Instantiate(Stage().gameObject);
            _made.Add(go);
            var stage = go.GetComponent<IEnemyStage>();
            stage.UseArt(art);
            stage.Show(new EnemyLook(1, type, 2f, 0.8f, 1.2f, 1, true, false));

            SpriteRenderer renderer = null;
            for (var i = 0; i < go.transform.childCount; i++)
            {
                var child = go.transform.GetChild(i);
                if (child.name.EndsWith("#1")) renderer = child.GetComponentInChildren<SpriteRenderer>();
            }

            Assert.That(renderer, Is.Not.Null, $"{type} is drawn");
            Assert.That(renderer.sprite, Is.SameAs(sprites[type]), $"{type} gets its own Simple sprite");
            foreach (var other in sprites)
            {
                if (other.Key != type) Assert.That(renderer.sprite, Is.Not.SameAs(other.Value), $"not shared with {other.Key}");
            }
        }

        private readonly List<UnityEngine.Object> _made = new List<UnityEngine.Object>();

        private Sprite MakeSprite()
        {
            var texture = new Texture2D(8, 16);
            _made.Add(texture);
            var sprite = Sprite.Create(texture, new Rect(0, 0, 8, 16), new Vector2(0.5f, 0f), 8f);
            _made.Add(sprite);
            return sprite;
        }

        /// <summary>A copy of the real config set to the IDEA column: Archer and Boss are not in the game.</summary>
        private static EnemyConfig IdeaConfig()
        {
            var real = AssetDatabase.LoadAssetAtPath<EnemyConfig>(ConfigPath);
            Assert.That(real, Is.Not.Null, $"Missing {ConfigPath}");
            var copy = UnityEngine.Object.Instantiate(real);
            var so = new SerializedObject(copy);
            var kinds = so.FindProperty("_kinds");
            for (var i = 0; i < kinds.arraySize; i++)
            {
                var kind = kinds.GetArrayElementAtIndex(i);
                var type = (EnemyType)kind.FindPropertyRelative("_type").intValue;
                kind.FindPropertyRelative("_available").boolValue =
                    type == EnemyType.Soldier || type == EnemyType.Wolf || type == EnemyType.Giant;
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            return copy;
        }

        private static Component Stage()
        {
            var installer = AssetDatabase.LoadAssetAtPath<EnemyInstaller>(InstallerPath);
            Assert.That(installer, Is.Not.Null, $"Missing {InstallerPath}");
            var prefab = new SerializedObject(installer).FindProperty("_stagePrefab").objectReferenceValue as GameObject;
            Assert.That(prefab, Is.Not.Null, "EnemyInstaller.asset has no stage prefab assigned");
            var stage = prefab.GetComponent<IEnemyStage>() as Component;
            Assert.That(stage, Is.Not.Null, "stage prefab has no IEnemyStage component");
            return stage;
        }

        private static float StageFloat(string field)
        {
            var property = new SerializedObject(Stage()).FindProperty(field);
            Assert.That(property, Is.Not.Null, $"stage has no field {field}");
            return property.floatValue;
        }

        private void Run(float seconds)
        {
            var steps = (int)Math.Round(seconds / Step);
            for (var i = 0; i < steps; i++) _field.Tick(Step);
        }

        private int FirstHandle()
        {
            var alive = new List<EnemyTarget>();
            _field.GetAlive(alive);
            return alive[0].Handle;
        }

        private float FirstX()
        {
            var alive = new List<EnemyTarget>();
            _field.GetAlive(alive);
            return alive[0].X;
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
    }
}
