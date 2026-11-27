using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Game.Core.Tests
{
    public class ArtTests
    {
        private const string Key = "Hero/Knight";
        private static readonly string SimpleId = Key + "/Simple";
        private static readonly string DetailId = Key + "/Detail";
        private readonly List<Object> created = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            foreach (var o in created)
            {
                if (o != null) Object.DestroyImmediate(o);
            }

            created.Clear();
        }

        [TestCase(true, true, false, ArtTier.Detail)]
        [TestCase(true, false, false, ArtTier.Detail)]
        [TestCase(false, true, false, ArtTier.Simple)]
        [TestCase(false, false, false, ArtTier.GreyBox)]
        public void Pick_ShowsDetailThenSimpleThenGreyBox(bool hasDetail, bool hasSimple, bool kept, ArtTier expected)
        {
            Assert.That(ArtTiers.Pick(hasDetail, hasSimple, kept), Is.EqualTo(expected));
        }

        [Test]
        public void Pick_KeyThePersonKeptOnSimple_ShowsSimpleEvenWithDetail()
        {
            Assert.That(ArtTiers.Pick(true, true, true), Is.EqualTo(ArtTier.Simple));
        }

        [Test]
        public void Pick_KeptOnSimpleButNoSimpleFile_ShowsDetail()
        {
            Assert.That(ArtTiers.Pick(true, false, true), Is.EqualTo(ArtTier.Detail));
            Assert.That(ArtTiers.Pick(false, false, true), Is.EqualTo(ArtTier.GreyBox));
        }

        [Test]
        public void ArtChoice_ReadsTheKeysKeptOnSimple()
        {
            var kept = ArtChoice.Parse("{\"simple\": [\"Hero/Knight\", \"Enemy/Slime\", \"\"]}");

            Assert.That(kept, Is.EquivalentTo(new[] { "Hero/Knight", "Enemy/Slime" }));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("{\"simple\": [\"Hero/Kn")]
        [TestCase("not json")]
        [TestCase("{}")]
        public void ArtChoice_MissingOrBrokenJson_KeepsNothing(string json)
        {
            Assert.That(ArtChoice.Parse(json), Is.Empty);
        }

        [Test]
        public void GreyBoxSprite_IsASquareOfTheLookColourAndSize()
        {
            var sprite = Keep(GreyBox.Sprite(new GreyBox.Look(Color.red, 2f)));

            Assert.That(sprite.bounds.size.x, Is.EqualTo(2f).Within(1e-4f));
            Assert.That(sprite.bounds.size.y, Is.EqualTo(2f).Within(1e-4f));
            Assert.That(sprite.texture.GetPixel(1, 1), Is.EqualTo(Color.red));
        }

        [Test]
        public void NoArtCatalog_EveryKeyShowsGreyBox()
        {
            var art = new Art(null);

            Assert.That(art.TierOf(Key), Is.EqualTo(ArtTier.GreyBox));
            Assert.That(Keep(art.Sprite(Key, new GreyBox.Look(Color.red, 1f))).name, Is.EqualTo("GreyBox"));
            Assert.That(Keep(art.Model(Key, Vector3.one)).name, Is.EqualTo("GreyBox"));
        }

        [Test]
        public void CatalogWithoutTheKeysIds_ShowsGreyBox()
        {
            var art = new Art(Catalog(("Enemy/Slime/Simple", Texture(4, 4))));

            Assert.That(art.TierOf(Key), Is.EqualTo(ArtTier.GreyBox));
            Assert.That(Keep(art.Sprite(Key, new GreyBox.Look(Color.red, 1f))).name, Is.EqualTo("GreyBox"));
        }

        [Test]
        public void Sprite_SimpleTexture_BecomesSpriteOfTheLookWidth()
        {
            var simple = Texture(8, 4);
            var art = new Art(Catalog((SimpleId, simple)));

            var sprite = Keep(art.Sprite(Key, new GreyBox.Look(Color.white, 1f)));

            Assert.That(sprite.texture, Is.SameAs(simple));
            Assert.That(sprite.bounds.size.x, Is.EqualTo(1f).Within(1e-4f));
            Assert.That(sprite.bounds.size.y, Is.EqualTo(0.5f).Within(1e-4f));
        }

        [Test]
        public void Sprite_DetailBesideSimple_ShowsDetail()
        {
            var detail = Texture(4, 4);
            var art = new Art(Catalog((SimpleId, Texture(4, 4)), (DetailId, detail)));

            Assert.That(art.TierOf(Key), Is.EqualTo(ArtTier.Detail));
            Assert.That(Keep(art.Sprite(Key, new GreyBox.Look(Color.white, 1f))).texture, Is.SameAs(detail));
        }

        [Test]
        public void KeptOnSimpleInArtChoice_ShowsSimpleOverDetail()
        {
            var simple = Texture(4, 4);
            var art = new Art(Catalog((Art.ChoiceId, Text("{\"simple\": [\"Hero/Knight\"]}")), (SimpleId, simple),
                (DetailId, Texture(4, 4))));

            Assert.That(art.TierOf(Key), Is.EqualTo(ArtTier.Simple));
            Assert.That(Keep(art.Sprite(Key, new GreyBox.Look(Color.white, 1f))).texture, Is.SameAs(simple));
        }

        [Test]
        public void KeptOnSimpleInArtChoice_WithoutSimpleFile_ShowsDetail()
        {
            var detail = Texture(4, 4);
            var art = new Art(Catalog((Art.ChoiceId, Text("{\"simple\": [\"Hero/Knight\"]}")), (DetailId, detail)));

            Assert.That(art.TierOf(Key), Is.EqualTo(ArtTier.Detail));
            Assert.That(Keep(art.Sprite(Key, new GreyBox.Look(Color.white, 1f))).texture, Is.SameAs(detail));
        }

        [Test]
        public void BrokenArtChoice_IsIgnoredAndDetailShows()
        {
            var art = new Art(Catalog((Art.ChoiceId, Text("{simple: [Hero/Knight")), (SimpleId, Texture(4, 4)),
                (DetailId, Texture(4, 4))));

            Assert.That(art.TierOf(Key), Is.EqualTo(ArtTier.Detail));
        }

        [Test]
        public void Sprite_SpriteFile_IsReturnedAsIs()
        {
            var file = Keep(Sprite.Create(Texture(4, 4), new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f));
            var art = new Art(Catalog((SimpleId, file)));

            Assert.That(art.Sprite(Key, new GreyBox.Look(Color.white, 1f)), Is.SameAs(file));
        }

        [Test]
        public void Model_NoFile_GivesGreyBoxCubeOfTheSize()
        {
            var model = Keep(new Art(null).Model(Key, new Vector3(1f, 2f, 3f)));

            Assert.That(model.name, Is.EqualTo("GreyBox"));
            Assert.That(model.transform.localScale, Is.EqualTo(new Vector3(1f, 2f, 3f)));
            Assert.That(model.GetComponent<MeshFilter>().sharedMesh.name, Does.Contain("Cube"));
        }

        [Test]
        public void Model_SimpleFile_GivesANewInstanceOfIt()
        {
            var prefab = Keep(new GameObject("Knight"));
            var art = new Art(Catalog((SimpleId, prefab)));

            var model = Keep(art.Model(Key, Vector3.one));

            Assert.That(model, Is.Not.SameAs(prefab));
            Assert.That(model.name, Is.EqualTo(Key));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("Knight")]
        [TestCase("/Knight")]
        [TestCase("Hero/")]
        [TestCase("Hero/Knight/Extra")]
        public void KeyNotOfTheFormFeatureSlashId_Fails(string key)
        {
            Assert.Throws<ArgumentException>(() => new Art(null).TierOf(key));
        }

        [Test]
        public void ArtInstaller_SceneWithoutArtCatalog_RegistersArtThatShowsGreyBox()
        {
            var installer = Keep(ScriptableObject.CreateInstance<ArtInstaller>());
            var registry = new ServiceRegistry();
            registry.Register<IAssetCatalogs>(new AssetCatalogs(new AssetCatalog[0]));

            Assert.DoesNotThrow(() => installer.Install(registry));
            Assert.That(registry.Resolve<IArt>().TierOf(Key), Is.EqualTo(ArtTier.GreyBox));
        }

        [Test]
        public void ArtInstaller_ReadsTheCatalogOfKindArt()
        {
            var installer = Keep(ScriptableObject.CreateInstance<ArtInstaller>());
            var registry = new ServiceRegistry();
            registry.Register<IAssetCatalogs>(new AssetCatalogs(new[] { Catalog((SimpleId, Texture(4, 4))) }));

            installer.Install(registry);

            Assert.That(registry.Resolve<IArt>().TierOf(Key), Is.EqualTo(ArtTier.Simple));
        }

        private AssetCatalog Catalog(params (string id, Object asset)[] entries)
        {
            var catalog = Keep(ScriptableObject.CreateInstance<AssetCatalog>());
            catalog.name = "Art";
            var so = new SerializedObject(catalog);
            so.FindProperty("kind").stringValue = Art.Kind;
            var list = so.FindProperty("entries");
            list.arraySize = entries.Length;
            for (var i = 0; i < entries.Length; i++)
            {
                var item = list.GetArrayElementAtIndex(i);
                item.FindPropertyRelative("id").stringValue = entries[i].id;
                item.FindPropertyRelative("asset").objectReferenceValue = entries[i].asset;
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            return catalog;
        }

        private Texture2D Texture(int width, int height) => Keep(new Texture2D(width, height));

        private TextAsset Text(string text) => Keep(new TextAsset(text));

        private T Keep<T>(T o) where T : Object
        {
            created.Add(o);
            if (o is Sprite sprite && sprite.texture != null && sprite.texture.name == "GreyBox") created.Add(sprite.texture);
            return o;
        }
    }
}
