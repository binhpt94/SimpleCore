using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Enemy
{
    /// <summary>
    /// Draws the enemies as 2D sprites standing on the ground (y = 0, 1 world unit = 1u) and plays the shared unit
    /// motions: bob while walking, lunge when hitting, flash when hit, shrink away when defeated. Only shows what the
    /// presenter pushes; decides nothing.
    /// </summary>
    internal sealed class EnemyStageView : MonoBehaviour, IEnemyStage
    {
        [Serializable]
        private struct TypeLook
        {
            public EnemyType Type;
            [Tooltip("Sprite facing right, pivot at the bottom centre. Empty: a coloured box.")]
            public Sprite Sprite;
            public Color BoxColor;
        }

        [SerializeField] private TypeLook[] _looks = Array.Empty<TypeLook>();
        [SerializeField] private Color _flashColor = new Color(1f, 0.35f, 0.35f, 1f);
        [SerializeField] private Color _slowTint = new Color(0.6f, 0.8f, 1f, 1f);
        [SerializeField] private int _sortingOrder = 10;

        [Header("Motions (shared with heroes)")]
        [SerializeField] private float _bobHeight = 0.1f;
        [SerializeField] private float _bobPeriod = 0.4f;
        [SerializeField] private float _lungeDistance = 0.2f;
        [SerializeField] private float _lungeSeconds = 0.15f;
        [SerializeField] private float _flashSeconds = 0.1f;
        [SerializeField] private float _defeatSeconds = 0.3f;

        private readonly Dictionary<int, Drawn> _drawn = new Dictionary<int, Drawn>();
        private readonly List<Drawn> _dying = new List<Drawn>();
        private Sprite _boxSprite;
        private bool _paused;

        public event Action<float> Frame;

        public void Show(EnemyLook look)
        {
            if (_drawn.ContainsKey(look.Handle)) return;

            var go = new GameObject($"Enemy {look.Type} #{look.Handle}");
            go.transform.SetParent(transform, false);
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sortingOrder = _sortingOrder;

            var typeLook = FindLook(look.Type);
            Vector3 baseScale;
            if (typeLook.Sprite != null)
            {
                renderer.sprite = typeLook.Sprite;
                var size = typeLook.Sprite.bounds.size;
                baseScale = new Vector3(look.Width / Mathf.Max(size.x, 0.001f), look.Height / Mathf.Max(size.y, 0.001f), 1f);
                renderer.color = Color.white;
            }
            else
            {
                renderer.sprite = BoxSprite();
                baseScale = new Vector3(look.Width, look.Height, 1f);
                renderer.color = typeLook.BoxColor.a > 0f ? typeLook.BoxColor : Color.gray;
            }

            renderer.flipX = look.Direction < 0;
            var drawn = new Drawn(go, renderer, baseScale, renderer.color);
            _drawn.Add(look.Handle, drawn);
            Place(drawn, look);
        }

        public void Move(EnemyLook look)
        {
            if (_drawn.TryGetValue(look.Handle, out var drawn)) Place(drawn, look);
        }

        public void Lunge(int handle, int direction)
        {
            if (!_drawn.TryGetValue(handle, out var drawn)) return;
            drawn.LungeLeft = _lungeSeconds;
            drawn.LungeDirection = direction;
        }

        public void Flash(int handle)
        {
            if (_drawn.TryGetValue(handle, out var drawn)) drawn.FlashLeft = _flashSeconds;
        }

        public void Defeat(int handle)
        {
            if (!_drawn.TryGetValue(handle, out var drawn)) return;
            _drawn.Remove(handle);
            drawn.DefeatLeft = _defeatSeconds;
            _dying.Add(drawn);
        }

        public void RemoveAll()
        {
            foreach (var drawn in _drawn.Values) Destroy(drawn.Go);
            _drawn.Clear();
            for (var i = 0; i < _dying.Count; i++) Destroy(_dying[i].Go);
            _dying.Clear();
        }

        public void SetPaused(bool paused) => _paused = paused;

        public void Close()
        {
            if (this != null) Destroy(gameObject);
        }

        private void Update()
        {
            var dt = Time.deltaTime;
            Frame?.Invoke(dt);
            if (_paused) return;

            foreach (var drawn in _drawn.Values) Animate(drawn, dt);

            for (var i = _dying.Count - 1; i >= 0; i--)
            {
                var drawn = _dying[i];
                drawn.DefeatLeft -= dt;
                if (drawn.DefeatLeft <= 0f)
                {
                    Destroy(drawn.Go);
                    _dying.RemoveAt(i);
                    continue;
                }

                var t = Mathf.Clamp01(drawn.DefeatLeft / Mathf.Max(_defeatSeconds, 0.001f));
                drawn.Go.transform.localScale = drawn.BaseScale * t;
            }
        }

        private void OnDestroy()
        {
            if (_boxSprite != null) Destroy(_boxSprite);
        }

        private void Animate(Drawn drawn, float dt)
        {
            drawn.BobTime = drawn.Walking ? drawn.BobTime + dt : 0f;
            drawn.LungeLeft = Mathf.Max(0f, drawn.LungeLeft - dt);
            drawn.FlashLeft = Mathf.Max(0f, drawn.FlashLeft - dt);

            var bob = 0f;
            if (drawn.Walking && _bobPeriod > 0f)
            {
                var phase = (drawn.BobTime % _bobPeriod) / _bobPeriod;
                bob = Mathf.Sin(phase * Mathf.PI) * _bobHeight;
            }

            var lunge = 0f;
            if (drawn.LungeLeft > 0f && _lungeSeconds > 0f)
            {
                var phase = 1f - drawn.LungeLeft / _lungeSeconds;
                lunge = Mathf.Sin(phase * Mathf.PI) * _lungeDistance * drawn.LungeDirection;
            }

            drawn.Go.transform.localPosition = new Vector3(drawn.X + lunge, bob, 0f);

            var color = drawn.Slowed ? drawn.BaseColor * _slowTint : drawn.BaseColor;
            drawn.Renderer.color = drawn.FlashLeft > 0f ? _flashColor : color;
        }

        private static void Place(Drawn drawn, EnemyLook look)
        {
            drawn.X = look.X;
            drawn.Walking = look.Walking;
            drawn.Slowed = look.Slowed;
            drawn.Go.transform.localScale = drawn.BaseScale;
            var p = drawn.Go.transform.localPosition;
            drawn.Go.transform.localPosition = new Vector3(look.X, p.y, 0f);
        }

        private TypeLook FindLook(EnemyType type)
        {
            for (var i = 0; i < _looks.Length; i++)
            {
                if (_looks[i].Type == type) return _looks[i];
            }

            return new TypeLook { Type = type, BoxColor = Color.gray };
        }

        private Sprite BoxSprite()
        {
            if (_boxSprite != null) return _boxSprite;
            var texture = Texture2D.whiteTexture;
            _boxSprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0f),
                texture.width);
            _boxSprite.name = "EnemyBox";
            return _boxSprite;
        }

        private sealed class Drawn
        {
            public Drawn(GameObject go, SpriteRenderer renderer, Vector3 baseScale, Color baseColor)
            {
                Go = go;
                Renderer = renderer;
                BaseScale = baseScale;
                BaseColor = baseColor;
            }

            public GameObject Go { get; }
            public SpriteRenderer Renderer { get; }
            public Vector3 BaseScale { get; }
            public Color BaseColor { get; }
            public float X { get; set; }
            public bool Walking { get; set; }
            public bool Slowed { get; set; }
            public float BobTime { get; set; }
            public float LungeLeft { get; set; }
            public int LungeDirection { get; set; }
            public float FlashLeft { get; set; }
            public float DefeatLeft { get; set; }
        }
    }
}
