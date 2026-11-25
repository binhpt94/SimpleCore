using UnityEngine;

namespace Game.Battlefield
{
    /// <summary>The backdrop layers, back to front: sky, far scenery strips, standing decor, ground, flat decor.</summary>
    internal enum BackdropLayer
    {
        Sky,
        Far,
        StandDecor,
        Ground,
        Decor,
    }

    /// <summary>One box of the backdrop in world units: left, bottom, width, height, draw order and look.</summary>
    internal readonly struct BackdropPiece
    {
        public readonly string Id;
        public readonly BackdropLayer Layer;
        public readonly float Left;
        public readonly float Bottom;
        public readonly float Width;
        public readonly float Height;
        public readonly int SortingOrder;
        public readonly Sprite Sprite;

        /// <summary>The colour of the box drawn when <see cref="Sprite"/> is empty, already mixed towards the sky.</summary>
        public readonly Color FallbackColor;

        /// <summary>How far the piece's colour is mixed towards the sky colour (0 to 1), to read as farther away.</summary>
        public readonly float FadeToSky;

        /// <summary>Size of the shapes drawn in the piece, relative to the front far strip (1).</summary>
        public readonly float ArtScale;

        public BackdropPiece(string id, BackdropLayer layer, float left, float bottom, float width, float height,
            int sortingOrder, Sprite sprite, Color fallbackColor, float fadeToSky = 0f, float artScale = 1f)
        {
            Id = id;
            Layer = layer;
            Left = left;
            Bottom = bottom;
            Width = width;
            Height = height;
            SortingOrder = sortingOrder;
            Sprite = sprite;
            FallbackColor = fallbackColor;
            FadeToSky = fadeToSky;
            ArtScale = artScale;
        }

        public float Right => Left + Width;
        public float Top => Bottom + Height;
        public float CenterX => Left + Width / 2f;
    }
}
