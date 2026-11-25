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
        private RectTransform _canvasRoot;
        private RectTransform _root;

        [SetUp]
        public void SetUp()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            Assert.IsNotNull(prefab, "missing " + PrefabPath);
            _instance = Object.Instantiate(prefab);
            _view = _instance.GetComponent<IHudView>();
            Assert.IsNotNull(_view, "prefab root has no IHudView");
            _canvasRoot = (RectTransform)_instance.transform;

            _view.ShowHealth("1000/1000", 1f);
            _view.ShowCoins("50");
            _view.ShowWave("Đợt 1");
            _view.ShowBanner("Đợt 1");
            _view.CreateSkillButtons(new[] { "Sấm sét", "Băng giá", "Cổ vũ" }, new[] { 0, 1, 2 });
            _view.ShowCheatButton("Cheat: mid-battle");

            // Awake does not run in EditMode: the view builds itself on the first call above.
            _root = (RectTransform)_canvasRoot.Find("Safe");
            Assert.IsNotNull(_root, "HUD parts must sit in the safe-area box 'Safe'");
        }

        [TestCase(Wide16By9)]
        [TestCase(Wide21By9)]
        public void DevCheatButtonSitsInTheTopBandBetweenHealthAndWave(float width)
        {
            var cheat = Box("Cheat", width);

            Assert.LessOrEqual(cheat.yMax, StripTop);
            Assert.Greater(cheat.xMin, Box("Health", width).xMax);
            Assert.Less(cheat.xMax, Box("Wave", width).xMin);
        }

        [TearDown]
        public void TearDown()
        {
            if (_instance != null) Object.DestroyImmediate(_instance);
        }

        [TestCase(Wide16By9)]
        [TestCase(Wide21By9)]
        public void TopRowGoesPauseThenMoneyThenHealthThenWaveFromLeftToRight(float width)
        {
            var pause = Box("Pause", width);
            var coin = Box("Coin", width);
            var health = Box("Health", width);
            var wave = Box("Wave", width);

            Assert.Less(pause.xMax, coin.xMin);
            Assert.Less(coin.xMax, health.xMin);
            Assert.Less(health.xMax, wave.xMin);
            Assert.AreEqual(width / 2f, health.center.x, 0.5f);
        }

        [TestCase(Wide16By9)]
        [TestCase(Wide21By9)]
        public void WithoutThePauseButtonTheTopRowIsMoneyHealthWaveAndTheCornerStaysEmpty(float width)
        {
            // IDEA: parts on screen are health, money, wave and the Thunder button; no pause button.
            _view.ShowPauseButton(false);
            var parts = Parts(width);

            Assert.IsFalse(parts.ContainsKey("Pause"));
            foreach (var part in parts)
            {
                if (part.Key == "Band") continue;
                Assert.IsFalse(part.Value.Overlaps(Rect.MinMaxRect(40f, 10f, 160f, 130f)), part.Key + " sits in the pause corner");
            }

            Assert.Less(Box("Coins", width).xMax, Box("Health", width).xMin);
            Assert.Less(Box("Health", width).xMax, Box("Wave", width).xMin);
        }

        [TestCase(Wide16By9)]
        [TestCase(Wide21By9)]
        public void PauseButtonSitsInTheTopLeftCornerOnTheOtherEdgeFromTheSkills(float width)
        {
            var pause = Box("Pause", width);

            Assert.AreEqual(40f, pause.xMin, 0.5f);
            Assert.AreEqual(10f, pause.yMin, 0.5f);
            Assert.AreEqual(120f, pause.width, 0.5f);
            Assert.AreEqual(120f, pause.height, 0.5f);
            Assert.LessOrEqual(pause.yMax, StripTop, "pause is on the top edge");
            Assert.GreaterOrEqual(Box("Skill Sấm sét", width).yMin, StripBottom, "skills are on the bottom edge");
        }

        [Test]
        public void PauseButtonIsOnePressableButtonWithTwoVerticalBarsAndNoText()
        {
            var pause = _root.Find("Pause");
            Assert.IsNotNull(pause);

            var selectables = 0;
            foreach (var component in pause.GetComponentsInChildren<Component>(true))
            {
                Assert.IsFalse(component.GetType().Name == "Text", "pause button has text on " + component.name);
                if (IsSelectable(component)) selectables++;
            }

            Assert.AreEqual(1, selectables);
            Assert.AreEqual(1, CountNamed(_root, "Pause"), "exactly one pause button");

            if (SpriteOf(pause.gameObject) != null) return;
            var left = BoxOf((RectTransform)pause.Find("Bar left"), Wide16By9);
            var right = BoxOf((RectTransform)pause.Find("Bar right"), Wide16By9);
            Assert.Greater(left.height, left.width, "bars stand upright");
            Assert.Greater(right.height, right.width, "bars stand upright");
            Assert.Less(left.xMax, right.xMin);
            Assert.AreEqual(left.yMin, right.yMin, 0.5f);
        }

        [TestCase(Wide16By9)]
        [TestCase(Wide21By9)]
        public void WithASafeAreaEveryPartStaysInsideItAndKeepsItsOrder(float width)
        {
            // Notch 80 on the left, rounded corner 60 on the right, navigation bar 30 at the bottom (beyond the margins).
            _view.ApplySafeArea(new HudInsets(80f, 60f, 0f, 30f));

            foreach (var part in Parts(width))
            {
                var box = part.Value;
                Assert.GreaterOrEqual(box.xMin, 80f - 0.5f, part.Key);
                Assert.LessOrEqual(box.xMax, width - 60f + 0.5f, part.Key);
                Assert.LessOrEqual(box.yMax, Height - 30f + 0.5f, part.Key);
            }

            Assert.Less(Box("Pause", width).xMax, Box("Coin", width).xMin);
            Assert.Less(Box("Skill Sấm sét", width).xMax, Box("Skill Băng giá", width).xMin);
            Assert.Less(Box("Skill Băng giá", width).xMax, Box("Skill Cổ vũ", width).xMin);
        }

        [TestCase(Wide16By9)]
        [TestCase(Wide21By9)]
        public void UpgradeBarAlwaysGetsAtLeastItsFrameWidthLeftOfTheSkills(float width)
        {
            // Frame: upgrade bar x 40..1380 (1340 wide), then 40 to the cluster. Six buttons shrink inside that part;
            // the cluster never gives up room, so the free bottom edge is never narrower than the frame's.
            var clusterLeft = Box("Skill Sấm sét", width).xMin;
            Assert.GreaterOrEqual(clusterLeft, 40f + 1340f + 40f - 0.5f);
            Assert.AreEqual(width - 40f - HudSkillSlots.ClusterWidth, clusterLeft, 0.5f);
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
                if (!child.gameObject.activeSelf) continue;
                parts[child.name] = BoxOf((RectTransform)child, width);
            }

            Assert.IsNotEmpty(parts);
            return parts;
        }

        /// <summary>Box of a HUD part on the screen (y down from the top), through the safe-area box it sits in.</summary>
        private Rect BoxOf(RectTransform rect, float width)
        {
            var part = UpBoxOnScreen(rect, width);
            return Rect.MinMaxRect(part.xMin, Height - part.yMax, part.xMax, Height - part.yMin);
        }

        /// <summary>Box on the screen, y up, walking the parents up to the canvas (the whole screen).</summary>
        private Rect UpBoxOnScreen(RectTransform rect, float width)
        {
            if (rect == _canvasRoot) return new Rect(0f, 0f, width, Height);
            var parent = UpBoxOnScreen((RectTransform)rect.parent, width);
            return UpBox(rect, parent.xMin, parent.yMin, parent.width, parent.height);
        }

        /// <summary>Box of a rect inside a parent box, y measured upward (Unity's way).</summary>
        private static Rect UpBox(RectTransform rect, float parentX, float parentY, float parentWidth, float parentHeight)
        {
            var left = parentX + rect.anchorMin.x * parentWidth + rect.offsetMin.x;
            var right = parentX + rect.anchorMax.x * parentWidth + rect.offsetMax.x;
            var bottom = parentY + rect.anchorMin.y * parentHeight + rect.offsetMin.y;
            var top = parentY + rect.anchorMax.y * parentHeight + rect.offsetMax.y;
            return Rect.MinMaxRect(left, bottom, right, top);
        }

        private static int CountNamed(Transform parent, string name)
        {
            var count = 0;
            foreach (Transform child in parent)
                if (child.name == name) count++;
            return count;
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
