using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Home
{
    /// <summary>
    /// Builds the start screen in code from <see cref="HomeLayout"/>: an overlay canvas scaled from the
    /// reference screen, one text per label and one button. Only the button takes taps.
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Home/Home Screen View Factory")]
    internal sealed class HomeScreenViewFactory : HomeScreenFactoryAsset
    {
        [SerializeField] private int sortingOrder = 0;
        [SerializeField] private Color textColor = Color.white;
        [SerializeField] private Color buttonColor = new Color(0.95f, 0.65f, 0.15f, 1f);
        [SerializeField] private Color buttonTextColor = new Color(0.2f, 0.1f, 0.02f, 1f);
        [Tooltip("Font size as a share of the element's height.")]
        [SerializeField, Range(0.2f, 1f)] private float fontShareOfHeight = 0.6f;

        public override IHomeScreenDisplay Create(HomeLayout layout)
        {
            if (layout == null) throw new ArgumentNullException(nameof(layout));

            var root = new GameObject("HomeScreen", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortingOrder;
            var scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = layout.ReferenceScreen;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 1f;

            var view = new HomeScreenView(root);
            foreach (var element in layout.Elements)
            {
                switch (element.Kind)
                {
                    case HomeElementKind.Label:
                        view.AddLabel(element.Id, CreateText(element.Id.ToString(), root.transform, element.Rect, textColor, layout.ReferenceScreen.x));
                        break;
                    case HomeElementKind.Button:
                        view.AddButton(element.Id, CreateButton(element, root.transform, view, layout.ReferenceScreen.x));
                        break;
                }
            }
            root.SetActive(false);
            return view;
        }

        private TextMeshProUGUI CreateText(string objectName, Transform parent, Rect rect, Color color, float referenceWidth)
        {
            var go = new GameObject(objectName, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            Place((RectTransform)go.transform, rect, parent, referenceWidth);
            var text = go.GetComponent<TextMeshProUGUI>();
            text.alignment = TextAlignmentOptions.Center;
            text.fontSize = rect.height * fontShareOfHeight;
            text.color = color;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Overflow;
            text.raycastTarget = false;
            return text;
        }

        private TextMeshProUGUI CreateButton(HomeElement element, Transform parent, HomeScreenView view, float referenceWidth)
        {
            var go = new GameObject(element.Id.ToString(), typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            Place((RectTransform)go.transform, element.Rect, parent, referenceWidth);
            go.GetComponent<Image>().color = buttonColor;
            go.GetComponent<Button>().onClick.AddListener(view.ReportPlayPressed);

            return CreateText("Label", go.transform, new Rect(0, 0, element.Rect.width, element.Rect.height), buttonTextColor, 0f);
        }

        /// <summary>
        /// Puts <paramref name="target"/> at <paramref name="rect"/> (px from the top-left corner of the reference
        /// screen, or of the parent element). On the screen it is anchored to the top centre, so wider screens
        /// keep it centred.
        /// </summary>
        private void Place(RectTransform target, Rect rect, Transform parent, float referenceWidth)
        {
            var onScreen = referenceWidth > 0;
            var anchorX = onScreen ? 0.5f : 0f;
            target.anchorMin = new Vector2(anchorX, 1);
            target.anchorMax = new Vector2(anchorX, 1);
            target.pivot = new Vector2(0, 1);
            target.anchoredPosition = new Vector2(onScreen ? rect.x - referenceWidth / 2f : rect.x, -rect.y);
            target.sizeDelta = new Vector2(rect.width, rect.height);
        }
    }
}
