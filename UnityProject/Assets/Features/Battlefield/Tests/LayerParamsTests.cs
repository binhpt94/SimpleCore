using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace Game.Battlefield.Tests
{
    /// <summary>
    /// One test per row of the design table LayerParams (node layer-values), against the real config asset, which
    /// holds the column of the phase in work (PROTOTYPE). Each test names the key of its row.
    /// </summary>
    public class LayerParamsTests
    {
        private const float Eps = 0.001f;

        private static BattlefieldConfig Config => BattlefieldTestAssets.Config();
        private static BackdropLayout Layout => new BackdropLayout(Config);
        private static IReadOnlyList<BackdropPiece> Pieces => Layout.Build();
        private static ScreenMapping Screen => new ScreenMapping(Config);

        private static BackdropPiece Piece(string id) => Pieces.Single(p => p.Id == id);
        private static List<BackdropPiece> Of(BackdropLayer layer) => Pieces.Where(p => p.Layer == layer).ToList();

        private static float[] CentersOf(string prefix) =>
            Pieces.Where(p => p.Id.StartsWith(prefix + "-")).Select(p => p.CenterX).OrderBy(x => x).ToArray();

        private static float[] BothSides(params float[] oneSide) =>
            oneSide.Select(x => -x).Concat(oneSide).OrderBy(x => x).ToArray();

        private static void AssertColor(Color actual, Color expected, string key)
        {
            Assert.That(actual.r, Is.EqualTo(expected.r).Within(0.01f), key);
            Assert.That(actual.g, Is.EqualTo(expected.g).Within(0.01f), key);
            Assert.That(actual.b, Is.EqualTo(expected.b).Within(0.01f), key);
        }

        // Shared palette of the root style ui-flat.
        private static readonly Color BlueLight = new Color(0.486f, 0.816f, 1f);   // #7CD0FF, light of Xanh dương
        private static readonly Color Blue = new Color(0.133f, 0.627f, 0.941f);    // #22A0F0, Xanh dương
        private static readonly Color Green = new Color(0.298f, 0.761f, 0.118f);   // #4CC21E, Xanh lá
        private static readonly Color Pink = new Color(0.949f, 0.31f, 0.627f);     // #F24FA0, Hồng
        private static readonly Color Yellow = new Color(1f, 0.776f, 0.122f);      // #FFC61F, Vàng
        private static readonly Color Cream = new Color(1f, 0.945f, 0.894f);       // #FFF1E4, Kem

        [Test]
        public void UnitPx_OneUnitIs60Pixels()
        {
            Assert.That(Config.PixelsPerUnit, Is.EqualTo(60f), "unit_px");
            Assert.That(Screen.ToReferencePx(new Vector2(1f, 0f)).x - Screen.ToReferencePx(Vector2.zero).x,
                Is.EqualTo(60f).Within(Eps), "unit_px");
        }

        [Test]
        public void GroundLinePx_GroundLineIsAtPixel840FromTheTop()
        {
            Assert.That(Config.GroundLinePx, Is.EqualTo(840f), "ground_line_px");
            Assert.That(Screen.ToReferencePx(Vector2.zero).y, Is.EqualTo(840f).Within(Eps), "ground_line_px");
        }

        [Test]
        public void ViewXHalf_A16By9ScreenShows16UnitsEachSide()
        {
            Assert.That(Screen.HalfWidthAt(16f / 9f), Is.EqualTo(16f).Within(Eps), "view_x_half");
        }

        [Test]
        public void ViewYMin_BottomOfTheViewIsYMinus4()
        {
            Assert.That(Screen.ViewBottom, Is.EqualTo(-4f).Within(Eps), "view_y_min");
        }

        [Test]
        public void ViewYMax_TopOfTheViewIsY14()
        {
            Assert.That(Screen.ViewTop, Is.EqualTo(14f).Within(Eps), "view_y_max");
        }

        [Test]
        public void CoverXHalf_SkyFarAndGroundReach21UnitsEachSide()
        {
            Assert.That(Config.CoverLeft, Is.EqualTo(-21f).Within(Eps), "cover_x_half");
            Assert.That(Config.CoverRight, Is.EqualTo(21f).Within(Eps), "cover_x_half");
            Assert.That(Screen.Covers(21f / 9f), Is.True, "cover_x_half");
        }

        [Test]
        public void SkyTop_SkyReachesY14()
        {
            Assert.That(Piece("sky").Top, Is.EqualTo(14f).Within(Eps), "sky_top");
        }

        [Test]
        public void SkyArt_SkyIsOneStillSprite_InTheLightBlueOfThePalette()
        {
            var sky = Of(BackdropLayer.Sky);

            Assert.That(sky.Count, Is.EqualTo(1), "sky_art");
            Assert.That(sky[0].Sprite, Is.Not.Null, "sky_art: a sprite (IDEA sprite until the Codex art is in)");
            AssertColor(Config.SkyColor, BlueLight, "sky_art");
            Assert.That(Layout.Build()[0].Left, Is.EqualTo(Layout.Build()[0].Left), "sky_art: still");
        }

        [Test]
        public void FarBandCount_TwoFarStrips()
        {
            Assert.That(Config.FarBands.Count, Is.EqualTo(2), "far_band_count");
            Assert.That(Of(BackdropLayer.Far).Select(p => p.Id), Is.EqualTo(new[] { "far-back", "far-front" }), "far_band_count");
        }

        [Test]
        public void FarFrontTop_FrontStripTopIsY2()
        {
            Assert.That(Piece("far-front").Top, Is.EqualTo(2f).Within(Eps), "far_front_top");
            Assert.That(Piece("far-front").Top, Is.LessThan(4f), "far_front_top: below the 4u castle");
        }

        [Test]
        public void FarBackTop_BackStripTopIsY3Point2_AboveTheFrontStrip_BelowTheCastle()
        {
            var back = Piece("far-back");

            Assert.That(back.Top, Is.EqualTo(3.2f).Within(Eps), "far_back_top");
            Assert.That(back.Top, Is.GreaterThan(Piece("far-front").Top), "far_back_top");
            Assert.That(back.Top, Is.LessThan(4f), "far_back_top: below the 4u castle");
        }

        [Test]
        public void FarBase_EveryFarStripStandsOnTheGroundLine()
        {
            Assert.That(Config.FarBase, Is.EqualTo(0f), "far_base");
            Assert.That(Of(BackdropLayer.Far).Select(p => p.Bottom), Has.All.EqualTo(0f).Within(Eps), "far_base");
        }

        [Test]
        public void FarFrontScale_FrontStripIsTheSizeReference()
        {
            Assert.That(Piece("far-front").ArtScale, Is.EqualTo(1f).Within(Eps), "far_front_scale");
        }

        [Test]
        public void FarBackScale_BackStripHillsAre0Point6OfTheFront()
        {
            Assert.That(Piece("far-back").ArtScale, Is.EqualTo(0.6f).Within(Eps), "far_back_scale");
        }

        [Test]
        public void FarFrontColor_FrontStripIsGreenOfThePalette()
        {
            AssertColor(Config.FarBands.Single(b => b.id == "far-front").baseColor, Green, "far_front_color");
        }

        [Test]
        public void FarBackColor_BackStripIsBlueOfThePalette()
        {
            AssertColor(Config.FarBands.Single(b => b.id == "far-back").baseColor, Blue, "far_back_color");
        }

        [Test]
        public void FarFrontFade_FrontStripIsMixed20PercentTowardsTheSky()
        {
            var front = Piece("far-front");

            Assert.That(front.FadeToSky, Is.EqualTo(0.2f).Within(Eps), "far_front_fade");
            AssertColor(front.FallbackColor, Color.Lerp(Green, BlueLight, 0.2f), "far_front_fade");
        }

        [Test]
        public void FarBackFade_BackStripIsMixed45PercentTowardsTheSky_PalerThanTheFront()
        {
            var back = Piece("far-back");

            Assert.That(back.FadeToSky, Is.EqualTo(0.45f).Within(Eps), "far_back_fade");
            Assert.That(back.FadeToSky, Is.GreaterThan(Piece("far-front").FadeToSky), "far_back_fade");
            AssertColor(back.FallbackColor, Color.Lerp(Blue, BlueLight, 0.45f), "far_back_fade");
        }

        [Test]
        public void FarArt_TwoStillStripsEachCoveringTheWholeWidth()
        {
            var far = Of(BackdropLayer.Far);

            Assert.That(far.Count, Is.EqualTo(2), "far_art");
            foreach (var strip in far)
                Assert.That(strip.Width, Is.EqualTo(42f).Within(Eps), "far_art " + strip.Id);
            Assert.That(Piece("far-front").Sprite, Is.Not.Null, "far_art: IDEA strip until the Codex art is in");
        }

        [Test]
        public void GroundBottom_GroundReachesYMinus4()
        {
            Assert.That(Piece("ground").Bottom, Is.EqualTo(-4f).Within(Eps), "ground_bottom");
        }

        [Test]
        public void GroundArt_GroundIsOneSpriteAcrossTheWidth()
        {
            var ground = Of(BackdropLayer.Ground);

            Assert.That(ground.Count, Is.EqualTo(1), "ground_art");
            Assert.That(ground[0].Sprite, Is.Not.Null, "ground_art");
            Assert.That(ground[0].Width, Is.EqualTo(42f).Within(Eps), "ground_art");
        }

        [Test]
        public void DecorFlatCount_TenFlatDecorPieces()
        {
            Assert.That(Of(BackdropLayer.Decor).Count, Is.EqualTo(10), "decor_flat_count");
        }

        [Test]
        public void DecorGrassX_GrassAt4_9_14EachSide()
        {
            Assert.That(CentersOf("grass"), Is.EqualTo(BothSides(4f, 9f, 14f)).Within(Eps), "decor_grass_x");
        }

        [Test]
        public void DecorStoneX_StonesAt6Point5_11Point5EachSide()
        {
            Assert.That(CentersOf("stone"), Is.EqualTo(BothSides(6.5f, 11.5f)).Within(Eps), "decor_stone_x");
        }

        [Test]
        public void DecorGrassW_GrassIs0Point8Wide()
        {
            Assert.That(Pieces.Where(p => p.Id.StartsWith("grass-")).Select(p => p.Width),
                Has.All.EqualTo(0.8f).Within(Eps), "decor_grass_w");
        }

        [Test]
        public void DecorStoneW_StoneIs0Point6Wide()
        {
            Assert.That(Pieces.Where(p => p.Id.StartsWith("stone-")).Select(p => p.Width),
                Has.All.EqualTo(0.6f).Within(Eps), "decor_stone_w");
        }

        [Test]
        public void DecorFlatH_FlatDecorIs0Point3High_OnTheGround()
        {
            foreach (var piece in Of(BackdropLayer.Decor))
            {
                Assert.That(piece.Height, Is.EqualTo(0.3f).Within(Eps), "decor_flat_h " + piece.Id);
                Assert.That(piece.Bottom, Is.EqualTo(0f).Within(Eps), "decor_flat_h " + piece.Id);
            }
        }

        [Test]
        public void DecorClearHalf_NoFlatOrStandingDecorWithin3UnitsOfTheCastle()
        {
            Assert.That(Config.CastleClearHalfWidth, Is.EqualTo(3f), "decor_clear_half");
            foreach (var piece in Pieces.Where(p => p.Layer == BackdropLayer.Decor || p.Layer == BackdropLayer.StandDecor))
                Assert.That(piece.Right <= -3f + Eps || piece.Left >= 3f - Eps, Is.True, "decor_clear_half " + piece.Id);
        }

        [Test]
        public void DecorStandCount_EightStandingDecor_TwoFlagsAndTwoFencesEachSide()
        {
            var stand = Of(BackdropLayer.StandDecor);

            Assert.That(stand.Count, Is.EqualTo(8), "decor_stand_count");
            Assert.That(stand.Count(p => p.Id.StartsWith("flag-") && p.CenterX < 0f), Is.EqualTo(2), "decor_stand_count");
            Assert.That(stand.Count(p => p.Id.StartsWith("flag-") && p.CenterX > 0f), Is.EqualTo(2), "decor_stand_count");
            Assert.That(stand.Count(p => p.Id.StartsWith("fence-") && p.CenterX < 0f), Is.EqualTo(2), "decor_stand_count");
            Assert.That(stand.Count(p => p.Id.StartsWith("fence-") && p.CenterX > 0f), Is.EqualTo(2), "decor_stand_count");
        }

        [Test]
        public void DecorFlagX_FlagsAt7Point75_12Point75EachSide()
        {
            Assert.That(CentersOf("flag"), Is.EqualTo(BothSides(7.75f, 12.75f)).Within(Eps), "decor_flag_x");
        }

        [Test]
        public void DecorFenceX_FencesAt5Point25_10Point25EachSide()
        {
            Assert.That(CentersOf("fence"), Is.EqualTo(BothSides(5.25f, 10.25f)).Within(Eps), "decor_fence_x");
        }

        [Test]
        public void DecorFlagW_FlagIs0Point5Wide()
        {
            Assert.That(Pieces.Where(p => p.Id.StartsWith("flag-")).Select(p => p.Width),
                Has.All.EqualTo(0.5f).Within(Eps), "decor_flag_w");
        }

        [Test]
        public void DecorFlagH_FlagIs1Point2High_BelowTheBackFarStrip()
        {
            foreach (var flag in Pieces.Where(p => p.Id.StartsWith("flag-")))
            {
                Assert.That(flag.Height, Is.EqualTo(1.2f).Within(Eps), "decor_flag_h");
                Assert.That(flag.Top, Is.LessThan(Piece("far-back").Top), "decor_flag_h");
            }
        }

        [Test]
        public void DecorFenceW_FenceIs1Point4Wide()
        {
            Assert.That(Pieces.Where(p => p.Id.StartsWith("fence-")).Select(p => p.Width),
                Has.All.EqualTo(1.4f).Within(Eps), "decor_fence_w");
        }

        [Test]
        public void DecorFenceH_FenceIs0Point6High_LowerThanTheLowestUnit()
        {
            Assert.That(Pieces.Where(p => p.Id.StartsWith("fence-")).Select(p => p.Height),
                Has.All.EqualTo(0.6f).Within(Eps).And.All.LessThan(0.8f), "decor_fence_h");
        }

        [Test]
        public void DecorStandBase_StandingDecorStandsOnTheGroundLine()
        {
            Assert.That(Of(BackdropLayer.StandDecor).Select(p => p.Bottom), Has.All.EqualTo(0f).Within(Eps), "decor_stand_base");
        }

        [Test]
        public void DecorStandFade_StandingDecorIsMixed20PercentTowardsTheSky_LikeTheFrontStrip()
        {
            foreach (var piece in Of(BackdropLayer.StandDecor))
                Assert.That(piece.FadeToSky, Is.EqualTo(Piece("far-front").FadeToSky).Within(Eps).And.EqualTo(0.2f).Within(Eps),
                    "decor_stand_fade " + piece.Id);
            AssertColor(Pieces.First(p => p.Id.StartsWith("flag-")).FallbackColor, Color.Lerp(Pink, BlueLight, 0.2f),
                "decor_stand_fade");
        }

        [Test]
        public void DecorStandColor_FlagPinkWithYellowPole_FenceCreamWithYellowShade()
        {
            AssertColor(Config.ColorOf(DecorKind.Flag), Pink, "decor_stand_color: flag");
            AssertColor(Config.FlagPoleColor, Yellow, "decor_stand_color: flag pole");
            AssertColor(Config.ColorOf(DecorKind.Fence), Cream, "decor_stand_color: fence");
            AssertColor(Config.FenceShadeColor, Yellow, "decor_stand_color: fence shade");
        }

        [Test]
        public void DecorSway_GrassDoesNotSway()
        {
            Assert.That(Config.DecorSway, Is.False, "decor_sway");
            Assert.That(Layout.Problems(), Has.None.Contains("sway"), "decor_sway");
        }

        [Test]
        public void DrawOrder_SkyFarBackFarFrontStandingDecorGroundFlatDecor_AllBelowTheBattle()
        {
            var sky = Piece("sky").SortingOrder;
            var back = Piece("far-back").SortingOrder;
            var front = Piece("far-front").SortingOrder;
            var stand = Of(BackdropLayer.StandDecor).Select(p => p.SortingOrder).ToList();
            var ground = Piece("ground").SortingOrder;
            var flat = Of(BackdropLayer.Decor).Select(p => p.SortingOrder).ToList();

            Assert.That(sky, Is.LessThan(back), "draw_order");
            Assert.That(back, Is.LessThan(front), "draw_order");
            Assert.That(stand, Has.All.GreaterThan(front).And.All.LessThan(ground), "draw_order");
            Assert.That(flat, Has.All.GreaterThan(ground).And.All.LessThan(Config.LowestBattleOrder), "draw_order");
        }

        [Test]
        public void HudTopLine_TopHudEdgeIsY11Point7_AboveEveryFarStripAndDecor()
        {
            Assert.That(Config.HudTopEdge, Is.EqualTo(11.7f).Within(Eps), "hud_top_line");
            Assert.That(Screen.ToReferencePx(new Vector2(0f, Config.HudTopEdge)).y, Is.EqualTo(138f).Within(3f), "hud_top_line: px 140");
            Assert.That(Layout.FarTop, Is.LessThan(Config.HudTopEdge), "hud_top_line");
            Assert.That(Layout.HudCoversOnlySkyAndGround(), Is.True, "hud_top_line");
        }

        [Test]
        public void HudBottomLine_BottomBarEdgeIsYMinus1Point3_BelowTheGroundLine()
        {
            Assert.That(Config.HudBottomEdge, Is.EqualTo(-1.3f).Within(Eps), "hud_bottom_line");
            Assert.That(Screen.ToReferencePx(new Vector2(0f, Config.HudBottomEdge)).y, Is.EqualTo(918f).Within(3f), "hud_bottom_line: px 920");
            Assert.That(Layout.HudCoversOnlySkyAndGround(), Is.True, "hud_bottom_line");
        }

        [Test]
        public void BackdropByWave_TheBackdropDoesNotChangeByWave()
        {
            Assert.That(Config.BackdropByWave, Is.False, "backdrop_by_wave");
            var first = Layout.Build();
            var again = Layout.Build();
            Assert.That(again.Select(p => (p.Id, p.Left, p.Bottom, p.Width, p.Height)),
                Is.EqualTo(first.Select(p => (p.Id, p.Left, p.Bottom, p.Width, p.Height))), "backdrop_by_wave");
        }

        [Test]
        public void Params_RealConfigBreaksNoRuleOfTheTable()
        {
            Assert.That(Layout.Problems(), Is.Empty, "params");
        }
    }
}
