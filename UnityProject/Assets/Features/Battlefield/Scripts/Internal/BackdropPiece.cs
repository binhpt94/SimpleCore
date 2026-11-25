using UnityEngine;

namespace Game.Battlefield
{
    /// <summary>The four backdrop layers, back to front.</summary>
    internal enum BackdropLayer
    {
        Sky,
        Far,
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
        public readonly Color FallbackColor;

        public BackdropPiece(string id, BackdropLayer layer, float left, float bottom, float width, float height,
            int sortingOrder, Sprite sprite, Color fallbackColor)
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
        }

        public float Right => Left + Width;
        public float Top => Bottom + Height;
        public float CenterX => Left + Width / 2f;
    }
}
