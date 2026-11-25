using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Game.Hud.Tests
{
    /// <summary>
    /// Layout of the real HUD prefab (feature-hud/hud-layout/layout-spec, feature-hud/hud-status/status-spec):
    /// each part's box on the reference screen, worked out from its anchors so it holds for any width from 16:9
    /// to 21:9 (the canvas scales by height, so the screen is always 1080 units tall).
    /// </summary>
    public sealed class HudLayoutTests
    {
        private const string PrefabPath = "Assets/Features/Hud/FeatureAssets/Private/HudView.prefab";
        private const float Height = 1080f;
        private const float Wide16By9 = 1920f;
        private const float Wide21By9 = 2520f;
        private const float StripTop = 140f;
        private const float StripBottom = 900f;

        private static readonly string[] StatusParts = { "Band", "Coin", "Coins", "Health", "Wave", "Banner" };

        private GameObject _instance;
        private IHudView _view;
        private RectTransform _root;

        [SetUp]
        public void SetUp()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            Assert.IsNotNull(prefab, "missing " + PrefabPath);
            _instance = Object.Instantiate(prefab);
            _view = _instance.GetComponent<IHudView>();
            Assert.IsNotNull(_view, "prefab root has no IHudView");
            _root = (RectTransform)_instance.transform;

            _view.ShowHealth("1000/1000", 1f);
            _view.ShowCoins("50");
            _view.ShowWave("Đợt 1");
            _view.ShowBanner("Đợt 1");
            _view.CreateSkillButtons(new[] { "Sấm sét", "Băng giá", "Cổ vũ" }, new[] { 0, 1, 2 });
        }

        [TearDown]
        public void TearDown()
        {
            if (_instance != null) Object.DestroyImmediate(_instance);
        }

        [TestCase(Wide16By9)]
        [TestCase(Wide21By9)]
        public void TopRowGoesMoneyThenHealthThenWaveFromLeftToRight(float width)
        {
            var coin = Box("Coin", width);
            var coins = Box("Coins", width);
            var health = Box("Health", width);
            var wave = Box("Wave", width);

            Assert.Less(coin.xMax, coins.xMin + 0.5f);
            Assert.Less(coins.xMax, health.xMin);
            Assert.Less(health.xMax, wave.xMin);
            Assert.AreEqual(width / 2f, health.center.x, 0.5f, "health bar sits in the middle, above the castle");
        }

        [TestCase(Wide16By9)]
        [TestCase(Wide21By9)]
        public void SkillButtonsSitSideBySideInTheBottomRightCornerThunderFrostRally(float width)
        {
            var thunder = Box("Skill Sấm sét", width);
            var frost = Box("Skill Băng giá", width);
            var rally = Box("Skill Cổ vũ", width);

            Assert.Less(thunder.xMax, frost.xMin);
            Assert.Less(frost.xMax, rally.xMin);
            Assert.AreEqual(width - 40f, rally.xMax, 0.5f);
            Assert.AreEqual(Height - 20f, rally.yMax, 0.5f);
            Assert.AreEqual(thunder.yMin, rally.yMin, 0.5f);
            Assert.AreEqual(thunder.yMin, frost.yMin, 0.5f);
        }

        [TestCase(Wide16By9)]
        [TestCase(Wide21By9)]
        public void BottomEdgeLeftOfTheSkillClusterIsLeftFreeForTheUpgradeBar(float width)
        {
            var clusterLeft = Box("Skill Sấm sét", width).xMin;
            foreach (var part in Parts(width))
            {
                if (part.Value.yMax <= StripBottom) continue;
                Assert.GreaterOrEqual(part.Value.xMin, clusterLeft - 0.5f, part.Key + " sits where the upgrade bar goes");
            }
        }

        [TestCase(Wide16By9)]
        [TestCase(Wide21By9)]
        public void NoHudPartCoversTheBattleStrip(float width)
        {
            foreach (var part in Parts(width))
            {
                var box = part.Value;
                var clear = box.yMax <= StripTop + 0.5f || box.yMin >= StripBottom - 0.5f;
                Assert.IsTrue(clear, part.Key + " overlaps the battle strip: y " + box.yMin + ".." + box.yMax);
            }
        }

        [TestCase(Wide16By9)]
        [TestCase(Wide21By9)]
        public void EveryHudPartStaysOnScreen(float width)
        {
            foreach (var part in Parts(width))
            {
                var box = part.Value;
                Assert.GreaterOrEqual(box.xMin, -0.5f, part.Key);
                Assert.LessOrEqual(box.xMax, width + 0.5f, part.Key);
                Assert.GreaterOrEqual(box.yMin, -0.5f, part.Key);
                Assert.LessOrEqual(box.yMax, Height + 0.5f, part.Key);
            }
        }

        [Test]
        public void WaveBannerSitsInTheTopBandUnderTheHealthBar()
        {
            var banner = Box("Banner", Wide16By9);
            var health = Box("Health", Wide16By9);

            Assert.IsTrue(_root.Find("Banner").gameObject.activeSelf);
            Assert.GreaterOrEqual(banner.yMin, health.yMax);
            Assert.LessOrEqual(banner.yMax, StripTop);
            Assert.AreEqual(health.center.x, banner.center.x, 0.5f);
        }

        [Test]
        public void StatusBoxesAndBannerTakeNoTaps()
        {
            foreach (var name in StatusParts)
            {
                var part = _root.Find(name);
                Assert.IsNotNull(part, "missing " + name);
                foreach (var component in part.GetComponentsInChildren<Component>(true))
                {
                    Assert.IsFalse(IsSelectable(component), name + " has a pressable " + component.GetType().Name);
                    var raycast = component.GetType().GetProperty("raycastTarget");
                    if (raycast != null)
                        Assert.IsFalse((bool)raycast.GetValue(component), name + "/" + component.name + " catches taps");
                }
            }
        }

        [Test]
        public void MoneyShowsTheCoinIconRightBeforeTheNumber()
        {
            var coin = _root.Find("Coin");
            Assert.IsNotNull(coin);
            Assert.IsNotNull(SpriteOf(coin.gameObject), "coin icon has no sprite");

            var icon = Box("Coin", Wide16By9);
            var number = Box("Coins", Wide16By9);
            Assert.AreEqual(60f, icon.width, 0.5f);
            Assert.AreEqual(60f, icon.height, 0.5f);
            Assert.AreEqual(icon.xMax, number.xMin, 15f);
        }

        [Test]
        public void HudCanvasSitsAboveTheGameButBelowPopups()
        {
            var canvas = _instance.GetComponent<Canvas>();
            Assert.IsNotNull(canvas);
            Assert.AreEqual(RenderMode.ScreenSpaceOverlay, canvas.renderMode);
            Assert.AreEqual(10, canvas.sortingOrder, "popups must use a higher sorting order to cover the HUD");
        }

        /// <summary>Box of a direct child of the HUD root, y measured downward from the top of the screen.</summary>
        private Rect Box(string name, float width)
        {
            var child = _root.Find(name);
            Assert.IsNotNull(child, "missing HUD part " + name);
            return BoxOf((RectTransform)child, width);
        }

        private Dictionary<string, Rect> Parts(float width)
        {
            var parts = new Dictionary<string, Rect>();
            foreach (Transform child in _root)
            {
                if (!child.gameObject.activeSelf || child.name == "Cheat") continue;
                parts[child.name] = BoxOf((RectTransform)child, width);
            }

            Assert.IsNotEmpty(parts);
            return parts;
        }

        private static Rect BoxOf(RectTransform rect, float width)
        {
            var left = rect.anchorMin.x * width + rect.offsetMin.x;
            var right = rect.anchorMax.x * width + rect.offsetMax.x;
            var bottomUp = rect.anchorMin.y * Height + rect.offsetMin.y;
            var topUp = rect.anchorMax.y * Height + rect.offsetMax.y;
            return Rect.MinMaxRect(left, Height - topUp, right, Height - bottomUp);
        }

        private static bool IsSelectable(Component component)
        {
            for (var type = component.GetType(); type != null; type = type.BaseType)
                if (type.Name == "Selectable") return true;
            return false;
        }

        private static Object SpriteOf(GameObject go)
        {
            foreach (var component in go.GetComponents<Component>())
            {
                var sprite = component.GetType().GetProperty("sprite");
                if (sprite != null && sprite.PropertyType == typeof(Sprite)) return (Object)sprite.GetValue(component);
            }

            return null;
        }
    }
}
