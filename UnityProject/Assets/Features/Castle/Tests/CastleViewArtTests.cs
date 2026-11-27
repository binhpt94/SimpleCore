using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Game.Castle.Tests
{
    /// <summary>The castle, its shots and the Thunder effect take their pictures from Core's art.</summary>
    public class CastleViewArtTests
    {
        private readonly List<Object> _made = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            foreach (var made in _made)
                if (made) Object.DestroyImmediate(made);
            _made.Clear();
        }

        private Sprite MakeSprite(string spriteName)
        {
            var texture = new Texture2D(8, 8);
            var sprite = Sprite.Create(texture, new Rect(0f, 0f, 8f, 8f), new Vector2(0.5f, 0.5f), 8f);
            sprite.name = spriteName;
            _made.Add(texture);
            _made.Add(sprite);
            return sprite;
        }

        private const string PrefabPath = "Assets/Features/Castle/FeatureAssets/Private/CastleView.prefab";

        private ICastleView MakeView(FakeArt art, out GameObject viewObject)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            Assert.That(prefab, Is.Not.Null, $"Missing {PrefabPath}");
            viewObject = Object.Instantiate(prefab);
            _made.Add(viewObject);
            var view = viewObject.GetComponent<ICastleView>();
            view.UseArt(art);
            return view;
        }

        private static SpriteRenderer Drawn(GameObject viewObject, string part)
            => viewObject.GetComponentsInChildren<SpriteRenderer>().Single(r => r.gameObject.name == part);

        [Test]
        public void Castle_shows_the_simple_art_of_the_catalog_for_its_body_shot_and_thunder()
        {
            var art = new FakeArt();
            var body = MakeSprite("body");
            var shot = MakeSprite("shot");
            var thunder = MakeSprite("thunder");
            art.Simple["Castle/body"] = body;
            art.Simple["Castle/shot"] = shot;
            art.Simple["Castle/thunder"] = thunder;

            var view = MakeView(art, out var viewObject);
            view.ShowShot(1, 1.5f, 4f, 0.15f);
            view.ShowThunder(3f, 0.3f);

            Assert.That(Drawn(viewObject, "Body").sprite, Is.SameAs(body));
            Assert.That(Drawn(viewObject, "Shot").sprite, Is.SameAs(shot));
            Assert.That(Drawn(viewObject, "Thunder").sprite, Is.SameAs(thunder));
        }

        [Test]
        public void Without_a_catalog_the_castle_is_drawn_as_grey_boxes_and_still_shoots_and_casts_thunder()
        {
            var art = new FakeArt();

            var view = MakeView(art, out var viewObject);
            view.Place(1.5f, 4f);
            view.ShowShot(1, 1.5f, 4f, 0.15f);
            view.ShowThunder(3f, 0.3f);
            view.Advance(0.05f);

            Assert.That(art.Asked, Is.EquivalentTo(new[] { "Castle/body", "Castle/shot", "Castle/thunder" }));
            Assert.That(Drawn(viewObject, "Body").sprite.name, Is.EqualTo("GreyBox"));
            Assert.That(Drawn(viewObject, "Shot").sprite.name, Is.EqualTo("GreyBox"));
            Assert.That(Drawn(viewObject, "Thunder").sprite.name, Is.EqualTo("GreyBox"));
            var bodyBounds = Drawn(viewObject, "Body").bounds;
            Assert.That(bodyBounds.size.x, Is.EqualTo(3f).Within(1e-3f), "GreyBox castle is 3u wide");
            Assert.That(bodyBounds.size.y, Is.EqualTo(4f).Within(1e-3f), "and 4u high");
            Assert.That(bodyBounds.min.y, Is.EqualTo(0f).Within(1e-3f), "standing on the ground");
        }
    }
}
