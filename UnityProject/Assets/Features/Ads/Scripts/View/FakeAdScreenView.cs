using UnityEngine;

namespace Game.Ads
{
    /// <summary>
    /// Dev-only stand-in for an ad: covers the screen with a plain panel while the fake provider "plays". Drawn with
    /// IMGUI so the prefab needs nothing but this component. Shows only; the fake provider decides everything.
    /// </summary>
    internal sealed class FakeAdScreenView : MonoBehaviour, IFakeAdScreen
    {
        private bool _visible;
        private string _label = string.Empty;
        private GUIStyle _style;

        public void Show(FakeAdOutcome outcome)
        {
            _label = $"QUẢNG CÁO THỬ\n(bản dev: {outcome})";
            _visible = true;
        }

        public void Hide() => _visible = false;

        private void OnGUI()
        {
            if (!_visible) return;

            if (_style == null)
            {
                _style = new GUIStyle(GUI.skin.box)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontSize = Mathf.Max(24, Screen.height / 18),
                    wordWrap = true,
                };
            }

            GUI.depth = -1000;
            var full = new Rect(0f, 0f, Screen.width, Screen.height);
            GUI.Box(full, GUIContent.none);
            GUI.Box(full, GUIContent.none);
            GUI.Box(full, _label, _style);
        }
    }
}
