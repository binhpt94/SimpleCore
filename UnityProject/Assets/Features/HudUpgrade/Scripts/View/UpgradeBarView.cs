using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Game.HudUpgrade
{
    /// <summary>
    /// The upgrade bar along the bottom edge (frame feature-hud-upgrade/bar-layout/bar-buttons and
    /// button-state/button-display, px on a 1920x1080 screen). Shows what the presenter sends and reports taps.
    /// </summary>
    internal sealed class UpgradeBarView : MonoBehaviour, IUpgradeBarView
    {
        [Serializable]
        private struct HeroPortrait
        {
            public string heroCode;
            public Sprite sprite;
        }

        [Header("Hình")]
        [SerializeField] private HeroPortrait[] heroPortraits = Array.Empty<HeroPortrait>();
        [SerializeField] private Sprite castleHealthIcon;
        [SerializeField] private Sprite castleAttackIcon;
        [SerializeField] private Sprite castleSkillIcon;
        [SerializeField] private Sprite coinIcon;

        [Header("Bố cục (px trên màn 1920x1080)")]
        [SerializeField] private Vector2 referenceResolution = new Vector2(1920, 1080);
        [SerializeField] private float barLeft = 50;
        [SerializeField] private float barBottom = 20;
        [SerializeField] private Vector2 buttonSize = new Vector2(200, 140);
        [SerializeField] private float buttonGap = 10;
        [SerializeField] private float groupGap = 70;
        [Tooltip("Mép trái cụm kĩ năng của feature-hud trên màn chuẩn; thanh phải kết thúc trước đó.")]
        [SerializeField] private float skillsLeft = 1420;
        [Tooltip("Khoảng hở tối thiểu giữa thanh và cụm kĩ năng (frame bar-buttons: thanh kết thúc ở 1380, cụm kĩ năng ở 1420).")]
        [SerializeField] private float skillsGap = 40;
        [SerializeField] private int sortingOrder = 10;

        [Header("Màu và chuyển động")]
        [SerializeField] private Color buttonColor = new Color(0.16f, 0.22f, 0.30f, 0.92f);
        [SerializeField] private Color textColor = Color.white;
        [SerializeField, Range(0, 1)] private float dimAlpha = 0.4f;
        [SerializeField] private float bounceSeconds = 0.2f;
        [SerializeField] private float bounceScale = 1.15f;
        [Tooltip("Số cấp mới nhấp sáng khi mua thành công (PROTOTYPE: 0,2 giây).")]
        [SerializeField] private float flashSeconds = 0.2f;
        [SerializeField] private Color flashColor = new Color(1f, 0.92f, 0.35f);
        [SerializeField] private float flashScale = 1.25f;
        [Tooltip("Rung nhẹ khi chạm nút mờ (PROTOTYPE: 0,2 giây).")]
        [SerializeField] private float shakeSeconds = 0.2f;
        [SerializeField] private float shakeDistance = 8f;
        [SerializeField] private int shakeCycles = 3;

        [Header("Hero đang gục")]
        [SerializeField] private Color downTint = new Color(0f, 0f, 0f, 0.6f);
        [SerializeField] private string downLabel = "Gục";

        private readonly List<ButtonParts> _buttons = new List<ButtonParts>();
        private RectTransform _bar;
        private int[] _groups = Array.Empty<int>();
        private Font _font;

        private sealed class ButtonParts
        {
            public RectTransform Root;
            public RectTransform Body;
            public CanvasGroup Group;
            public Text Level;
            public Text Price;
            public GameObject DownMark;
            public Coroutine Bounce;
            public Coroutine Shake;
            public Coroutine Flash;
        }

        private void Awake()
        {
            var canvas = gameObject.GetComponent<Canvas>();
            if (!canvas) canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortingOrder;
            var scaler = gameObject.GetComponent<CanvasScaler>();
            if (!scaler) scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = referenceResolution;
            scaler.matchWidthOrHeight = 1f;
            if (!gameObject.GetComponent<GraphicRaycaster>()) gameObject.AddComponent<GraphicRaycaster>();

            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _bar = NewRect("Bar", transform);
            _bar.anchorMin = _bar.anchorMax = Vector2.zero;
            _bar.pivot = Vector2.zero;
            _bar.anchoredPosition = new Vector2(barLeft, barBottom);
            _bar.sizeDelta = new Vector2(0, buttonSize.y);
            _bar.gameObject.SetActive(false);
        }

        public void Build(IReadOnlyList<ButtonPicture> buttons, Action<int> onTap)
        {
            foreach (var parts in _buttons) Destroy(parts.Root.gameObject);
            _buttons.Clear();

            _groups = new int[buttons.Count];
            for (var i = 0; i < buttons.Count; i++) _groups[i] = buttons[i].Group;
            var layout = BarLayout.Compute(_groups, CanvasWidth(), LayoutSettings());
            for (var i = 0; i < buttons.Count; i++)
                _buttons.Add(NewButton(i, buttons[i], layout.ButtonLefts[i], onTap));

            _bar.sizeDelta = new Vector2(layout.Width, buttonSize.y);
            _bar.localScale = Vector3.one * layout.Scale;
        }

        /// <summary>Screen size changed: shrink or grow the bar so it still ends left of the skill cluster.</summary>
        private void OnRectTransformDimensionsChange()
        {
            if (!_bar) return;
            var layout = BarLayout.Compute(_groups, CanvasWidth(), LayoutSettings());
            _bar.localScale = Vector3.one * layout.Scale;
        }

        private BarLayout.Settings LayoutSettings() =>
            new BarLayout.Settings(barLeft, buttonSize.x, buttonGap, groupGap, referenceResolution.x - skillsLeft + skillsGap);

        /// <summary>Canvas width in canvas units (equals 1920 on a 16:9 screen; less on a narrower one).</summary>
        private float CanvasWidth()
        {
            var root = transform as RectTransform;
            var width = root ? root.rect.width : 0f;
            return width > 0 ? width : referenceResolution.x;
        }

        public void SetButton(int index, ButtonLook look)
        {
            if (index < 0 || index >= _buttons.Count) return;
            var parts = _buttons[index];
            parts.Level.text = look.LevelText;
            parts.Price.text = look.PriceText;
            parts.Group.alpha = look.Lit ? 1f : dimAlpha;
        }

        public void Bounce(int index)
        {
            if (index < 0 || index >= _buttons.Count || !isActiveAndEnabled) return;
            var parts = _buttons[index];
            if (parts.Bounce != null) StopCoroutine(parts.Bounce);
            parts.Bounce = StartCoroutine(BounceRoutine(parts));        }

        public void FlashLevel(int index)
        {
            if (index < 0 || index >= _buttons.Count || !isActiveAndEnabled) return;
            var parts = _buttons[index];
            if (parts.Flash != null) StopCoroutine(parts.Flash);
            parts.Flash = StartCoroutine(FlashRoutine(parts));
        }

        /// <summary>The level text glows bright and swells, then settles back over <see cref="flashSeconds"/>.</summary>
        private IEnumerator FlashRoutine(ButtonParts parts)
        {
            var text = parts.Level;
            var t = 0f;
            while (t < flashSeconds)
            {
                t += Time.unscaledDeltaTime;
                var k = 1f - Mathf.Clamp01(t / flashSeconds);
                text.color = Color.Lerp(textColor, flashColor, k);
                text.rectTransform.localScale = Vector3.one * Mathf.Lerp(1f, flashScale, k);
                yield return null;
            }

            text.color = textColor;
            text.rectTransform.localScale = Vector3.one;
            parts.Flash = null;
        }

        public void Shake(int index)
        {
            if (index < 0 || index >= _buttons.Count || !isActiveAndEnabled) return;
            var parts = _buttons[index];
            if (parts.Shake != null) StopCoroutine(parts.Shake);
            parts.Shake = StartCoroutine(ShakeRoutine(parts));
        }

        public void SetDown(int index, bool down)
        {
            if (index < 0 || index >= _buttons.Count) return;
            var mark = _buttons[index].DownMark;
            if (mark) mark.SetActive(down);
        }

        public void SetVisible(bool visible)
        {
            if (_bar) _bar.gameObject.SetActive(visible);
        }

        private IEnumerator BounceRoutine(ButtonParts parts)
        {
            var t = 0f;
            while (t < bounceSeconds)
            {
                t += Time.unscaledDeltaTime;
                var k = Mathf.Sin(Mathf.Clamp01(t / bounceSeconds) * Mathf.PI);
                parts.Root.localScale = Vector3.one * Mathf.Lerp(1f, bounceScale, k);
                yield return null;
            }

            parts.Root.localScale = Vector3.one;
            parts.Bounce = null;
        }

        /// <summary>Side-to-side wobble of the button's content; the button itself keeps its place and its hit area.</summary>
        private IEnumerator ShakeRoutine(ButtonParts parts)
        {
            var t = 0f;
            while (t < shakeSeconds)
            {
                t += Time.unscaledDeltaTime;
                var k = Mathf.Clamp01(t / shakeSeconds);
                var x = Mathf.Sin(k * shakeCycles * 2f * Mathf.PI) * shakeDistance * (1f - k);
                parts.Body.anchoredPosition = new Vector2(x, 0);
                yield return null;
            }

            parts.Body.anchoredPosition = Vector2.zero;
            parts.Shake = null;
        }

        private ButtonParts NewButton(int index, ButtonPicture picture, float x, Action<int> onTap)
        {
            var root = NewRect("Button " + (index + 1), _bar);
            root.anchorMin = root.anchorMax = new Vector2(0, 0.5f);
            root.pivot = new Vector2(0.5f, 0.5f);
            root.sizeDelta = buttonSize;
            root.anchoredPosition = new Vector2(x + buttonSize.x / 2f, 0);

            // The root keeps the hit area still; the visible body inside it is what shakes.
            var hitArea = root.gameObject.AddComponent<Image>();
            hitArea.color = Color.clear;
            var button = root.gameObject.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            button.onClick.AddListener(() => onTap?.Invoke(index));
            var group = root.gameObject.AddComponent<CanvasGroup>();

            var body = NewRect("Body", root);
            body.anchorMin = Vector2.zero;
            body.anchorMax = Vector2.one;
            body.offsetMin = body.offsetMax = Vector2.zero;
            var background = body.gameObject.AddComponent<Image>();
            background.color = buttonColor;
            background.raycastTarget = false;

            // Inner layout from button-display/main.frame (top-left of a 200x140 button).
            var sprite = SpriteOf(picture);
            var pic = NewImage("Picture", body, 10, 10, 70, 70, sprite, sprite ? Color.white : FallbackColor(picture.Kind));
            if (!sprite) NewText("Caption", pic.rectTransform, 0, 0, 70, 70, picture.Caption, 16, TextAnchor.MiddleCenter);

            GameObject downMark = null;
            if (picture.Kind == ButtonPictureKind.Hero)
            {
                // Over the portrait only, so level and price stay readable while the hero is down.
                var shade = NewImage("Down", body, 10, 10, 70, 70, null, downTint);
                shade.preserveAspect = false;
                NewText("DownLabel", shade.rectTransform, 0, 0, 70, 70, downLabel, 20, TextAnchor.MiddleCenter);
                downMark = shade.gameObject;
                downMark.SetActive(false);
            }

            var level = NewText("Level", body, 90, 10, 100, 70, "", 30, TextAnchor.MiddleLeft);
            NewImage("Coin", body, 10, 90, 40, 40, coinIcon, coinIcon ? Color.white : new Color(1f, 0.8f, 0.2f));
            var price = NewText("Price", body, 60, 90, 130, 40, "", 30, TextAnchor.MiddleLeft);

            return new ButtonParts
            {
                Root = root, Body = body, Group = group, Level = level, Price = price, DownMark = downMark,
            };
        }

        private Sprite SpriteOf(ButtonPicture picture)
        {
            switch (picture.Kind)
            {
                case ButtonPictureKind.Hero:
                    foreach (var portrait in heroPortraits)
                        if (portrait.heroCode == picture.HeroCode) return portrait.sprite;
                    return null;
                case ButtonPictureKind.CastleHealth: return castleHealthIcon;
                case ButtonPictureKind.CastleAttack: return castleAttackIcon;
                default: return castleSkillIcon;
            }
        }

        private static Color FallbackColor(ButtonPictureKind kind)
        {
            switch (kind)
            {
                case ButtonPictureKind.Hero: return new Color(0.35f, 0.55f, 0.85f);
                case ButtonPictureKind.CastleHealth: return new Color(0.85f, 0.25f, 0.25f);
                case ButtonPictureKind.CastleAttack: return new Color(0.95f, 0.55f, 0.15f);
                default: return new Color(0.55f, 0.35f, 0.85f);
            }
        }

        private static RectTransform NewRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = parent.gameObject.layer;
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            return rect;
        }

        private static RectTransform Place(string name, Transform parent, float x, float y, float w, float h)
        {
            var rect = NewRect(name, parent);
            rect.anchorMin = rect.anchorMax = new Vector2(0, 1);
            rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = new Vector2(x, -y);
            rect.sizeDelta = new Vector2(w, h);
            return rect;
        }

        private static Image NewImage(string name, Transform parent, float x, float y, float w, float h, Sprite sprite, Color color)
        {
            var image = Place(name, parent, x, y, w, h).gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.preserveAspect = true;
            image.raycastTarget = false;
            return image;
        }

        private Text NewText(string name, Transform parent, float x, float y, float w, float h, string value, int size,
            TextAnchor anchor)
        {
            var text = Place(name, parent, x, y, w, h).gameObject.AddComponent<Text>();
            text.font = _font;
            text.fontSize = size;
            text.fontStyle = FontStyle.Bold;
            text.alignment = anchor;
            text.color = textColor;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = 10;
            text.resizeTextMaxSize = size;
            text.raycastTarget = false;
            text.text = value;
            return text;
        }
    }
}
