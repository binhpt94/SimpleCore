using System.Collections.Generic;
using Game.Core;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Game.Enemy.Tests
{
    public sealed class EnemyStageArtTests
    {
        private const string InstallerPath = "Assets/Features/Enemy/FeatureAssets/Public/EnemyInstaller.asset";
        private readonly List<Object> _made = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            foreach (var made in _made)
            {
                if (made != null) Object.DestroyImmediate(made);
            }

            _made.Clear();
        }

        [TestCase(EnemyType.Soldier, "Enemy/soldier")]
        [TestCase(EnemyType.Wolf, "Enemy/wolf")]
        [TestCase(EnemyType.Giant, "Enemy/giant")]
        [TestCase(EnemyType.Archer, "Enemy/archer")]
        [TestCase(EnemyType.Boss, "Enemy/boss")]
        public void An_enemy_shows_the_Simple_sprite_of_its_type_from_the_catalog(EnemyType type, string key)
        {
            var art = new EnemyFakeArt();
            var simple = MakeSprite();
            art.Simple[key] = simple;
            var stage = OpenStage();
            stage.UseArt(art);

            stage.Show(new EnemyLook(1, type, 2f, 0.8f, 1.2f, 1, true, false));

            var renderer = RendererOf(stage, 1);
            Assert.That(renderer.sprite, Is.SameAs(simple));
            Assert.That(art.Asked, Does.Contain(key));
            Assert.That(renderer.bounds.size.x, Is.EqualTo(0.8f).Within(1e-3f), "width from the design box");
            Assert.That(renderer.bounds.size.y, Is.EqualTo(1.2f).Within(1e-3f), "height from the design box");
            Assert.That(renderer.bounds.min.y, Is.EqualTo(0f).Within(1e-3f), "feet on the ground");
        }

        [TestCase(EnemyType.Soldier)]
        [TestCase(EnemyType.Boss)]
        public void Without_a_catalog_file_an_enemy_is_a_GreyBox_in_its_box_colour_without_errors(EnemyType type)
        {
            var art = new EnemyFakeArt();
            var stage = OpenStage();
            stage.UseArt(art);

            Assert.DoesNotThrow(() => stage.Show(new EnemyLook(1, type, -3f, 0.8f, 1.2f, -1, true, false)));

            var renderer = RendererOf(stage, 1);
            Assert.That(renderer.sprite, Is.Not.Null);
            Assert.That(renderer.sprite.name, Is.EqualTo("GreyBox"));
            Assert.That(art.Looks.Count, Is.EqualTo(1));
            Assert.That(art.Looks[0].Color.a, Is.GreaterThan(0f), "the type's box colour");
            Assert.That(renderer.bounds.size.y, Is.EqualTo(1.2f).Within(1e-3f));
            Assert.That(renderer.bounds.min.y, Is.EqualTo(0f).Within(1e-3f), "feet on the ground");
            Assert.DoesNotThrow(() => stage.Close());
        }

        private IEnemyStage OpenStage()
        {
            var installer = AssetDatabase.LoadAssetAtPath<EnemyInstaller>(InstallerPath);
            var prefab = new SerializedObject(installer).FindProperty("_stagePrefab").objectReferenceValue as GameObject;
            Assert.That(prefab, Is.Not.Null, "EnemyInstaller.asset has no stage prefab assigned");
            var go = Object.Instantiate(prefab);
            _made.Add(go);
            return go.GetComponent<IEnemyStage>();
        }

        private static SpriteRenderer RendererOf(IEnemyStage stage, int handle)
        {
            var root = ((Component)stage).transform;
            for (var i = 0; i < root.childCount; i++)
            {
                var child = root.GetChild(i);
                if (child.name.EndsWith("#" + handle)) return child.GetComponentInChildren<SpriteRenderer>();
            }

            Assert.Fail($"enemy #{handle} is not drawn");
            return null;
        }

        private Sprite MakeSprite()
        {
            var texture = new Texture2D(8, 16);
            _made.Add(texture);
            var sprite = Sprite.Create(texture, new Rect(0, 0, 8, 16), new Vector2(0.5f, 0f), 8f);
            _made.Add(sprite);
            return sprite;
        }
    }

    /// <summary>Art catalog stand-in: keys in <see cref="Simple"/> have a file, others are GreyBox.</summary>
    internal sealed class EnemyFakeArt : IArt
    {
        public readonly Dictionary<string, Sprite> Simple = new Dictionary<string, Sprite>();
        public readonly List<string> Asked = new List<string>();
        public readonly List<GreyBox.Look> Looks = new List<GreyBox.Look>();

        public Sprite Sprite(string key, GreyBox.Look look)
        {
            Asked.Add(key);
            if (Simple.TryGetValue(key, out var sprite)) return sprite;
            Looks.Add(look);
            return GreyBox.Sprite(look);
        }

        public GameObject Model(string key, Vector3 size) => GreyBox.Model(size);

        public ArtTier TierOf(string key) => Simple.ContainsKey(key) ? ArtTier.Simple : ArtTier.GreyBox;
    }
}
