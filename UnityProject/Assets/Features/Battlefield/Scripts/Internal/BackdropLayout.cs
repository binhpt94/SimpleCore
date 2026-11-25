using System.Collections.Generic;
using UnityEngine;

namespace Game.Battlefield
{
    /// <summary>
    /// Turns the config into the boxes of the backdrop (sky, far scenery strips, standing decor, ground, flat decor)
    /// and checks them against the design rules. Pure C#: the backdrop has no game rule and reads nothing from the
    /// battle.
    /// </summary>
    internal sealed class BackdropLayout
    {
        private const float Tolerance = 0.001f;
        private readonly BattlefieldConfig _config;

        public BackdropLayout(BattlefieldConfig config)
        {
            _config = config;
        }

        /// <summary>Mixes <paramref name="baseColor"/> towards the sky colour by <paramref name="fade"/> (0 to 1).</summary>
        public Color Faded(Color baseColor, float fade) => Color.Lerp(baseColor, _config.SkyColor, Mathf.Clamp01(fade));

        /// <summary>Top of the tallest far strip (the far base when there is none).</summary>
        public float FarTop
        {
            get
            {
                var top = _config.FarBase;
                foreach (var band in _config.FarBands) top = Mathf.Max(top, band.top);
                return top;
            }
        }

        /// <summary>Every piece, in draw order from back to front.</summary>
        public IReadOnlyList<BackdropPiece> Build()
        {
            var c = _config;
            var width = c.CoverRight - c.CoverLeft;
            var pieces = new List<BackdropPiece>
            {
                new BackdropPiece("sky", BackdropLayer.Sky, c.CoverLeft, 0f, width, c.SkyTop,
                    c.SkyOrder, c.SkySprite, c.SkyColor),
            };

            for (var i = 0; i < c.FarBands.Count; i++)
            {
                var band = c.FarBands[i];
                pieces.Add(new BackdropPiece(band.id, BackdropLayer.Far, c.CoverLeft, c.FarBase, width, band.top - c.FarBase,
                    c.FarOrder + i, band.sprite, Faded(band.baseColor, band.fade), band.fade, band.artScale));
            }

            AddDecor(pieces, standing: true);

            pieces.Add(new BackdropPiece("ground", BackdropLayer.Ground, c.CoverLeft, c.GroundBottom, width, -c.GroundBottom,
                c.GroundOrder, c.GroundSprite, c.GroundColor));

            AddDecor(pieces, standing: false);
            return pieces;
        }

        private void AddDecor(List<BackdropPiece> pieces, bool standing)
        {
            var c = _config;
            for (var i = 0; i < c.Decor.Count; i++)
            {
                var slot = c.Decor[i];
                if (BattlefieldConfig.IsStanding(slot.kind) != standing) continue;
                var size = c.SizeOf(slot.kind);
                var id = slot.kind.ToString().ToLowerInvariant() + "-" + i;
                var fade = c.FadeOf(slot.kind);
                var layer = standing ? BackdropLayer.StandDecor : BackdropLayer.Decor;
                var order = standing ? c.StandDecorOrder : c.DecorOrder;
                var bottom = standing ? c.StandDecorBase : 0f;
                pieces.Add(new BackdropPiece(id, layer, slot.centerX - size.x / 2f, bottom, size.x, size.y,
                    order, c.SpriteOf(slot.kind), Faded(c.ColorOf(slot.kind), fade), fade));
            }
        }

        /// <summary>What breaks the design in the config; empty when the backdrop is right.</summary>
        public IReadOnlyList<string> Problems()
        {
            var c = _config;
            var problems = new List<string>();
            var screen = new ScreenMapping(c);

            if (c.SkyTop < screen.ViewTop - Tolerance)
                problems.Add($"sky stops at y={c.SkyTop}u, below the top of the view y={screen.ViewTop}u");
            if (c.GroundBottom > screen.ViewBottom + Tolerance)
                problems.Add($"ground stops at y={c.GroundBottom}u, above the bottom of the view y={screen.ViewBottom}u");
            if (c.FarBands.Count == 0)
                problems.Add("there is no far scenery strip");
            foreach (var band in c.FarBands)
                if (band.top <= c.FarBase || band.top >= c.HudTopEdge)
                    problems.Add($"far strip {band.id} top {band.top}u must be above its base {c.FarBase}u and below the top HUD edge {c.HudTopEdge}u");
            if (c.CoverRight <= c.CoverLeft)
                problems.Add("cover right edge must be right of the left edge");
            if (c.DecorSway)
                problems.Add("grass sway is not in this phase");
            if (c.BackdropByWave)
                problems.Add("the backdrop does not change by wave (one map)");

            var farLast = c.FarOrder + c.FarBands.Count - 1;
            var orders = new[] { c.SkyOrder, c.FarOrder, farLast, c.StandDecorOrder, c.GroundOrder, c.DecorOrder };
            for (var i = 1; i < orders.Length; i++)
                if (orders[i] < orders[i - 1] || (i != 2 && orders[i] == orders[i - 1]))
                    problems.Add("draw order must be sky, far strips, standing decor, ground, flat decor from back to front");
            if (c.DecorOrder >= c.LowestBattleOrder)
                problems.Add($"decor order {c.DecorOrder} must stay below the castle and units ({c.LowestBattleOrder})");

            foreach (var piece in Build())
            {
                if (piece.Layer != BackdropLayer.Decor && piece.Layer != BackdropLayer.StandDecor) continue;
                if (piece.Right > -c.CastleClearHalfWidth + Tolerance && piece.Left < c.CastleClearHalfWidth - Tolerance)
                    problems.Add($"decor {piece.Id} at x={piece.CenterX}u reaches into the castle zone ±{c.CastleClearHalfWidth}u");
                if (piece.Left < c.CoverLeft - Tolerance || piece.Right > c.CoverRight + Tolerance)
                    problems.Add($"decor {piece.Id} at x={piece.CenterX}u is outside the ground");
                if (piece.Bottom < c.HudBottomEdge || piece.Top > c.HudTopEdge)
                    problems.Add($"decor {piece.Id} goes under the HUD");
            }

            return problems;
        }

        /// <summary>
        /// True when the top HUD band only covers sky and the bottom bar only covers ground: the ground line,
        /// far scenery and decor stay between the two HUD edges.
        /// </summary>
        public bool HudCoversOnlySkyAndGround()
        {
            var c = _config;
            if (c.HudBottomEdge >= 0f || c.HudTopEdge <= FarTop) return false;
            foreach (var piece in Build())
                if ((piece.Layer == BackdropLayer.Decor || piece.Layer == BackdropLayer.StandDecor)
                    && (piece.Bottom < c.HudBottomEdge || piece.Top > c.HudTopEdge))
                    return false;
            return true;
        }
    }
}
