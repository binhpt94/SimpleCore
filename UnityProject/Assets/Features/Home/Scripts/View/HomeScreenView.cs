using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Game.Home
{
    /// <summary>The built start screen: writes the texts it is given and forwards a tap on Play.</summary>
    internal sealed class HomeScreenView : IHomeScreenDisplay
    {
        private readonly GameObject _root;
        private readonly Dictionary<HomeElementId, TextMeshProUGUI> _texts = new Dictionary<HomeElementId, TextMeshProUGUI>();

        public HomeScreenView(GameObject root)
        {
            _root = root ? root : throw new ArgumentNullException(nameof(root));
        }

        public event Action PlayPressed;

        internal void AddLabel(HomeElementId id, TextMeshProUGUI text) => _texts[id] = text;

        internal void AddButton(HomeElementId id, TextMeshProUGUI label) => _texts[id] = label;

        internal void ReportPlayPressed() => PlayPressed?.Invoke();

        public void Show(HomeScreenContent content)
        {
            if (!_root) return;
            Set(HomeElementId.Title, content.Title);
            Set(HomeElementId.Record, content.Record);
            Set(HomeElementId.Play, content.PlayLabel);
            _root.SetActive(true);
        }

        public void Hide()
        {
            if (_root) _root.SetActive(false);
        }

        public void Dispose()
        {
            PlayPressed = null;
            if (!_root) return;
            if (Application.isPlaying) Object.Destroy(_root);
            else Object.DestroyImmediate(_root);
        }

        private void Set(HomeElementId id, string value)
        {
            if (_texts.TryGetValue(id, out var text)) text.text = value;
        }
    }
}
