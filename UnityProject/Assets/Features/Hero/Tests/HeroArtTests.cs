using System.Collections.Generic;
using Game.Core;
using Game.Run;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Game.Hero.Tests
{
    /// <summary>Heroes take their battlefield sprite from Core's art, key Hero/&lt;code&gt;, GreyBox when there is no file.</summary>
    public sealed class HeroArtTests
    {
        private const string ArtCatalogPath = "Assets/_Composition/Game.Art.asset";

        private readonly List<Object> _made = new List<Object>();
        private HeroSquad _squad;
        private HeroFakes.DisplayFactory _displays;
        private HeroFakes.Art _art;
        private HeroPresenter _presenter;

        [SetUp]
        public void SetUp()
        {
            _squad = new HeroSquad(HeroFakes.LoadConfig(), new HeroFakes.EnemyField());
            _displays = new HeroFakes.DisplayFactory();
            _art = new HeroFakes.Art();
            _presenter = new HeroPresenter(_squad, HeroFakes.LoadConfig(), _displays, _art);
        }

        [TearDown]
        public void TearDown()
        {
            _presenter.Dispose();
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

        [Test]
        public void With_simple_art_in_the_catalog_each_hero_shows_its_own_sprite()
        {
            var knight = MakeSprite("knight");
            var archer = MakeSprite("archer");
            var mage = MakeSprite("mage");
            _art.Simple["Hero/knight"] = knight;
            _art.Simple["Hero/archer"] = archer;
            _art.Simple["Hero/mage"] = mage;

            _presenter.Attach();
            _squad.Begin(new RunTeam(new[] { "knight", "archer", "mage" }));

            Assert.That(_displays.Made[0].Sprite, Is.SameAs(knight));
            Assert.That(_displays.Made[1].Sprite, Is.SameAs(archer));
            Assert.That(_displays.Made[2].Sprite, Is.SameAs(mage));
        }

        [Test]
        public void Without_a_catalog_heroes_are_grey_boxes_in_their_placeholder_colour_without_errors()
        {
            var config = HeroFakes.LoadConfig();

            _presenter.Attach();
            _squad.Begin(new RunTeam(new[] { "knight", "axeman", "swordsman" }));

            Assert.That(_art.Asked, Is.EqualTo(new[] { "Hero/knight", "Hero/axeman", "Hero/swordsman" }));
            for (var i = 0; i < 3; i++)
            {
                var display = _displays.Made[i];
                Assert.That(display.Sprite, Is.Not.Null);
                Assert.That(display.Sprite.name, Is.EqualTo("GreyBox"));
                _made.Add(display.Sprite.texture);
                _made.Add(display.Sprite);
                Assert.That(_art.Looks[i].Color, Is.EqualTo(config.Find(display.HeroCode).placeholderColor));
            }
        }

        [Test]
        public void The_game_art_catalog_has_a_simple_sprite_for_every_hero()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<AssetCatalog>(ArtCatalogPath);
            Assert.That(catalog, Is.Not.Null, $"Missing {ArtCatalogPath}");
            foreach (var code in new[] { "knight", "axeman", "swordsman", "archer", "mage" })
            {
                Assert.That(catalog.TryGet($"Hero/{code}/Simple", out var asset), Is.True, code);
                Assert.That(asset, Is.InstanceOf<Sprite>(), code);
            }
        }
    }
}
