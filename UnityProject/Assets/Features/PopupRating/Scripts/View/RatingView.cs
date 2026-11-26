using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.PopupRating
{
    /// <summary>
    /// Root of the rating invite prefab: shows the line and button labels it is given, reports touches on
    /// "Đánh giá" and "Để sau", and the device back key. Whether a touch counts is decided in Internal.
    /// </summary>
    internal sealed class RatingView : MonoBehaviour, IRatingView
    {
        [SerializeField] private TMP_Text _inviteText;
        [SerializeField] private Button _rate;
        [SerializeField] private TMP_Text _rateLabel;
        [SerializeField] private Button _later;
        [SerializeField] private TMP_Text _laterLabel;

        public event Action RatePressed;
        public event Action LaterPressed;
        public event Action BackPressed;

        private void Awake()
        {
            if (_rate != null) _rate.onClick.AddListener(OnRateClicked);
            if (_later != null) _later.onClick.AddListener(OnLaterClicked);
        }

        private void OnDestroy()
        {
            if (_rate != null) _rate.onClick.RemoveListener(OnRateClicked);
            if (_later != null) _later.onClick.RemoveListener(OnLaterClicked);
        }

        // Android's back key arrives as Escape.
        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape)) BackPressed?.Invoke();
        }

        public void ShowText(string inviteText, string rateLabel, string laterLabel)
        {
            if (_inviteText != null) _inviteText.text = inviteText;
            if (_rateLabel != null) _rateLabel.text = rateLabel;
            if (_laterLabel != null) _laterLabel.text = laterLabel;
        }

        public void LockButtons()
        {
            if (_rate != null) _rate.interactable = false;
            if (_later != null) _later.interactable = false;
        }

        private void OnRateClicked() => RatePressed?.Invoke();

        private void OnLaterClicked() => LaterPressed?.Invoke();
    }
}
