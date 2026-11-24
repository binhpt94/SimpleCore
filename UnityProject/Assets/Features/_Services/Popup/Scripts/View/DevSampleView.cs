using System;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Services.Popup
{
    /// <summary>
    /// The sample popup of feature-popup/cheat (prefab PopupDevSample): passes the "Đóng" click down; the cheat
    /// in Internal decides to close.
    /// </summary>
    internal sealed class DevSampleView : MonoBehaviour, IDevSampleView
    {
        [SerializeField] private Button closeButton;

        public event Action CloseClicked;

        private void Awake()
        {
            if (closeButton == null) throw new InvalidOperationException($"Dev sample popup '{name}': closeButton is missing.");
            closeButton.onClick.AddListener(OnClose);
        }

        private void OnDestroy()
        {
            if (closeButton != null) closeButton.onClick.RemoveListener(OnClose);
        }

        private void OnClose() => CloseClicked?.Invoke();
    }
}
