using UnityEngine;

namespace Game.Hero
{
    /// <summary>
    /// One hero on the battlefield: a sprite (or a coloured box until the art arrives) with feet at y = 0, and the
    /// minimum effects of live feature-hero/hero-list/hero-roster-values: bob while walking, lunge on each hit,
    /// flash when hit, cleave streak, blast ring, projectile; shrink when falling, grow when respawning, pop on level up.
    /// Shows only what the presenter pushes; 1 u = 1 world unit.
    /// </summary>
    internal sealed class HeroView : MonoBehaviour, IHeroDisplay
    {
        [Header("Walk bob")]
        [SerializeField] private float bobHeight = 0.1f;
        [SerializeField] private float bobPeriod = 0.4f;
        [Header("Hit lunge")]
        [SerializeField] private float lungeDistance = 0.2f;
        [SerializeField] private float lungeSeconds = 0.15f;
        [Header("Hurt flash")]
        [SerializeField] private float flashSeconds = 0.1f;
        [SerializeField] private Color flashColor = new Color(1f, 0.35f, 0.35f, 1f);
        [Header("Attack effects")]
        [SerializeField] private float cleaveSeconds = 0.2f;
        [SerializeField] private float blastSeconds = 0.3f;
        [SerializeField] private float projectileSeconds = 0.15f;
        [SerializeField] private float projectileSize = 0.25f;
        [SerializeField] private Color effectColor = new Color(1f, 0.95f, 0.6f, 0.8f);
        [Header("Level up pop")]
        [SerializeField] private float levelUpScale = 1.2f;
        [SerializeField] private float levelUpSeconds = 0.3f;
        [Header("Drawing")]
        [SerializeField] private int sortingOrder = 10;

        private static Sprite _box; // a white 1x1 u square; built once, holds no game state

        private SpriteRenderer _body;
        private SpriteRenderer _effect;
        private Color _bodyColor = Color.white;
        private Vector2 _bodySize = Vector2.one;
        private float _bodyBaseY;
        private float _width = 1f;
        private float _height = 1.5f;

        private float _x;
        private int _facing = 1;
        private bool _moving;

        private float _bobTime;
        private float _lungeLeft;
        private int _lungeFacing = 1;
        private float _flashLeft;
        private float _popLeft;
        private float _fallLeft, _fallTotal;
        private float _growLeft, _growTotal;
        private bool _hidden;

        private float _effectLeft, _effectTotal;
        private HeroStrike _strike;

        public void Setup(string heroCode, Sprite sprite, Color placeholderColor, float width, float height)
        {
            gameObject.name = $"Hero {heroCode}";
            _width = width;
            _height = height;

            _body = CreateRenderer("Body", sortingOrder);
            if (sprite != null)
            {
                _body.sprite = sprite;
                _bodyColor = Color.white;
                var size = sprite.bounds.size;
                var scale = size.y > 0f ? height / size.y : 1f;
                _bodySize = new Vector2(scale, scale);
                _bodyBaseY = -sprite.bounds.min.y * scale;
            }
            else
            {
                _body.sprite = Box();
                _bodyColor = placeholderColor;
                _bodySize = new Vector2(width, height);
                _bodyBaseY = height * 0.5f;
            }
            _body.color = _bodyColor;

            _effect = CreateRenderer("Effect", sortingOrder + 1);
            _effect.sprite = Box();
            _effect.color = effectColor;
            _effect.enabled = false;
            ApplyLook();
        }

        public void SetPose(float x, int facing, bool moving)
        {
            _x = x;
            _facing = facing < 0 ? -1 : 1;
            _moving = moving;
            ApplyLook();
        }

        public void Animate(float deltaSeconds)
        {
            if (deltaSeconds <= 0f) return;
            _bobTime = _moving ? _bobTime + deltaSeconds : 0f;
            _lungeLeft = Mathf.Max(0f, _lungeLeft - deltaSeconds);
            _flashLeft = Mathf.Max(0f, _flashLeft - deltaSeconds);
            _popLeft = Mathf.Max(0f, _popLeft - deltaSeconds);
            if (_fallLeft > 0f)
            {
                _fallLeft = Mathf.Max(0f, _fallLeft - deltaSeconds);
                if (_fallLeft <= 0f) _hidden = true;
            }
            _growLeft = Mathf.Max(0f, _growLeft - deltaSeconds);
            _effectLeft = Mathf.Max(0f, _effectLeft - deltaSeconds);
            ApplyLook();
        }

