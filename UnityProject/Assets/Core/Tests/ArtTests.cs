using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Game.Core.Tests
{
    public class ArtTests
    {
        private const string Key = "Hero/Knight";
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
        public void Pick_KeptOnSimpleButNoSimpleFile_ShowsGreyBox()
        {
            Assert.That(ArtTiers.Pick(true, false, true), Is.EqualTo(ArtTier.GreyBox));
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
        public void Sprite_NoFile_GivesGreyBoxSquareOfTheLook()
        {
            var sprite = Keep(Art.Sprite(Key, new GreyBox.Look(Color.red, 2f), new FakeFiles()));

            Assert.That(sprite.name, Is.EqualTo("GreyBox"));
            Assert.That(sprite.bounds.size.x, Is.EqualTo(2f).Within(1e-4f));
            Assert.That(sprite.bounds.size.y, Is.EqualTo(2f).Within(1e-4f));
            Assert.That(sprite.texture.GetPixel(1, 1), Is.EqualTo(Color.red));
        }

        [Test]
        public void Sprite_SimpleTexture_BecomesSpriteOfTheLookWidth()
        {
            var simple = Texture(8, 4);
            var files = new FakeFiles { [Art.PathOf(Key, ArtTier.Simple)] = simple };

            var sprite = Keep(Art.Sprite(Key, new GreyBox.Look(Color.white, 1f), files));

            Assert.That(sprite.texture, Is.SameAs(simple));
            Assert.That(sprite.bounds.size.x, Is.EqualTo(1f).Within(1e-4f));
            Assert.That(sprite.bounds.size.y, Is.EqualTo(0.5f).Within(1e-4f));
        }

        [Test]
        public void Sprite_DetailBesideSimple_ShowsDetail()
        {
            var detail = Texture(4, 4);
            var files = new FakeFiles
            {
                [Art.PathOf(Key, ArtTier.Simple)] = Texture(4, 4),
                [Art.PathOf(Key, ArtTier.Detail)] = detail,
            };

            Assert.That(Keep(Art.Sprite(Key, new GreyBox.Look(Color.white, 1f), files)).texture, Is.SameAs(detail));
        }

        [Test]
        public void Sprite_KeptOnSimpleInArtChoice_ShowsSimpleOverDetail()
        {
            var simple = Texture(4, 4);
            var files = new FakeFiles
            {
                [Art.ChoicePath] = Text("{\"simple\": [\"Hero/Knight\"]}"),
                [Art.PathOf(Key, ArtTier.Simple)] = simple,
                [Art.PathOf(Key, ArtTier.Detail)] = Texture(4, 4),
            };

            Assert.That(Keep(Art.Sprite(Key, new GreyBox.Look(Color.white, 1f), files)).texture, Is.SameAs(simple));
        }

        [Test]
        public void Sprite_BrokenArtChoice_IsIgnoredAndDetailShows()
        {
            var detail = Texture(4, 4);
            var files = new FakeFiles
            {
                [Art.ChoicePath] = Text("{simple: [Hero/Knight"),
                [Art.PathOf(Key, ArtTier.Simple)] = Texture(4, 4),
                [Art.PathOf(Key, ArtTier.Detail)] = detail,
            };

            Assert.That(Keep(Art.Sprite(Key, new GreyBox.Look(Color.white, 1f), files)).texture, Is.SameAs(detail));
        }

        [Test]
        public void Sprite_SpriteFile_IsReturnedAsIs()
        {
            var file = Keep(Sprite.Create(Texture(4, 4), new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f));
            var files = new FakeFiles { [Art.PathOf(Key, ArtTier.Simple)] = file };

            Assert.That(Art.Sprite(Key, new GreyBox.Look(Color.white, 1f), files), Is.SameAs(file));
        }

        [Test]
        public void Model_NoFile_GivesGreyBoxCubeOfTheSize()
        {
            var model = Keep(Art.Model(Key, new Vector3(1f, 2f, 3f), new FakeFiles()));

            Assert.That(model.name, Is.EqualTo("GreyBox"));
            Assert.That(model.transform.localScale, Is.EqualTo(new Vector3(1f, 2f, 3f)));
            Assert.That(model.GetComponent<MeshFilter>().sharedMesh.name, Does.Contain("Cube"));
        }

        [Test]
        public void Model_SimpleFile_GivesANewInstanceOfIt()
        {
            var prefab = Keep(new GameObject("Knight"));
            var files = new FakeFiles { [Art.PathOf(Key, ArtTier.Simple)] = prefab };

            var model = Keep(Art.Model(Key, Vector3.one, files));

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
            Assert.Throws<System.ArgumentException>(() => Art.TierOf(key, new FakeFiles()));
        }

        private Texture2D Texture(int width, int height) => Keep(new Texture2D(width, height));

        private TextAsset Text(string text) => Keep(new TextAsset(text));

        private T Keep<T>(T o) where T : Object
        {
            created.Add(o);
            if (o is Sprite sprite && sprite.texture != null && sprite.texture.name == "GreyBox") created.Add(sprite.texture);
            return o;
        }

        private sealed class FakeFiles : Dictionary<string, Object>, IArtFiles
        {
            public Object Load(string path) => TryGetValue(path, out var file) ? file : null;
        }
    }
}
