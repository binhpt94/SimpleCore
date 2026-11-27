using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Game.CheatPanel
{
    /// <summary>
    /// The cheat overlay (frame feature-cheat-panel/cheat-access/access-spec wide.frame, 1920x1080): the "Cheat"
    /// button on top, and when open a shade over the whole screen (takes every tap, does nothing), the panel with its
    /// title and a vertical scroll list of rows (normal row full width, state rows in two columns). Built in code on
    /// <see cref="Bind"/>; draws what <see cref="Show"/> gets and passes taps down. No rules here.
    /// </summary>
    public sealed class CheatOverlayView : MonoBehaviour, ICheatPanelDisplay
    {
        private static readonly Vector2 Reference = new Vector2(1920, 1080);
        private static readonly Rect ButtonRect = new Rect(1396, 24, 160, 96);
        private static readonly Rect PanelRect = new Rect(40, 160, 1840, 880);
        private const float Pad = 40, TitleY = 24, TitleH = 64, ListY = 112, ListH = 728, RowH = 120, Gap = 16, ColW = 872, LabelPad = 16;

        [Tooltip("Above the HUD and every popup.")]
        [SerializeField] private int sortingOrder = 30000;
        [SerializeField] private Color buttonClosed = new Color(0.25f, 0.25f, 0.3f, 0.9f);
        [SerializeField] private Color buttonOpen = new Color(0.95f, 0.65f, 0.15f, 1f);
        [SerializeField] private Color shade = new Color(0f, 0f, 0f, 0.6f);
        [SerializeField] private Color panel = new Color(0.12f, 0.12f, 0.16f, 1f);
        [SerializeField] private Color row = new Color(0.3f, 0.32f, 0.4f, 1f);
        [SerializeField] private Color rowChosen = new Color(0.2f, 0.6f, 0.3f, 1f);
        [SerializeField] private Color rowLocked = new Color(0.22f, 0.22f, 0.24f, 1f);
        [SerializeField] private Color text = Color.white;
        [SerializeField] private Color textLocked = new Color(1f, 1f, 1f, 0.4f);

        private ICheatPanelInput _input;
        private Image _buttonImage;
        private TextMeshProUGUI _buttonLabel;
        private GameObject _shade;
        private TextMeshProUGUI _title;
        private RectTransform _content;
        private readonly List<GameObject> _rowObjects = new List<GameObject>();
        private IReadOnlyList<CheatRow> _shownRows;

        void ICheatPanelDisplay.Bind(ICheatPanelInput input)
        {
            _input = input;
            if (_buttonImage == null) Build();
        }

        void ICheatPanelDisplay.Show(CheatPanelScreen screen)
        {
            if (_buttonImage == null) Build();
            _buttonLabel.text = screen.ButtonLabel;
            _buttonImage.color = screen.IsOpen ? buttonOpen : buttonClosed;
            _shade.SetActive(screen.IsOpen);
            _title.text = screen.Title;
            if (!ReferenceEquals(_shownRows, screen.Rows)) BuildRows(screen);
            for (var i = 0; i < _rowObjects.Count - (screen.EmptyLabel != null ? 1 : 0); i++)
            {
                var go = _rowObjects[i];
                go.GetComponent<Image>().color = screen.RowsLocked ? rowLocked : i == screen.ActiveRow ? rowChosen : row;
                go.GetComponent<Button>().interactable = !screen.RowsLocked;
                go.GetComponentInChildren<TextMeshProUGUI>().color = screen.RowsLocked ? textLocked : text;
            }
        }

        private void Build()
        {
            var canvasGo = new GameObject("CheatCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortingOrder;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = Reference;
            scaler.matchWidthOrHeight = 1f;
            var root = canvasGo.transform;

            if (EventSystem.current == null)
            {
                var es = new GameObject("CheatEventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
                es.transform.SetParent(transform, false);
            }

            // Shade: full screen, catches every tap, does nothing with it.
            _shade = new GameObject("cheat-shade", typeof(RectTransform), typeof(Image));
            _shade.transform.SetParent(root, false);
            var shadeRect = (RectTransform)_shade.transform;
            shadeRect.anchorMin = Vector2.zero;
            shadeRect.anchorMax = Vector2.one;
            shadeRect.offsetMin = shadeRect.offsetMax = Vector2.zero;
            _shade.GetComponent<Image>().color = shade;

            var panelGo = Box("cheat-panel", _shade.transform, PanelRect, panel, true);
            _title = Label("panel-title", panelGo.transform, new Rect(Pad, TitleY, PanelRect.width - 2 * Pad, TitleH));

            var list = Box("state-list", panelGo.transform, new Rect(Pad, ListY, PanelRect.width - 2 * Pad, ListH), new Color(0, 0, 0, 0.25f), false);
            list.AddComponent<RectMask2D>();
            var scroll = list.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            var content = new GameObject("content", typeof(RectTransform));
            content.transform.SetParent(list.transform, false);
            _content = (RectTransform)content.transform;
            Place(_content, new Rect(0, 0, PanelRect.width - 2 * Pad, 0), false);
            scroll.content = _content;
            scroll.viewport = (RectTransform)list.transform;

            var button = Box("cheat-button", root, ButtonRect, buttonClosed, true);
            _buttonImage = button.GetComponent<Image>();
            button.AddComponent<Button>().onClick.AddListener(() => _input?.PressButton());
            _buttonLabel = Label("label", button.transform, new Rect(0, 0, ButtonRect.width, ButtonRect.height));
        }

        private void BuildRows(CheatPanelScreen screen)
        {
            foreach (var go in _rowObjects) Destroy(go);
            _rowObjects.Clear();
            _shownRows = screen.Rows;
            var width = PanelRect.width - 2 * Pad;
            var y = 0f;
            var labels = new List<TextMeshProUGUI>();
            for (var i = 0; i < screen.Rows.Count; i++)
            {
                var index = i;
                var w = i == 0 ? width : ColW;
                var go = Box(i == 0 ? "normal-row" : "state-row", _content, new Rect(0, 0, w, RowH), row, true);
                go.AddComponent<Button>().onClick.AddListener(() => _input?.PressRow(index));
                var label = Label("label", go.transform, new Rect(LabelPad, 0, w - 2 * LabelPad, RowH));
                label.fontSize = 40;
                label.overflowMode = TextOverflowModes.Overflow;
                label.text = screen.Rows[i].Label;
                labels.Add(label);
                _rowObjects.Add(go);
            }
            // Each row is as tall as its text (never cut, never shrunk); the two state rows of a line share the taller one.
            for (var i = 0; i < labels.Count; i++)
            {
                if (i == 0)
                {
                    var h = RowHeight(labels[0], width);
                    Resize(0, new Rect(0, 0, width, h));
                    y = h + Gap;
                    continue;
                }
                var hasPair = i + 1 < labels.Count;
                var lineH = RowHeight(labels[i], ColW);
                if (hasPair) lineH = Mathf.Max(lineH, RowHeight(labels[i + 1], ColW));
                Resize(i, new Rect(0, y, ColW, lineH));
                if (hasPair) Resize(i + 1, new Rect(ColW + Gap, y, ColW, lineH));
                y += lineH + Gap;
                i++;
            }
            if (screen.EmptyLabel != null)
            {
                var empty = Label("empty-label", _content, new Rect(0, y, width, RowH));
                empty.text = screen.EmptyLabel;
                y += RowH;
                _rowObjects.Add(empty.gameObject);
            }
            _content.sizeDelta = new Vector2(width, y);
        }

        private static float RowHeight(TextMeshProUGUI label, float rowWidth)
        {
            var textH = label.GetPreferredValues(label.text, rowWidth - 2 * LabelPad, 0).y;
            return Mathf.Max(RowH, Mathf.Ceil(textH) + 2 * LabelPad);
        }

        private void Resize(int index, Rect rect)
        {
            var go = _rowObjects[index];
            Place((RectTransform)go.transform, rect, false);
            var label = go.GetComponentInChildren<TextMeshProUGUI>();
            Place(label.rectTransform, new Rect(LabelPad, 0, rect.width - 2 * LabelPad, rect.height), false);
        }

        private static GameObject Box(string name, Transform parent, Rect rect, Color color, bool takesTaps)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            Place((RectTransform)go.transform, rect, parent is RectTransform p && p.GetComponent<Canvas>() != null);
            var image = go.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = takesTaps;
            return go;
        }

        private TextMeshProUGUI Label(string name, Transform parent, Rect rect)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            Place((RectTransform)go.transform, rect, false);
            var label = go.GetComponent<TextMeshProUGUI>();
            label.alignment = TextAlignmentOptions.Center;
            label.fontSize = Mathf.Min(48, rect.height * 0.6f);
            label.color = text;
            label.textWrappingMode = TextWrappingModes.Normal;
            label.overflowMode = TextOverflowModes.Ellipsis;
            label.raycastTarget = false;
            return label;
        }

        /// <summary>Px from the top-left of the parent; on the canvas, the top centre anchor keeps wider screens centred.</summary>
        private static void Place(RectTransform target, Rect rect, bool onScreen)
        {
            var anchorX = onScreen ? 0.5f : 0f;
            target.anchorMin = new Vector2(anchorX, 1);
            target.anchorMax = new Vector2(anchorX, 1);
            target.pivot = new Vector2(0, 1);
            target.anchoredPosition = new Vector2(onScreen ? rect.x - Reference.x / 2f : rect.x, -rect.y);
            target.sizeDelta = new Vector2(rect.width, rect.height);
        }
    }
}