        public void PlayStrike(HeroStrike strike)
        {
            _lungeLeft = lungeSeconds;
            _lungeFacing = strike.Facing < 0 ? -1 : 1;
            _strike = strike;
            switch (strike.Attack)
            {
                case HeroAttack.FrontCleave:
                    _effectTotal = _effectLeft = cleaveSeconds;
                    break;
                case HeroAttack.Blast:
                    _effectTotal = _effectLeft = blastSeconds;
                    break;
                default:
                    _effectTotal = _effectLeft = strike.Group == HeroGroup.Ranged ? projectileSeconds : 0f;
                    break;
            }
            ApplyLook();
        }

        public void PlayHurt()
        {
            _flashLeft = flashSeconds;
            ApplyLook();
        }

        public void PlayFall(float seconds)
        {
            _growLeft = 0f;
            _flashLeft = 0f;
            _effectLeft = 0f;
            _fallTotal = _fallLeft = Mathf.Max(seconds, 0.0001f);
            ApplyLook();
        }

        public void PlayRespawn(float seconds)
        {
            _hidden = false;
            _fallLeft = 0f;
            _growTotal = _growLeft = Mathf.Max(seconds, 0.0001f);
            ApplyLook();
        }

        public void PlayLevelUp()
        {
            _popLeft = levelUpSeconds;
            ApplyLook();
        }

        public void Remove()
        {
            if (this != null) Destroy(gameObject);
        }

        private void ApplyLook()
        {
            if (_body == null) return;

            var offsetX = 0f;
            if (_lungeLeft > 0f && lungeSeconds > 0f)
                offsetX = _lungeFacing * lungeDistance * Mathf.Sin(Mathf.PI * (1f - _lungeLeft / lungeSeconds));
            var offsetY = _moving && bobPeriod > 0f ? bobHeight * Mathf.Abs(Mathf.Sin(Mathf.PI * _bobTime / bobPeriod)) : 0f;
            transform.localPosition = new Vector3(_x + offsetX, offsetY, 0f);

            var scale = 1f;
            if (_hidden) scale = 0f;
            else if (_fallLeft > 0f) scale = _fallLeft / _fallTotal;
            else if (_growLeft > 0f) scale = 1f - _growLeft / _growTotal;
            if (_popLeft > 0f && levelUpSeconds > 0f)
                scale *= 1f + (levelUpScale - 1f) * Mathf.Sin(Mathf.PI * (1f - _popLeft / levelUpSeconds));

            var body = _body.transform;
            body.localScale = new Vector3(_bodySize.x * scale, _bodySize.y * scale, 1f);
            body.localPosition = new Vector3(0f, _bodyBaseY * scale, 0f); // feet stay on the ground while scaling
            _body.flipX = _facing < 0;
            _body.color = _flashLeft > 0f ? flashColor : _bodyColor;

            ApplyEffect();
        }

        private void ApplyEffect()
        {
            if (_effect == null) return;
            if (_effectLeft <= 0f || _hidden)
            {
                _effect.enabled = false;
                return;
            }

            var progress = _effectTotal > 0f ? 1f - _effectLeft / _effectTotal : 1f;
            var t = _effect.transform;
            _effect.enabled = true;
            var color = effectColor;
            color.a *= 1f - progress * 0.6f;
            _effect.color = color;

            // The effect lives on the hero object; place it in battlefield x by undoing the hero's own position.
            var heroX = transform.localPosition.x;
            switch (_strike.Attack)
            {
                case HeroAttack.FrontCleave:
                {
                    var far = _strike.FrontX + _strike.Facing * _strike.AreaSize;
                    t.localPosition = new Vector3((_strike.FrontX + far) * 0.5f - heroX, _height * 0.5f, 0f);
                    t.localScale = new Vector3(_strike.AreaSize, 0.15f, 1f);
                    break;
                }
                case HeroAttack.Blast:
                {
                    var size = _strike.AreaSize * 2f * Mathf.Lerp(0.6f, 1f, progress);
                    t.localPosition = new Vector3(_strike.TargetX - heroX, _height * 0.4f, 0f);
                    t.localScale = new Vector3(size, size * 0.5f, 1f);
                    break;
                }
                default:
                {
                    var x = Mathf.Lerp(_strike.FrontX, _strike.TargetX, progress);
                    t.localPosition = new Vector3(x - heroX, _height * 0.6f, 0f);
                    t.localScale = new Vector3(projectileSize, projectileSize * 0.4f, 1f);
                    break;
                }
            }
        }

        private SpriteRenderer CreateRenderer(string childName, int order)
        {
            var child = new GameObject(childName);
            child.transform.SetParent(transform, false);
            var renderer = child.AddComponent<SpriteRenderer>();
            renderer.sortingOrder = order;
            return renderer;
        }

        private static Sprite Box()
        {
            if (_box != null) return _box;
            var texture = Texture2D.whiteTexture;
            _box = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), texture.width);
            _box.name = "HeroBox";
            return _box;
        }
    }
}
