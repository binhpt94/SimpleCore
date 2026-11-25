using UnityEngine;

namespace Game.Battlefield
{
    /// <summary>
    /// The Game scene's camera as the backdrop sees it: world units to pixels on the reference screen, and the
    /// part of the world a screen of a given aspect shows. The camera is the scene's; this only reads its numbers.
    /// </summary>
    internal sealed class ScreenMapping
    {
        private readonly BattlefieldConfig _config;

        public ScreenMapping(BattlefieldConfig config)
        {
            _config = config;
        }

        /// <summary>Height of the view in u (the camera keeps this height on every aspect).</summary>
        public float ViewHeight => _config.ReferenceHeightPx / _config.PixelsPerUnit;

        /// <summary>World y at the top edge of the screen.</summary>
        public float ViewTop => _config.GroundLinePx / _config.PixelsPerUnit;

        /// <summary>World y at the bottom edge of the screen.</summary>
        public float ViewBottom => ViewTop - ViewHeight;

        /// <summary>Half the width in u that a screen of <paramref name="aspect"/> (width / height) shows.</summary>
        public float HalfWidthAt(float aspect) => ViewHeight * aspect / 2f;

        /// <summary>Pixel of a world point on the reference screen, origin at the top left.</summary>
        public Vector2 ToReferencePx(Vector2 world)
        {
            var x = _config.ReferenceWidthPx / 2f + world.x * _config.PixelsPerUnit;
            var y = _config.GroundLinePx - world.y * _config.PixelsPerUnit;
            return new Vector2(x, y);
        }

        /// <summary>True when sky, far and ground reach both side edges of a screen of <paramref name="aspect"/>.</summary>
        public bool Covers(float aspect)
        {
            const float tolerance = 0.01f;
            var half = HalfWidthAt(aspect);
            return _config.CoverRight >= half - tolerance && _config.CoverLeft <= -half + tolerance;
        }
    }
}
