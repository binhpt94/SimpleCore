using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Castle
{
    /// <summary>
    /// Draws the castle, its shots and the Thunder effect in world units (1 world unit = 1 battlefield unit,
    /// ground at y = 0). Sizes from the design apply to the drawn part of each sprite (its content rect), so the
    /// transparent margin of the generated art does not shrink it. Sprites left empty are drawn as plain coloured boxes.
    /// </summary>
    internal sealed class CastleView : MonoBehaviour, ICastleView
    {
        [SerializeField] private Sprite castleSprite;
        [Tooltip("PROTOTYPE: the castle under half health. Empty: the whole castle is drawn darker instead.")]
        [SerializeField] private Sprite damagedSprite;
        [SerializeField] private Color damagedTint = new Color(0.8f, 0.7f, 0.65f);
        [Tooltip("How far the castle moves sideways while it shakes from a hit (u).")]
        [SerializeField] private float shakeAmplitude = 0.08f;
        [SerializeField] private Sprite shotSprite;
        [SerializeField] private Sprite thunderSprite;
        [Tooltip("Drawn part of each sprite as a fraction of the image (x, y from bottom left, width, height); sizes apply to this part, not to the transparent margin.")]
        [SerializeField] private Rect castleContent = new Rect(70f / 512f, 0f, 372f / 512f, 496f / 512f);
        [SerializeField] private Rect shotContent = new Rect(106f / 512f, 106f / 512f, 300f / 512f, 300f / 512f);
        [SerializeField] private Rect thunderContent = new Rect(137f / 512f, 52f / 512f, 238f / 512f, 410f / 512f);
        [SerializeField] private Color castleColor = new Color(0.55f, 0.5f, 0.45f);
        [SerializeField] private Color flashColor = new Color(1f, 0.35f, 0.35f);
        [SerializeField] private Color shotColor = new Color(1f, 0.9f, 0.3f);
        [SerializeField] private Color thunderColor = new Color(0.6f, 0.8f, 1f);
        [SerializeField] private float shotHeight = 3f;
        [SerializeField] private float shotSize = 0.3f;
        [SerializeField] private float thunderHeight = 1f;
        [SerializeField] private float thunderSize = 1.5f;

        private readonly List<Shot> _shots = new List<Shot>();
        private readonly List<Effect> _thunders = new List<Effect>();
        private Sprite _placeholder;
        private SpriteRenderer _body;
        private Vector3 _bodyScale = Vector3.one;
        private float _flashLeft;
        private float _popLeft;
        private float _popTotal;
        private float _popScale = 1f;
        private float _collapseLeft;
        private float _collapseTotal;
        private bool _collapsed;
        private float _shakeLeft;
        private float _shakeTotal;
        private Vector3 _bodyPosition;
        private bool _damaged;

        public event Action<float> Ticked;

        public UnityEngine.Object ViewObject => gameObject;

        private void Awake()
        {
            var bodyObject = new GameObject("Body");
            bodyObject.transform.SetParent(transform, false);
            _body = bodyObject.AddComponent<SpriteRenderer>();
            _body.sprite = castleSprite ? castleSprite : Placeholder();
            _body.color = castleSprite ? Color.white : castleColor;
        }

        private void Update() => Ticked?.Invoke(Time.deltaTime);

        private void OnDestroy()
        {
            if (_placeholder) Destroy(_placeholder);
        }

        public void Place(float halfWidth, float height)
        {
            transform.position = Vector3.zero;
            var drawn = Drawn(_body.sprite, castleSprite ? castleContent : FullImage);
            _bodyScale = new Vector3(2f * halfWidth / Mathf.Max(drawn.width, 1e-4f), height / Mathf.Max(drawn.height, 1e-4f), 1f);
            _bodyPosition = new Vector3(-drawn.center.x * _bodyScale.x, -drawn.yMin * _bodyScale.y, 0f);
            _body.transform.localPosition = _bodyPosition;
            _body.transform.localScale = _bodyScale;
        }

        public void ShakeHit(float seconds)
        {
            if (_collapsed) return;
            _shakeTotal = Mathf.Max(seconds, 1e-4f);
            _shakeLeft = _shakeTotal;
        }

        public void ShowDamaged(bool damaged)
        {
            if (_damaged == damaged) return;
            _damaged = damaged;
            // The damaged picture uses the whole castle's size and content rect so swapping keeps the box.
            _body.sprite = damaged && damagedSprite ? damagedSprite : (castleSprite ? castleSprite : Placeholder());
            if (_flashLeft <= 0f) _body.color = BodyColor;
        }

        public void SetVisible(bool visible) => gameObject.SetActive(visible);

        public void ResetBody()
        {
            _flashLeft = 0f;
            _popLeft = 0f;
            _collapseLeft = 0f;
            _collapsed = false;
            _shakeLeft = 0f;
            _body.transform.localScale = _bodyScale;
            _body.transform.localPosition = _bodyPosition;
            _body.color = BodyColor;
        }

        public void FlashHit(float seconds)
        {
            if (_collapsed) return;
            _flashLeft = seconds;
            _body.color = castleSprite ? flashColor : Color.Lerp(castleColor, flashColor, 0.8f);
        }

        public void PopLevelUp(float scale, float seconds)
        {
            if (_collapsed) return;
            _popScale = scale;
            _popTotal = Mathf.Max(seconds, 1e-4f);
            _popLeft = _popTotal;
        }

        public void Collapse(float seconds)
        {
            _collapsed = true;
            _flashLeft = 0f;
            _popLeft = 0f;
            _shakeLeft = 0f;
            _body.transform.localPosition = _bodyPosition;
            _body.color = BodyColor;
            _collapseTotal = Mathf.Max(seconds, 1e-4f);
            _collapseLeft = _collapseTotal;
        }

        public void ShowShot(int targetHandle, float fromX, float toX, float seconds)
        {
            var renderer = MakeEffect("Shot", shotSprite, shotContent, shotColor, shotSize);
            renderer.transform.position = new Vector3(fromX, shotHeight, 0f);
            _shots.Add(new Shot
            {
                Renderer = renderer, Handle = targetHandle, FromX = fromX, ToX = toX,
                Total = Mathf.Max(seconds, 1e-4f),
            });
        }

        public void DropShotsAt(int targetHandle)
        {
            for (var i = _shots.Count - 1; i >= 0; i--)
            {
                if (_shots[i].Handle != targetHandle) continue;
                Destroy(_shots[i].Renderer.gameObject);
                _shots.RemoveAt(i);
            }
        }

        public void ShowThunder(float x, float seconds)
        {
            var renderer = MakeEffect("Thunder", thunderSprite, thunderContent, thunderColor, thunderSize * 0.5f);
            renderer.transform.position = new Vector3(x, thunderHeight, 0f);
            _thunders.Add(new Effect { Renderer = renderer, Total = Mathf.Max(seconds, 1e-4f) });
        }

        public void Advance(float seconds)
        {
            if (_flashLeft > 0f)
            {
                _flashLeft -= seconds;
                if (_flashLeft <= 0f) _body.color = BodyColor;
            }

            if (_popLeft > 0f)
            {
                _popLeft = Mathf.Max(0f, _popLeft - seconds);
                // Up to the full pop at half time, then back to normal size.
                var t = 1f - _popLeft / _popTotal;
                var grow = 1f - Mathf.Abs(2f * t - 1f);
                _body.transform.localScale = _bodyScale * Mathf.Lerp(1f, _popScale, grow);
            }

            if (_shakeLeft > 0f)
            {
                _shakeLeft = Mathf.Max(0f, _shakeLeft - seconds);
                // Left-right swing that fades out by the end of the shake.
                var swing = Mathf.Sin((1f - _shakeLeft / _shakeTotal) * Mathf.PI * 6f) * (_shakeLeft / _shakeTotal);
                _body.transform.localPosition = _bodyPosition + new Vector3(swing * shakeAmplitude, 0f, 0f);
            }

            if (_collapseLeft > 0f)
            {
                _collapseLeft = Mathf.Max(0f, _collapseLeft - seconds);
                _body.transform.localScale = _bodyScale * (_collapseLeft / _collapseTotal);
            }

            for (var i = _shots.Count - 1; i >= 0; i--)
            {
                var shot = _shots[i];
                shot.Elapsed += seconds;
                if (shot.Elapsed >= shot.Total)
                {
                    Destroy(shot.Renderer.gameObject);
                    _shots.RemoveAt(i);
                    continue;
                }
                var t = shot.Elapsed / shot.Total;
                shot.Renderer.transform.position = new Vector3(Mathf.Lerp(shot.FromX, shot.ToX, t), Mathf.Lerp(shotHeight, thunderHeight, t), 0f);
                _shots[i] = shot;
            }

            for (var i = _thunders.Count - 1; i >= 0; i--)
            {
                var effect = _thunders[i];
                effect.Elapsed += seconds;
                if (effect.Elapsed >= effect.Total)
                {
                    Destroy(effect.Renderer.gameObject);
                    _thunders.RemoveAt(i);
                    continue;
                }
                var size = Mathf.Lerp(thunderSize * 0.5f, thunderSize, effect.Elapsed / effect.Total);
                effect.Renderer.transform.localScale = Scale(effect.Renderer.sprite, thunderSprite ? thunderContent : FullImage, size);
                _thunders[i] = effect;
            }
        }

        public void ClearEffects()
        {
            foreach (var shot in _shots) Destroy(shot.Renderer.gameObject);
            _shots.Clear();
            foreach (var effect in _thunders) Destroy(effect.Renderer.gameObject);
            _thunders.Clear();
        }

        private Color BodyColor
        {
            get
            {
                var whole = castleSprite ? Color.white : castleColor;
                // Without a damaged picture the castle is drawn darker under half health.
                return _damaged && !damagedSprite ? whole * damagedTint : whole;
            }
        }

        private static readonly Rect FullImage = new Rect(0f, 0f, 1f, 1f);

        private SpriteRenderer MakeEffect(string effectName, Sprite sprite, Rect content, Color placeholderColor, float size)
        {
            var effectObject = new GameObject(effectName);
            effectObject.transform.SetParent(transform, false);
            var renderer = effectObject.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite ? sprite : Placeholder();
            renderer.color = sprite ? Color.white : placeholderColor;
            renderer.sortingOrder = 10;
            renderer.transform.localScale = Scale(renderer.sprite, sprite ? content : FullImage, size);
            return renderer;
        }

        /// <summary>Uniform scale that makes the drawn part's longer side <paramref name="size"/> units.</summary>
        private static Vector3 Scale(Sprite sprite, Rect content, float size)
        {
            var drawn = Drawn(sprite, content);
            var largest = Mathf.Max(drawn.width, drawn.height, 1e-4f);
            return Vector3.one * (size / largest);
        }

        /// <summary>The drawn part of the sprite in its local units (before scaling).</summary>
        private static Rect Drawn(Sprite sprite, Rect content)
        {
            var bounds = sprite.bounds;
            var size = bounds.size;
            return new Rect(
                bounds.min.x + content.x * size.x, bounds.min.y + content.y * size.y,
                content.width * size.x, content.height * size.y);
        }

        private Sprite Placeholder()
        {
            if (!_placeholder)
                _placeholder = Sprite.Create(Texture2D.whiteTexture, new Rect(0f, 0f, 4f, 4f), new Vector2(0.5f, 0.5f), 4f);
            return _placeholder;
        }

        private struct Shot
        {
            public SpriteRenderer Renderer;
            public int Handle;
            public float FromX;
            public float ToX;
            public float Elapsed;
            public float Total;
        }

        private struct Effect
        {
            public SpriteRenderer Renderer;
            public float Elapsed;
            public float Total;
        }
    }
}
