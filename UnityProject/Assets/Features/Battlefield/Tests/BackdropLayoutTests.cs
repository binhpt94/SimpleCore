using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace Game.Battlefield.Tests
{
    public class BackdropLayoutTests
    {
        private const float Eps = 0.001f;

        private static IReadOnlyList<BackdropPiece> Pieces() => new BackdropLayout(BattlefieldTestAssets.Config()).Build();

        private static BackdropPiece Layer(BackdropLayer layer) => Pieces().Single(p => p.Layer == layer);

        private static IEnumerable<BackdropPiece> Decor() => Pieces().Where(p => p.Layer == BackdropLayer.Decor);

        [Test]
        public void Camera_GroundLineSitsAtPixel840_AndOneUnitIs60Pixels()
        {
            var screen = new ScreenMapping(BattlefieldTestAssets.Config());

            Assert.That(screen.ToReferencePx(Vector2.zero), Is.EqualTo(new Vector2(960f, 840f)));
            Assert.That(screen.ToReferencePx(new Vector2(1f, 1f)), Is.EqualTo(new Vector2(1020f, 780f)));
        }

        [Test]
        public void Camera_At16By9_ShowsXMinus16To16_AndYMinus4To14()
        {
            var screen = new ScreenMapping(BattlefieldTestAssets.Config());

            Assert.That(screen.HalfWidthAt(16f / 9f), Is.EqualTo(16f).Within(Eps));
            Assert.That(screen.ViewTop, Is.EqualTo(14f).Within(Eps));
            Assert.That(screen.ViewBottom, Is.EqualTo(-4f).Within(Eps));
        }

        [Test]
        public void SkyFarAndGround_CoverXMinus21To21()
        {
            var covering = Pieces().Where(p =>
                p.Layer == BackdropLayer.Sky || p.Layer == BackdropLayer.Far || p.Layer == BackdropLayer.Ground).ToList();

            Assert.That(covering.Count, Is.EqualTo(4), "sky, two far strips, ground");
            foreach (var piece in covering)
            {
                Assert.That(piece.Left, Is.EqualTo(-21f).Within(Eps), piece.Id);
                Assert.That(piece.Right, Is.EqualTo(21f).Within(Eps), piece.Id);
            }
        }

        [Test]
        public void Sky_FillsFromTheGroundLineToY14()
        {
            var sky = Layer(BackdropLayer.Sky);

            Assert.That(sky.Bottom, Is.EqualTo(0f).Within(Eps));
            Assert.That(sky.Top, Is.EqualTo(14f).Within(Eps));
        }

        [Test]
        public void FarScenery_IsOneStripStandingOnTheGround_2Point5High_BelowTheCastle()
        {
            // The IDEA column of LayerParams: one far strip, 2.5u high. The real config holds the PROTOTYPE column
            // (two strips), checked in LayerParamsTests.
            var config = ScriptableObject.CreateInstance<BattlefieldConfig>();
            config.FarBands = new List<FarBand> { new FarBand("far", 2.5f, 1f, Color.green, 0f) };

            var far = new BackdropLayout(config).Build().Single(p => p.Layer == BackdropLayer.Far);

            Assert.That(far.Bottom, Is.EqualTo(0f).Within(Eps));
            Assert.That(far.Height, Is.EqualTo(2.5f).Within(Eps));
            Assert.That(far.Height, Is.LessThan(4f), "castle is 4u high");
            Object.DestroyImmediate(config);
        }

        [Test]
        public void Ground_FillsFromYMinus4UpToTheGroundLine()
        {
            var ground = Layer(BackdropLayer.Ground);

            Assert.That(ground.Bottom, Is.EqualTo(-4f).Within(Eps));
            Assert.That(ground.Top, Is.EqualTo(0f).Within(Eps));
        }

        [Test]
        public void Layers_DrawSkyThenFarThenGroundThenDecor_AllBelowTheCastleAndUnits()
        {
            var config = BattlefieldTestAssets.Config();
            var sky = Layer(BackdropLayer.Sky).SortingOrder;
            var far = Pieces().Where(p => p.Layer == BackdropLayer.Far).Select(p => p.SortingOrder).ToList();
            var ground = Layer(BackdropLayer.Ground).SortingOrder;

            Assert.That(far, Has.All.GreaterThan(sky));
            Assert.That(far, Has.All.LessThan(ground));
            Assert.That(Decor().Select(d => d.SortingOrder), Has.All.GreaterThan(ground));
            Assert.That(Pieces().Select(p => p.SortingOrder), Has.All.LessThan(config.LowestBattleOrder));
        }

        [Test]
        public void Decor_IsTenFlatPieces_ThreeGrassAndTwoStonesEachSide_AtTheDesignedCentres()
        {
            var decor = Decor().ToList();
            var grass = decor.Where(d => d.Id.StartsWith("grass")).Select(d => d.CenterX).OrderBy(x => x).ToArray();
            var stone = decor.Where(d => d.Id.StartsWith("stone")).Select(d => d.CenterX).OrderBy(x => x).ToArray();

            Assert.That(decor.Count, Is.EqualTo(10));
            Assert.That(grass, Is.EqualTo(new[] { -14f, -9f, -4f, 4f, 9f, 14f }).Within(Eps));
            Assert.That(stone, Is.EqualTo(new[] { -11.5f, -6.5f, 6.5f, 11.5f }).Within(Eps));
            foreach (var piece in decor)
            {
                var expectedWidth = piece.Id.StartsWith("grass") ? 0.8f : 0.6f;
                Assert.That(piece.Width, Is.EqualTo(expectedWidth).Within(Eps), piece.Id);
                Assert.That(piece.Height, Is.EqualTo(0.3f).Within(Eps), piece.Id);
                Assert.That(piece.Bottom, Is.EqualTo(0f).Within(Eps), piece.Id);
            }
        }

        [Test]
        public void Decor_NoPieceReachesIntoXMinus3To3()
        {
            foreach (var piece in Decor())
                Assert.That(piece.Right <= -3f + Eps || piece.Left >= 3f - Eps, Is.True, $"{piece.Id} at {piece.CenterX}");
        }

        [Test]
        public void Decor_PlacedInsideTheCastleZone_IsReportedAsAProblem()
        {
            var config = ScriptableObject.CreateInstance<BattlefieldConfig>();
            config.Decor = new List<DecorSlot> { new DecorSlot(DecorKind.Grass, 2.8f) };

            var problems = new BackdropLayout(config).Problems();

            Assert.That(problems, Has.Some.Contains("castle zone"));
            Object.DestroyImmediate(config);
        }

        [Test]
        public void Hud_TopBandOnlyCoversSky_BottomBarOnlyCoversGround()
        {
            var config = BattlefieldTestAssets.Config();

            Assert.That(config.HudTopEdge, Is.EqualTo(11.7f).Within(Eps));
            Assert.That(config.HudBottomEdge, Is.EqualTo(-1.3f).Within(Eps));
            Assert.That(new BackdropLayout(config).HudCoversOnlySkyAndGround(), Is.True);
            Assert.That(new BackdropLayout(config).FarTop, Is.LessThan(config.HudTopEdge));
            Assert.That(4f, Is.LessThan(config.HudTopEdge), "castle top stays below the top HUD band");
        }

        [Test]
        public void Hud_FarSceneryReachingUnderTheTopBand_IsNotClear()
        {
            var config = ScriptableObject.CreateInstance<BattlefieldConfig>();
            config.FarBands = new List<FarBand> { new FarBand("far", 12f, 1f, Color.green, 0f) };

            Assert.That(new BackdropLayout(config).HudCoversOnlySkyAndGround(), Is.False);
            Object.DestroyImmediate(config);
        }

        [Test]
        public void WideScreen_Up21By9_IsCovered_WiderLeavesGapsAtTheSides()
        {
            var screen = new ScreenMapping(BattlefieldTestAssets.Config());

            Assert.That(screen.Covers(16f / 9f), Is.True);
            Assert.That(screen.Covers(4f / 3f), Is.True);
            Assert.That(screen.Covers(21f / 9f), Is.True);
            Assert.That(screen.Covers(32f / 9f), Is.False);
        }

        [Test]
        public void RealConfig_BreaksNoDesignRule()
        {
            Assert.That(new BackdropLayout(BattlefieldTestAssets.Config()).Problems(), Is.Empty);
        }
    }
}
