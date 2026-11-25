using System.Collections.Generic;
using UnityEngine;

namespace Game.Battlefield
{
    /// <summary>
    /// Draws each backdrop piece as a sprite stretched to its box. A piece without a sprite gets a flat colour box.
    /// No collider: touches and units pass through.
    /// </summary>
    internal sealed class BackdropView : MonoBehaviour, IBackdropView
    {
        private Sprite _blank;
        private Texture2D _blankTexture;

        public void Show(IReadOnlyList<BackdropPiece> pieces)
        {
            foreach (var piece in pieces)
            {
                var item = new GameObject(piece.Id);
                item.transform.SetParent(transform, false);

                var renderer = item.AddComponent<SpriteRenderer>();
                var sprite = piece.Sprite != null ? piece.Sprite : Blank();
                renderer.sprite = sprite;
                renderer.color = piece.Sprite != null ? Color.white : piece.FallbackColor;
                renderer.sortingOrder = piece.SortingOrder;

                var bounds = sprite.bounds;
                var scale = new Vector3(piece.Width / bounds.size.x, piece.Height / bounds.size.y, 1f);
                var center = new Vector3(piece.Left + piece.Width / 2f, piece.Bottom + piece.Height / 2f, 0f);
                item.transform.localScale = scale;
                item.transform.localPosition = center - Vector3.Scale(bounds.center, scale);
            }
        }

        private Sprite Blank()
        {
            if (_blank != null) return _blank;
            _blankTexture = new Texture2D(4, 4) { name = "BattlefieldBlank" };
            var pixels = new Color[16];
            for (var i = 0; i < pixels.Length; i++) pixels[i] = Color.white;
            _blankTexture.SetPixels(pixels);
            _blankTexture.Apply();
            _blank = Sprite.Create(_blankTexture, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f);
            return _blank;
        }

        private void OnDestroy()
        {
            if (_blank != null) Destroy(_blank);
            if (_blankTexture != null) Destroy(_blankTexture);
        }
    }
}
