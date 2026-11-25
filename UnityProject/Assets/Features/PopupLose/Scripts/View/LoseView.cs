using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.PopupLose
{
    /// <summary>
    /// Root of the lose popup prefab: shows the lines it is given, pulses the "Kỉ lục mới!" band while it is shown,
    /// and reports touches on "Chơi lại" and "Về màn bắt đầu".
    /// </summary>
    internal sealed class LoseView : MonoBehaviour, ILoseView
    {
        [SerializeField] private TMP_Text _title;
        [SerializeField] private TMP_Text _score;
        [SerializeField] private TMP_Text _best;
        [Tooltip("The band and its \"Kỉ lục mới!\" text: shown only on a new best, and pulses.")]
        [SerializeField] private RectTransform _newRecord;
        [Tooltip("Plain \"Quảng cáo chưa hoàn tất\" in the place of the new-record line, no band.")]
        [SerializeField] private GameObject _adFailed;
        [SerializeField] private Button _replay;
        [SerializeField] private Button _home;

        private float _pulseSeconds;

        public event Action ReplayPressed;
        public event Action HomePressed;

        private void Awake()
        {
            if (_replay != null) _replay.onClick.AddListener(OnReplayClicked);
            if (_home != null) _home.onClick.AddListener(OnHomeClicked);
        }

        private void OnDestroy()
        {
            if (_replay != null) _replay.onClick.RemoveListener(OnReplayClicked);
            if (_home != null) _home.onClick.RemoveListener(OnHomeClicked);
        }

        // Unscaled time: the popup opens while the battle stands still.
        private void Update()
        {
            if (_newRecord == null || !_newRecord.gameObject.activeSelf) return;
            _pulseSeconds += Time.unscaledDeltaTime;
            var scale = NewRecordPulse.ScaleAt(_pulseSeconds);
            _newRecord.localScale = new Vector3(scale, scale, 1f);
        }

        public void ShowResult(LoseResultText text)
        {
            if (_title != null) _title.text = text.Title;
            if (_score != null) _score.text = text.Score;
            if (_best != null) _best.text = text.Best;
            if (_newRecord != null)
            {
                _pulseSeconds = 0f;
                _newRecord.localScale = Vector3.one;
                _newRecord.gameObject.SetActive(text.ShowNewRecord);
            }
            if (_adFailed != null) _adFailed.SetActive(text.ShowAdFailed);
        }

        public void LockButtons()
        {
            if (_replay != null) _replay.interactable = false;
            if (_home != null) _home.interactable = false;
        }

        private void OnReplayClicked() => ReplayPressed?.Invoke();

        private void OnHomeClicked() => HomePressed?.Invoke();
    }
}
