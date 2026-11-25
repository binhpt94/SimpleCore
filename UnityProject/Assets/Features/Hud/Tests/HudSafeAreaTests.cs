using NUnit.Framework;

namespace Game.Hud.Tests
{
    /// <summary>feature-hud/hud-layout/layout-spec#safe-area: how the device safe area moves the HUD parts in.</summary>
    public sealed class HudSafeAreaTests
    {
        [Test]
        public void FullSafeAreaMovesNothing()
        {
            var insets = HudSafeArea.Insets(2400f, 1080f, 0f, 0f, 2400f, 1080f);

            Assert.IsTrue(insets.IsNone);
        }

        [Test]
        public void NotchNarrowerThanTheSideMarginMovesNothing()
        {
            // 30 px notch on the left of a 1080-tall screen: inside the 40 unit margin the layout already keeps.
            var insets = HudSafeArea.Insets(2400f, 1080f, 30f, 0f, 2370f, 1080f);

            Assert.AreEqual(0f, insets.Left);
        }

        [Test]
        public void WideNotchMovesTheLeftSideInByWhatTheMarginDoesNotCover()
        {
            // 132 px notch on the left of a 2532x1170 phone: 132 / (1170/1080) = 121.8 units, minus the 40 margin.
            var insets = HudSafeArea.Insets(2532f, 1170f, 132f, 0f, 2400f, 1170f);

            Assert.AreEqual(132f * 1080f / 1170f - 40f, insets.Left, 0.01f);
            Assert.AreEqual(0f, insets.Right);
        }

        [Test]
        public void NavigationBarAtTheBottomMovesTheBottomPartsUpByWhatTheMarginDoesNotCover()
        {
            // 63 px home bar at the bottom of a 1170-tall screen: 58.2 units, minus the 20 margin.
            var insets = HudSafeArea.Insets(2532f, 1170f, 0f, 63f, 2532f, 1107f);

            Assert.AreEqual(63f * 1080f / 1170f - 20f, insets.Bottom, 0.01f);
            Assert.AreEqual(0f, insets.Top);
        }

        [Test]
        public void CutsOnEverySideAreMeasuredFromTheirOwnEdge()
        {
            var insets = HudSafeArea.Insets(2160f, 1080f, 100f, 50f, 1900f, 1010f);

            Assert.AreEqual(60f, insets.Left, 0.01f);   // 100 - 40
            Assert.AreEqual(120f, insets.Right, 0.01f); // 2160 - 2000 = 160, - 40
            Assert.AreEqual(10f, insets.Top, 0.01f);    // 1080 - 1060 = 20, - 10
            Assert.AreEqual(30f, insets.Bottom, 0.01f); // 50 - 20
        }

        [Test]
        public void EmptyScreenGivesNoInsets()
        {
            Assert.IsTrue(HudSafeArea.Insets(0f, 0f, 0f, 0f, 0f, 0f).IsNone);
        }
    }
}
