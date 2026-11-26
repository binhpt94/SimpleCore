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

        [Header("Entrance (the boss coming on screen)")]
        [SerializeField] private float _entranceScale = 1.2f;
        [SerializeField] private float _entranceSeconds = 0.5f;

        [Header("Shot of a ranged enemy")]
        [SerializeField] private float _shotSeconds = 0.15f;
        [SerializeField] private Vector2 _shotSize = new Vector2(0.4f, 0.08f);
        [SerializeField] private Color _shotColor = new Color(0.35f, 0.2f, 0.1f, 1f);

        [Header("Coins paid by a defeated enemy")]
        [SerializeField] private float _coinRise = 1f;
        [SerializeField] private float _coinSeconds = 0.6f;
        [SerializeField] private float _coinTextHeight = 0.5f;
        [SerializeField] private Color _coinColor = new Color(1f, 0.85f, 0.2f, 1f);
        [Tooltip("Empty: Unity's built-in font.")]
        [SerializeField] private Font _coinFont;

        private const int CoinFontSize = 64;

        private readonly Dictionary<int, Drawn> _drawn = new Dictionary<int, Drawn>();
        private readonly List<Drawn> _dying = new List<Drawn>();
        private readonly List<Flying> _shots = new List<Flying>();
        private readonly List<Flying> _coins = new List<Flying>();
        private Sprite _boxSprite;
        private bool _frozen;
        private Func<UnityEngine.Object, IDisposable> _trackView;
        private IDisposable _stageHandle;

        public void TrackWith(Func<UnityEngine.Object, IDisposable> trackView)
        {
            _trackView = trackView ?? throw new ArgumentNullException(nameof(trackView));
            _stageHandle?.Dispose();
            _stageHandle = trackView(gameObject);
        }

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
            go.transform.localScale = baseScale;
            var drawn = new Drawn(go, Track(go), renderer, baseScale, renderer.color);
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

        public void Shoot(EnemyShot shot)
        {
            var go = new GameObject($"Enemy shot #{shot.Handle}");
            go.transform.SetParent(transform, false);
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = BoxSprite();
            renderer.color = _shotColor;
            renderer.sortingOrder = _sortingOrder + 1;
            go.transform.localScale = new Vector3(_shotSize.x, _shotSize.y, 1f);
            var from = new Vector3(shot.FromX, shot.Y - _shotSize.y * 0.5f, 0f);
            var to = new Vector3(shot.ToX, from.y, 0f);
            go.transform.localPosition = from;
            _shots.Add(new Flying(go, Track(go), from, to, _shotSeconds));
        }

        public void ShowCoins(EnemyReward reward)
        {
            var go = new GameObject($"Enemy coins +{reward.Coins}");
            go.transform.SetParent(transform, false);
            var text = go.AddComponent<TextMesh>();
            var font = _coinFont != null ? _coinFont : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.font = font;
            text.fontSize = CoinFontSize;
            text.characterSize = _coinTextHeight / (CoinFontSize * 0.1f);
            text.anchor = TextAnchor.LowerCenter;
            text.alignment = TextAlignment.Center;
            text.color = _coinColor;
            text.text = "+" + reward.Coins;
            var mesh = go.GetComponent<MeshRenderer>();
            if (font != null) mesh.sharedMaterial = font.material;
            mesh.sortingOrder = _sortingOrder + 2;

            var from = new Vector3(reward.X, reward.Top, 0f);
            go.transform.localPosition = from;
            _coins.Add(new Flying(go, Track(go), from, from + Vector3.up * _coinRise, _coinSeconds));
        }

        public void Flash(int handle)
        {
            if (_drawn.TryGetValue(handle, out var drawn)) drawn.FlashLeft = _flashSeconds;
        }

        public void Enter(int handle)
        {
            if (_drawn.TryGetValue(handle, out var drawn)) drawn.EntranceLeft = _entranceSeconds;
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
            foreach (var drawn in _drawn.Values) Release(drawn.Go, drawn.Handle);
            _drawn.Clear();
            for (var i = 0; i < _dying.Count; i++) Release(_dying[i].Go, _dying[i].Handle);
            _dying.Clear();
            ClearFlying(_shots);
            ClearFlying(_coins);
        }

        public void SetFrozen(bool frozen) => _frozen = frozen;

        public void Close()
        {
            if (this == null) return;
            RemoveAll();
            _stageHandle?.Dispose();
            _stageHandle = null;
            DestroyObject(gameObject);
        }

        // Display only: animates the looks the presenter pushed after the last clock step; never moves an enemy.
        private void Update()
        {
            var dt = Time.deltaTime;
            if (_frozen) return;

            foreach (var drawn in _drawn.Values) Animate(drawn, dt);

            for (var i = _dying.Count - 1; i >= 0; i--)
            {
                var drawn = _dying[i];
                drawn.DefeatLeft -= dt;
                if (drawn.DefeatLeft <= 0f)
                {
                    Release(drawn.Go, drawn.Handle);
                    _dying.RemoveAt(i);
                    continue;
                }

                var t = Mathf.Clamp01(drawn.DefeatLeft / Mathf.Max(_defeatSeconds, 0.001f));
                drawn.Go.transform.localScale = drawn.BaseScale * t;
            }

            Fly(_shots, dt);
            Fly(_coins, dt);
        }

        /// <summary>Moves each flying thing from its start to its end in its seconds, then removes it.</summary>
        private static void Fly(List<Flying> flying, float dt)
        {
            for (var i = flying.Count - 1; i >= 0; i--)
            {
                var item = flying[i];
                item.Elapsed += dt;
                if (item.Elapsed >= item.Seconds)
                {
                    Release(item.Go, item.Handle);
                    flying.RemoveAt(i);
                    continue;
                }

                item.Go.transform.localPosition = Vector3.Lerp(item.From, item.To, item.Elapsed / item.Seconds);
            }
        }

        private static void ClearFlying(List<Flying> flying)
        {
            for (var i = 0; i < flying.Count; i++) Release(flying[i].Go, flying[i].Handle);
            flying.Clear();
        }

        private void OnDestroy()
        {
            _stageHandle?.Dispose();
            _stageHandle = null;
            if (_boxSprite != null) DestroyObject(_boxSprite);
        }

        private IDisposable Track(GameObject go) => _trackView?.Invoke(go);

        /// <summary>Stops counting <paramref name="go"/> for the session, then destroys it.</summary>
        private static void Release(GameObject go, IDisposable handle)
        {
            handle?.Dispose();
            DestroyObject(go);
        }

        // Outside Play (EditMode tests) Destroy is not allowed; the object goes at once instead.
        private static void DestroyObject(UnityEngine.Object target)
        {
            if (target == null) return;
            if (Application.isPlaying) Destroy(target);
            else DestroyImmediate(target);
        }

        private void Animate(Drawn drawn, float dt)
        {
            drawn.BobTime = drawn.Walking ? drawn.BobTime + dt : 0f;
            drawn.LungeLeft = Mathf.Max(0f, drawn.LungeLeft - dt);
            drawn.FlashLeft = Mathf.Max(0f, drawn.FlashLeft - dt);
            drawn.EntranceLeft = Mathf.Max(0f, drawn.EntranceLeft - dt);

            // Entrance: grow to the entrance scale and back in its seconds (from the feet: pivot at the bottom).
            var grow = 1f;
            if (drawn.EntranceLeft > 0f && _entranceSeconds > 0f)
            {
                var phase = 1f - drawn.EntranceLeft / _entranceSeconds;
                grow = 1f + (_entranceScale - 1f) * Mathf.Sin(phase * Mathf.PI);
            }

            drawn.Go.transform.localScale = drawn.BaseScale * grow;

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

        private sealed class Flying
        {
            public Flying(GameObject go, IDisposable handle, Vector3 from, Vector3 to, float seconds)
            {
                Go = go;
                Handle = handle;
                From = from;
                To = to;
                Seconds = Mathf.Max(seconds, 0.001f);
            }

            public GameObject Go { get; }
            public IDisposable Handle { get; }
            public Vector3 From { get; }
            public Vector3 To { get; }
            public float Seconds { get; }
            public float Elapsed { get; set; }
        }

        private sealed class Drawn
        {
            public Drawn(GameObject go, IDisposable handle, SpriteRenderer renderer, Vector3 baseScale, Color baseColor)
            {
                Go = go;
                Handle = handle;
                Renderer = renderer;
                BaseScale = baseScale;
                BaseColor = baseColor;
            }

            public GameObject Go { get; }
            public IDisposable Handle { get; }
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
            public float EntranceLeft { get; set; }
            public float DefeatLeft { get; set; }
        }
    }
}
