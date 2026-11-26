using NUnit.Framework;
using UnityEngine;

namespace Game.ButtonTest.Tests
{
    // button-spec#bottom-left: nút thử ở góc trái dưới màn trận, ngay trên thanh dưới của HUD (frame main.frame trên live).
    public sealed class ButtonLayoutTests
    {
        // Số của frame HUD (feature-hud/hud-layout/layout-spec, main.frame), chép vào đây làm đồ giả: px trên màn 1920x1080.
        private const float ScreenWidth = 1920f;
        private const float StripTop = 140f;
        private const float HudBottomTop = 900f;
        private const float SafeMargin = 40f;
        private const float MinTapEdge = 120f;

        private ButtonTestConfig _config;

        [SetUp]
        public void SetUp() => _config = ScriptableObject.CreateInstance<ButtonTestConfig>();

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(_config);

        [Test]
        public void BottomLeft_ButtonSitsAtLeftEdgeInsideSafeMargin()
        {
            var button = _config.Layout.Button;

            Assert.AreEqual(SafeMargin, button.X, "Nút sát mép trái, cách đúng vùng an toàn.");
            Assert.Less(button.X + button.Width, ScreenWidth / 2f, "Nút ở nửa trái màn, không còn ở mép phải.");
        }

        [Test]
        public void BottomLeft_ButtonBottomSitsJustAboveHudBottomBar_WithoutCoveringIt()
        {
            var button = _config.Layout.Button;
            var bottom = button.Y + button.Height;

            Assert.LessOrEqual(bottom, HudBottomTop, "Đáy nút không đè lên thanh dưới của HUD.");
            Assert.GreaterOrEqual(bottom, HudBottomTop - SafeMargin, "Đáy nút ngay trên thanh dưới của HUD.");
            Assert.GreaterOrEqual(button.Y, StripTop, "Nút nằm trong dải chiến trường.");
        }

        [Test]
        public void BottomLeft_MatchesLiveFrame_AndKeepsMinimumTapEdge()
        {
            var layout = _config.Layout;

            Assert.AreEqual(new[] { 40f, 756f, 200f, 120f },
                new[] { layout.Button.X, layout.Button.Y, layout.Button.Width, layout.Button.Height });
            Assert.AreEqual(new[] { 264f, 786f, 300f, 60f },
                new[] { layout.Counter.X, layout.Counter.Y, layout.Counter.Width, layout.Counter.Height });
            Assert.GreaterOrEqual(Mathf.Min(layout.Button.Width, layout.Button.Height), MinTapEdge);
            Assert.DoesNotThrow(_config.Validate);
        }
    }
}
