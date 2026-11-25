using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.PopupRevive
{
    /// <summary>
    /// The revive popup prefab's root. Shows the words and the waiting state the presenter gives and forwards taps.
    /// Layout (revive-spec frame main): panel 800x700; title y=40 h=90, gain line y=160 h=70, waiting line y=340
    /// h=70; "Xem quảng cáo" x=50 y=540 400x120, "Kết thúc lượt" x=490 y=540 260x120.
    /// </summary>
    internal sealed class RevivePopupView : MonoBehaviour, IRevivePopupView
    {
        [SerializeField] private TMP_Text title;
        [SerializeField] private TMP_Text gain;
        [SerializeField] private TMP_Text waiting;
        [SerializeField] private Button watchButton;
        [SerializeField] private TMP_Text watchLabel;
        [SerializeField] private Button endButton;
        [SerializeField] private TMP_Text endLabel;
        [SerializeField] private CanvasGroup buttons;
        [SerializeField, Range(0f, 1f)] private float lockedAlpha = 0.4f;

        public event Action WatchTapped;
        public event Action EndTapped;

        private void Awake()
        {
            watchButton.onClick.AddListener(() => WatchTapped?.Invoke());
            endButton.onClick.AddListener(() => EndTapped?.Invoke());
        }

        public void SetTexts(string titleText, string gainText, string waitingText, string watchText, string endText)
        {
            title.text = titleText;
            gain.text = gainText;
            waiting.text = waitingText;
            watchLabel.text = watchText;
            endLabel.text = endText;
        }

        public void SetWaiting(bool isWaiting)
        {
            waiting.gameObject.SetActive(isWaiting);
            buttons.interactable = !isWaiting;
            buttons.blocksRaycasts = !isWaiting;
            buttons.alpha = isWaiting ? lockedAlpha : 1f;
        }
    }
}
