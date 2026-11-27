using UnityEngine;

namespace Game.Core
{
    /// <summary>
    /// The no-file art level: a square in <see cref="Look.Color"/> for 2D, a cube for 3D. Every call makes a new
    /// object and keeps nothing; the caller owns it, reuses it and destroys it.
    /// </summary>
    public static class GreyBox
    {
        private const int Pixels = 4;

        /// <summary>How a 2D GreyBox looks: its colour and the side of the square in world units.</summary>
        public readonly struct Look
        {
            public Look(Color color, float size)
            {
                Color = color;
                Size = size;
            }

            public Color Color { get; }

            /// <summary>Side of the square in world units; 0 or less counts as 1.</summary>
            public float Size { get; }

            internal float Side => Size > 0f ? Size : 1f;
        }

        /// <summary>A centred square sprite of side <c>look.Size</c>, filled with <c>look.Color</c>.</summary>
        public static Sprite Sprite(Look look)
        {
            var texture = new Texture2D(Pixels, Pixels, TextureFormat.RGBA32, false)
            {
                name = "GreyBox",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
            };
            var pixels = new Color[Pixels * Pixels];
            for (var i = 0; i < pixels.Length; i++) pixels[i] = look.Color;
            texture.SetPixels(pixels);
            texture.Apply();
            var sprite = UnityEngine.Sprite.Create(texture, new Rect(0f, 0f, Pixels, Pixels), new Vector2(0.5f, 0.5f),
                Pixels / look.Side);
            sprite.name = "GreyBox";
            return sprite;
        }

        /// <summary>A cube scaled to <paramref name="size"/> (world units), with Unity's default BoxCollider.</summary>
        public static GameObject Model(Vector3 size)
        {
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = "GreyBox";
            cube.transform.localScale = size;
            return cube;
        }
    }
}
