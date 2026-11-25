using System.Collections.Generic;

namespace Game.Battlefield
{
    /// <summary>
    /// Turns the config into the boxes of the four backdrop layers (sky, far, ground, decor) and checks them
    /// against the design rules. Pure C#: the backdrop has no game rule and reads nothing from the battle.
    /// </summary>
    internal sealed class BackdropLayout
    {
        private const float Tolerance = 0.001f;
        private readonly BattlefieldConfig _config;

        public BackdropLayout(BattlefieldConfig config)
        {
            _config = config;
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
                new BackdropPiece("far", BackdropLayer.Far, c.CoverLeft, 0f, width, c.FarHeight,
                    c.FarOrder, c.FarSprite, c.FarColor),
                new BackdropPiece("ground", BackdropLayer.Ground, c.CoverLeft, c.GroundBottom, width, -c.GroundBottom,
                    c.GroundOrder, c.GroundSprite, c.GroundColor),
            };

            for (var i = 0; i < c.Decor.Count; i++)
            {
                var slot = c.Decor[i];
                var size = c.SizeOf(slot.kind);
                var id = (slot.kind == DecorKind.Grass ? "grass-" : "stone-") + i;
                pieces.Add(new BackdropPiece(id, BackdropLayer.Decor, slot.centerX - size.x / 2f, 0f, size.x, size.y,
                    c.DecorOrder, c.SpriteOf(slot.kind), c.ColorOf(slot.kind)));
            }

            return pieces;
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
            if (c.FarHeight <= 0f || c.FarHeight >= c.HudTopEdge)
                problems.Add($"far scenery height {c.FarHeight}u must be above 0 and below the top HUD edge {c.HudTopEdge}u");
            if (c.CoverRight <= c.CoverLeft)
                problems.Add("cover right edge must be right of the left edge");

            var orders = new[] { c.SkyOrder, c.FarOrder, c.GroundOrder, c.DecorOrder };
            for (var i = 1; i < orders.Length; i++)
                if (orders[i] <= orders[i - 1])
                    problems.Add("draw order must be sky, far, ground, decor from back to front");
            if (c.DecorOrder >= c.LowestBattleOrder)
                problems.Add($"decor order {c.DecorOrder} must stay below the castle and units ({c.LowestBattleOrder})");

            foreach (var piece in Build())
            {
                if (piece.Layer != BackdropLayer.Decor) continue;
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
            if (c.HudBottomEdge >= 0f || c.HudTopEdge <= c.FarHeight) return false;
            foreach (var piece in Build())
                if (piece.Layer == BackdropLayer.Decor && (piece.Bottom < c.HudBottomEdge || piece.Top > c.HudTopEdge))
                    return false;
            return true;
        }
    }
}
