using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.TeamSelect
{
    /// <summary>
    /// The team select screen (frame slots-spec/main and card-spec/card, 1920x1080). Builds its widgets once,
    /// shows what the presenter pushes and forwards taps. No rules here.
    /// </summary>
    [RequireComponent(typeof(Canvas))]
    internal sealed class TeamSelectView : MonoBehaviour, ITeamSelectView, IHeroPortraits
    {
        [Serializable]
        internal struct Portrait
        {
            public string heroCode;
            public Sprite sprite;
        }

        [Header("Art (all optional; missing art falls back to flat colours)")]
        [SerializeField] private Sprite cardSprite;
        [SerializeField] private Sprite slotSprite;
        [SerializeField] private Sprite buttonSprite;
        [SerializeField] private Sprite pickedSprite;
        [SerializeField] private Sprite meleeSprite;
        [SerializeField] private Sprite rangedSprite;
        [SerializeField] private Portrait[] portraits = Array.Empty<Portrait>();

        [Header("Text")]
        [SerializeField] private string title = "Chọn đội";
        [SerializeField] private string startLabel = "Vào trận";
        [SerializeField] private TMP_FontAsset font;

        [Header("Colours")]
        [SerializeField] private Color background = new Color(0.12f, 0.14f, 0.2f, 1f);
        [SerializeField] private Color cardColor = new Color(0.24f, 0.28f, 0.38f, 1f);
        [SerializeField] private Color cardPickedColor = new Color(0.32f, 0.46f, 0.36f, 1f);
        [SerializeField] private Color slotColor = new Color(0.2f, 0.22f, 0.3f, 1f);
        [SerializeField] private Color startColor = new Color(0.95f, 0.66f, 0.2f, 1f);
        [SerializeField] private Color startLockedColor = new Color(0.4f, 0.4f, 0.42f, 0.6f);
        [SerializeField] private Color meleeColor = new Color(0.85f, 0.35f, 0.3f, 1f);
        [SerializeField] private Color rangedColor = new Color(0.3f, 0.55f, 0.9f, 1f);

        private sealed class CardWidgets
        {
            public string HeroCode;
            public Image Frame;
            public Image Portrait;
            public TextMeshProUGUI Name;
            public TextMeshProUGUI Style;
            public Image Role;
            public Image Picked;
        }

        private sealed class SlotWidgets
        {
            public Image Portrait;
            public TextMeshProUGUI Name;
        }

        private readonly List<CardWidgets> _cards = new List<CardWidgets>();
        private readonly List<SlotWidgets> _slots = new List<SlotWidgets>();
        private RectTransform _root;
        private RectTransform _cardRow;
        private RectTransform _slotRow;
        private Button _start;
        private Image _startImage;
        private ITeamSelectInput _input;

        public void Bind(ITeamSelectInput input) => _input = input;

        public bool Has(string heroCode) => FindPortrait(heroCode) != null;

        public void SetVisible(bool visible)
        {
            Build();
            _root.gameObject.SetActive(visible);
        }

        private void Update()
        {
            if (_input != null && _root != null && _root.gameObject.activeSelf && Input.GetKeyDown(KeyCode.Escape))
                _input.TapBack();
        }

        public void ShowCards(IReadOnlyList<HeroCard> cards)
        {
            Build();
            while (_cards.Count < cards.Count) _cards.Add(MakeCard(_cards.Count));
            for (var i = 0; i < _cards.Count; i++)
            {
                var w = _cards[i];
                var shown = i < cards.Count;
                w.Frame.gameObject.SetActive(shown);
                if (!shown) continue;
                var card = cards[i];
                w.HeroCode = card.HeroCode;
                w.Name.text = card.Name;
                w.Style.text = card.StyleLine;
                var sprite = card.HasPortrait ? FindPortrait(card.HeroCode) : null;
                w.Portrait.sprite = sprite;
                w.Portrait.enabled = sprite != null;
                var melee = card.Role == RoleMark.Melee;
                w.Role.sprite = melee ? meleeSprite : rangedSprite;
                w.Role.color = w.Role.sprite != null ? Color.white : (melee ? meleeColor : rangedColor);
                w.Picked.gameObject.SetActive(card.Picked);
                w.Frame.color = cardSprite != null ? (card.Picked ? new Color(0.8f, 1f, 0.8f) : Color.white)
                    : (card.Picked ? cardPickedColor : cardColor);
            }
        }

        public void ShowSlots(IReadOnlyList<SlotState> slots)
        {
            Build();
            while (_slots.Count < slots.Count) _slots.Add(MakeSlot(_slots.Count));
            for (var i = 0; i < _slots.Count; i++)
            {
                var w = _slots[i];
                var state = i < slots.Count ? slots[i] : default;
                var sprite = state.IsEmpty ? null : FindPortrait(state.HeroCode);
                w.Portrait.sprite = sprite;
                w.Portrait.enabled = sprite != null;
                w.Name.text = state.IsEmpty ? string.Empty : state.HeroName;
            }
        }

        public void ShowStartButton(bool enabled)
        {
            Build();
            _start.interactable = enabled;
            _startImage.color = enabled ? (buttonSprite != null ? Color.white : startColor) : startLockedColor;
        }

        private Sprite FindPortrait(string heroCode)
        {
            if (heroCode == null || portraits == null) return null;
            foreach (var p in portraits)
                if (p.heroCode == heroCode) return p.sprite;
            return null;
        }

        // ---- layout (numbers from the design frames, px on the 1920x1080 reference screen) ----

        private void Build()
        {
            if (_root != null) return;
            var canvas = GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = GetComponent<CanvasScaler>();
            if (scaler == null) scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 1f;
            if (GetComponent<GraphicRaycaster>() == null) gameObject.AddComponent<GraphicRaycaster>();

            _root = NewRect("Screen", (RectTransform)transform);
            Stretch(_root);
            var bg = _root.gameObject.AddComponent<Image>();
            bg.color = background;

            var titleText = NewText("Title", _root, title, 56, FontStyles.Bold);
            Place(titleText.rectTransform, 560, 40, 800, 80);

            _cardRow = NewRect("Cards", _root);
            Place(_cardRow, 130, 150, 1660, 400);
            _slotRow = NewRect("Slots", _root);
            Place(_slotRow, 560, 600, 800, 240);

            var start = NewRect("Start", _root);
            Place(start, 760, 890, 400, 120);
            _startImage = start.gameObject.AddComponent<Image>();
            SetSprite(_startImage, buttonSprite, true);
            _start = start.gameObject.AddComponent<Button>();
            _start.targetGraphic = _startImage;
            _start.onClick.AddListener(() => _input?.TapStart());
            var label = NewText("Label", start, startLabel, 44, FontStyles.Bold);
            Stretch(label.rectTransform);
        }

        private CardWidgets MakeCard(int index)
        {
            const float w = 300, h = 400, gap = 40;
            var rect = NewRect("Card" + (index + 1), _cardRow);
            PlaceIn(rect, index * (w + gap), 0, w, h);
            var w0 = new CardWidgets { Frame = rect.gameObject.AddComponent<Image>() };
            SetSprite(w0.Frame, cardSprite, false);
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = w0.Frame;
            button.onClick.AddListener(() => _input?.TapCard(w0.HeroCode));

            w0.Portrait = NewImage("Portrait", rect, 30, 20, 240, 200, null);
            w0.Name = NewText("Name", rect, string.Empty, 38, FontStyles.Bold);
            PlaceIn(w0.Name.rectTransform, 20, 235, 260, 50);
            w0.Style = NewText("Style", rect, string.Empty, 28, FontStyles.Normal);
            PlaceIn(w0.Style.rectTransform, 20, 290, 260, 40);
            w0.Role = NewImage("Role", rect, 20, 340, 48, 48, null);
            w0.Picked = NewImage("Picked", rect, 232, 12, 56, 56, pickedSprite);
            if (pickedSprite == null) w0.Picked.color = new Color(0.4f, 0.9f, 0.4f, 1f);
            w0.Picked.gameObject.SetActive(false);
            return w0;
        }

        private SlotWidgets MakeSlot(int index)
        {
            const float size = 240, gap = 40;
            var rect = NewRect("Slot" + (index + 1), _slotRow);
            PlaceIn(rect, index * (size + gap), 0, size, size);
            var frame = rect.gameObject.AddComponent<Image>();
            SetSprite(frame, slotSprite, false);
            if (slotSprite == null) frame.color = slotColor;
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = frame;
            var slot = index;
            button.onClick.AddListener(() => _input?.TapSlot(slot));
            var w0 = new SlotWidgets
            {
                Portrait = NewImage("Portrait", rect, 20, 10, 200, 170, null),
                Name = NewText("Name", rect, string.Empty, 30, FontStyles.Bold),
            };
            PlaceIn(w0.Name.rectTransform, 10, 185, 220, 45);
            w0.Portrait.raycastTarget = false;
            return w0;
        }

        private static RectTransform NewRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = parent.gameObject.layer;
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            return rect;
        }

        private Image NewImage(string name, RectTransform parent, float x, float y, float w, float h, Sprite sprite)
        {
            var rect = NewRect(name, parent);
            PlaceIn(rect, x, y, w, h);
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.preserveAspect = true;
            image.raycastTarget = false;
            return image;
        }

        private TextMeshProUGUI NewText(string name, RectTransform parent, string text, float size, FontStyles style)
        {
            var rect = NewRect(name, parent);
            var tmp = rect.gameObject.AddComponent<TextMeshProUGUI>();
            if (font != null) tmp.font = font;
            tmp.text = text;
            tmp.fontSize = size;
            tmp.fontStyle = style;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            tmp.raycastTarget = false;
            return tmp;
        }

        private static void SetSprite(Image image, Sprite sprite, bool sliced)
        {
            image.sprite = sprite;
            image.type = sprite != null && sliced ? Image.Type.Sliced : Image.Type.Simple;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        /// <summary>Top-left x, y and size on the 1920x1080 screen, centred horizontally for wider screens.</summary>
        private static void Place(RectTransform rect, float x, float y, float w, float h)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.sizeDelta = new Vector2(w, h);
            rect.anchoredPosition = new Vector2(x - 960f, -y);
        }

        /// <summary>Top-left x, y and size inside the parent rect.</summary>
        private static void PlaceIn(RectTransform rect, float x, float y, float w, float h)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.sizeDelta = new Vector2(w, h);
            rect.anchoredPosition = new Vector2(x, -y);
        }
    }
}
