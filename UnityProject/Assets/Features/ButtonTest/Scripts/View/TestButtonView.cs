using System;
using UnityEngine;
using UnityEngine.UI;

namespace Game.ButtonTest
{
    /// <summary>
    /// Nút "Thử" và nhãn "Đã bấm: N": tự dựng canvas overlay (thứ tự vẽ thấp hơn lớp popup, nên popup che và chặn chạm),
    /// đặt chỗ theo bố cục được đưa xuống, chỉ hiển thị và báo lần nhả chạm. Không tính gì.
    /// Button của Unity chỉ báo click khi nhả trên nút; kéo ra ngoài rồi nhả thì không có click, nên không có lần tính.
    /// </summary>
    [DisallowMultipleComponent]
    internal sealed class TestButtonView : MonoBehaviour, ITestButtonView
    {
        private static readonly Color ButtonColor = new Color(0.22f, 0.47f, 0.86f, 1f);
        private static readonly Color ButtonTextColor = Color.white;
        private static readonly Color CounterTextColor = Color.white;

        private Canvas _canvas;
        private Button _button;
        private Text _buttonText;
        private Text _counterText;

        public UnityEngine.Object Root => gameObject;

        public event Action<bool> Released;

        public void ApplyLayout(TestButtonLayout layout, string buttonLabel)
        {
            EnsureBuilt();

            _canvas.sortingOrder = layout.SortingOrder;
            var scaler = GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = layout.ReferenceResolution;
            scaler.matchWidthOrHeight = 0.5f;

            Place((RectTransform)_button.transform, layout.Button);
            Place(_counterText.rectTransform, layout.Counter);
            _buttonText.text = buttonLabel;
        }

        public void SetVisible(bool visible)
        {
            EnsureBuilt();
            _canvas.enabled = visible;
            _button.interactable = visible;
        }

        public void SetCounterText(string text)
        {
            EnsureBuilt();
            _counterText.text = text;
        }

        private void OnDestroy()
        {
            if (_button != null) _button.onClick.RemoveListener(OnClick);
        }

        private void OnClick() => Released?.Invoke(true);

        // Frame tính từ góc trên trái màn chuẩn: neo góc trên trái, y đi xuống.
        private static void Place(RectTransform rect, FrameRect frame)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(frame.X, -frame.Y);
            rect.sizeDelta = new Vector2(frame.Width, frame.Height);
        }

        private void EnsureBuilt()
        {
            if (_canvas != null) return;

            _canvas = GetComponent<Canvas>();
            if (_canvas == null) _canvas = gameObject.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            if (GetComponent<CanvasScaler>() == null) gameObject.AddComponent<CanvasScaler>();
            if (GetComponent<GraphicRaycaster>() == null) gameObject.AddComponent<GraphicRaycaster>();

            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            var buttonGo = new GameObject("test-button", typeof(RectTransform), typeof(Image), typeof(Button));
            buttonGo.transform.SetParent(transform, false);
            buttonGo.GetComponent<Image>().color = ButtonColor;
            _button = buttonGo.GetComponent<Button>();
            _button.onClick.AddListener(OnClick);

            _buttonText = CreateText("label", buttonGo.transform, font, ButtonTextColor, TextAnchor.MiddleCenter);
            var labelRect = _buttonText.rectTransform;
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;
            _buttonText.raycastTarget = false;

            _counterText = CreateText("test-counter", transform, font, CounterTextColor, TextAnchor.MiddleLeft);
            _counterText.raycastTarget = false;
        }

        // Chữ dài ra (N rất lớn) thì tự co trong chỗ của nó.
        private static Text CreateText(string objectName, Transform parent, Font font, Color color, TextAnchor anchor)
        {
            var go = new GameObject(objectName, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var text = go.GetComponent<Text>();
            text.font = font;
            text.color = color;
            text.alignment = anchor;
            text.fontSize = 40;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = 10;
            text.resizeTextMaxSize = 40;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            return text;
        }
    }
}
