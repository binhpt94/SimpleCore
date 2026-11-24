using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Hud
{
    /// <summary>
    /// The battle HUD (IDEA level): builds its own canvas from the layout frame of
    /// feature-hud/hud-layout/layout-spec (1920x1080 reference, top band 140 px, bottom band 180 px).
    /// Each part is anchored to its edge or corner so it keeps its place on wider screens. Shows only; no rules.
    /// </summary>
    internal sealed class HudView : MonoBehaviour, IHudView
    {
        private const float RefWidth = 1920f;
        private const float RefHeight = 1080f;
        private const float SkillSize = 140f;
        private const float SkillGap = 20f;
        private const int SortingOrder = 10;

        private static readonly Color PanelColor = new Color(0f, 0f, 0f, 0.45f);
        private static readonly Color HealthBack = new Color(0.15f, 0.05f, 0.05f, 0.85f);
        private static readonly Color HealthFill = new Color(0.85f, 0.2f, 0.2f, 1f);
        private static readonly Color CoinColor = new Color(1f, 0.8f, 0.15f, 1f);
        private static readonly Color SkillReady = new Color(0.25f, 0.45f, 0.85f, 1f);
        private static readonly Color SkillCooling = new Color(0.25f, 0.28f, 0.35f, 1f);
        private static readonly Color CoverColor = new Color(0f, 0f, 0f, 0.55f);

        private readonly List<SkillButton> _skills = new List<SkillButton>();
        private Canvas _canvas;
        private RectTransform _root;
        private Font _font;
        private Text _coins;
        private Text _health;
        private RectTransform _healthFill;
        private Text _wave;
        private Text _banner;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private GameObject _cheatButton;
#endif

        public event Action<int> SkillPressed;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        public event Action CheatPressed;
#endif

        private void Awake() => Build();

        public void SetVisible(bool visible)
        {
            Build();
            _canvas.enabled = visible;
        }

        public void ShowHealth(string text, float fill)
        {
            Build();
            _health.text = text;
            _healthFill.anchorMax = new Vector2(Mathf.Clamp01(fill), 1f);
        }

        public void ShowCoins(string text)
        {
            Build();
            _coins.text = text;
        }

        public void ShowWave(string text)
        {
            Build();
            _wave.text = text;
        }

        public void ShowBanner(string text)
        {
            Build();
            _banner.text = text;
            _banner.gameObject.SetActive(true);
        }

        public void HideBanner()
        {
            Build();
            _banner.gameObject.SetActive(false);
        }

        public void CreateSkillButtons(IReadOnlyList<string> names)
        {
            Build();
            foreach (var old in _skills) Destroy(old.Root.gameObject);
            _skills.Clear();

            // Cluster in the bottom-right corner: x 1420..1880, y 920..1060 on the reference screen, right to left from the edge.
            var count = names.Count;
            var clusterWidth = count * SkillSize + Math.Max(0, count - 1) * SkillGap;
            var left = RefWidth - 40f - clusterWidth;
            for (var i = 0; i < count; i++)
            {
                var index = i;
                var button = new SkillButton();
                button.Root = Rect("Skill " + names[i], _root, new Vector2(1f, 0f), left + i * (SkillSize + SkillGap), 920f, SkillSize, SkillSize);
                button.Back = button.Root.gameObject.AddComponent<Image>();
                button.Back.color = SkillReady;
                var press = button.Root.gameObject.AddComponent<Button>();
                press.transition = Selectable.Transition.None;
                press.onClick.AddListener(() => SkillPressed?.Invoke(index));

                var title = Label("Name", button.Root, 0f, 90f, SkillSize, 45f, 26, TextAnchor.MiddleCenter);
                title.text = names[i];

                button.Cover = Rect("Cover", button.Root, Vector2.zero, 0f, 0f, 0f, 0f);
                button.Cover.anchorMin = Vector2.zero;
                button.Cover.anchorMax = Vector2.one;
                button.Cover.offsetMin = Vector2.zero;
                button.Cover.offsetMax = Vector2.zero;
                var cover = button.Cover.gameObject.AddComponent<Image>();
                cover.color = CoverColor;
                cover.raycastTarget = false;

                button.Seconds = Label("Seconds", button.Root, 30f, 40f, 80f, 60f, 48, TextAnchor.MiddleCenter);
                _skills.Add(button);
            }
        }

        public void ShowSkill(int index, bool ready, string seconds, float cover, float scale)
        {
            if (index < 0 || index >= _skills.Count) return;
            var button = _skills[index];
            button.Back.color = ready ? SkillReady : SkillCooling;
            button.Seconds.text = seconds;
            button.Seconds.gameObject.SetActive(!string.IsNullOrEmpty(seconds));
            var share = Mathf.Clamp01(cover);
            button.Cover.gameObject.SetActive(share > 0f);
            button.Cover.anchorMax = new Vector2(1f, share);
            button.Root.localScale = new Vector3(scale, scale, 1f);
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        public void ShowCheatButton(string label)
        {
            Build();
            if (_cheatButton != null) return;
            // Dev only, just under the pause corner; not part of the released HUD.
            var rect = Rect("Cheat", _root, new Vector2(0f, 1f), 40f, 150f, 260f, 50f);
            var back = rect.gameObject.AddComponent<Image>();
            back.color = new Color(0.6f, 0.1f, 0.6f, 0.8f);
            var press = rect.gameObject.AddComponent<Button>();
            press.onClick.AddListener(() => CheatPressed?.Invoke());
            var text = Label("Label", rect, 0f, 0f, 260f, 50f, 22, TextAnchor.MiddleCenter);
            text.text = label;
            _cheatButton = rect.gameObject;
        }
#endif

        public void Destroy()
        {
            if (this != null) Destroy(gameObject);
        }

        private void Build()
        {
            if (_canvas != null) return;

            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _canvas = gameObject.GetComponent<Canvas>();
            if (_canvas == null) _canvas = gameObject.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = SortingOrder;
            var scaler = gameObject.GetComponent<CanvasScaler>();
            if (scaler == null) scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(RefWidth, RefHeight);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 1f;
            if (gameObject.GetComponent<GraphicRaycaster>() == null) gameObject.AddComponent<GraphicRaycaster>();
            _root = (RectTransform)transform;

            // Money: coin icon + number, top band, left of the health bar.
            var coin = Rect("Coin", _root, new Vector2(0f, 1f), 220f, 10f, 60f, 60f);
            var coinImage = coin.gameObject.AddComponent<Image>();
            coinImage.color = CoinColor;
            coinImage.raycastTarget = false;
            var coinMark = Label("Mark", coin, 0f, 0f, 60f, 60f, 26, TextAnchor.MiddleCenter);
            coinMark.text = "xu";
            coinMark.color = new Color(0.35f, 0.22f, 0f, 1f);
            _coins = Label("Coins", _root, new Vector2(0f, 1f), 290f, 10f, 230f, 60f, 44, TextAnchor.MiddleLeft);

            // Castle health: centre of the top band, right above the castle.
            var bar = Rect("Health", _root, new Vector2(0.5f, 1f), 660f, 10f, 600f, 60f);
            var back = bar.gameObject.AddComponent<Image>();
            back.color = HealthBack;
            back.raycastTarget = false;
            _healthFill = Rect("Fill", bar, Vector2.zero, 0f, 0f, 0f, 0f);
            _healthFill.anchorMin = Vector2.zero;
            _healthFill.anchorMax = Vector2.one;
            _healthFill.offsetMin = Vector2.zero;
            _healthFill.offsetMax = Vector2.zero;
            var fill = _healthFill.gameObject.AddComponent<Image>();
            fill.color = HealthFill;
            fill.raycastTarget = false;
            _health = Label("Number", bar, 180f, 5f, 240f, 50f, 38, TextAnchor.MiddleCenter);

            // Wave number: right end of the top band.
            _wave = Label("Wave", _root, new Vector2(1f, 1f), 1580f, 10f, 300f, 60f, 44, TextAnchor.MiddleRight);

            // "Đợt N" banner: in the top band, under the health bar; never blocks taps.
            _banner = Label("Banner", _root, new Vector2(0.5f, 1f), 760f, 76f, 400f, 56f, 44, TextAnchor.MiddleCenter);
            _banner.color = new Color(1f, 0.92f, 0.5f, 1f);
            _banner.gameObject.SetActive(false);
        }

        /// <summary>
        /// A rect placed by reference-screen coordinates (top-left origin, y down) and kept on the given anchor,
        /// so it follows its edge or corner when the screen is wider than 16:9.
        /// </summary>
        private RectTransform Rect(string name, RectTransform parent, Vector2 anchor, float x, float y, float width, float height)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = gameObject.layer;
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = new Vector2(0f, 1f);
            rect.sizeDelta = new Vector2(width, height);
            if (parent == _root)
                rect.anchoredPosition = new Vector2(x - anchor.x * RefWidth, -(y - (1f - anchor.y) * RefHeight));
            else
                rect.anchoredPosition = new Vector2(x, -y);
            return rect;
        }

        private Text Label(string name, RectTransform parent, float x, float y, float width, float height, int size, TextAnchor align)
        {
            return Label(name, parent, new Vector2(0f, 1f), x, y, width, height, size, align);
        }

        private Text Label(string name, RectTransform parent, Vector2 anchor, float x, float y, float width, float height, int size, TextAnchor align)
        {
            var rect = Rect(name, parent, anchor, x, y, width, height);
            var text = rect.gameObject.AddComponent<Text>();
            text.font = _font;
            text.fontSize = size;
            text.alignment = align;
            text.color = Color.white;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            var outline = rect.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.8f);
            outline.effectDistance = new Vector2(2f, -2f);
            return text;
        }

        private sealed class SkillButton
        {
            public RectTransform Root;
            public Image Back;
            public RectTransform Cover;
            public Text Seconds;
        }
    }
}
