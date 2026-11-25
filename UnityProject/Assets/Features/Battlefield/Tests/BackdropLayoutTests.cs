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
            foreach (var layer in new[] { BackdropLayer.Sky, BackdropLayer.Far, BackdropLayer.Ground })
            {
                var piece = Layer(layer);
                Assert.That(piece.Left, Is.EqualTo(-21f).Within(Eps), layer.ToString());
                Assert.That(piece.Right, Is.EqualTo(21f).Within(Eps), layer.ToString());
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
            var far = Layer(BackdropLayer.Far);

            Assert.That(far.Bottom, Is.EqualTo(0f).Within(Eps));
            Assert.That(far.Height, Is.EqualTo(2.5f).Within(Eps));
            Assert.That(far.Height, Is.LessThan(4f), "castle is 4u high");
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
            var far = Layer(BackdropLayer.Far).SortingOrder;
            var ground = Layer(BackdropLayer.Ground).SortingOrder;

            Assert.That(sky, Is.LessThan(far));
            Assert.That(far, Is.LessThan(ground));
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
            Assert.That(Layer(BackdropLayer.Far).Top, Is.LessThan(config.HudTopEdge));
            Assert.That(4f, Is.LessThan(config.HudTopEdge), "castle top stays below the top HUD band");
        }

        [Test]
        public void Hud_FarSceneryReachingUnderTheTopBand_IsNotClear()
        {
            var config = ScriptableObject.CreateInstance<BattlefieldConfig>();
            config.FarHeight = 12f;

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
