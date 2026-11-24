using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.PopupLose
{
    /// <summary>Root of the lose popup prefab: shows the lines it is given and reports the "Chơi lại" touch.</summary>
    internal sealed class LoseView : MonoBehaviour, ILoseView
    {
        [SerializeField] private TMP_Text _title;
        [SerializeField] private TMP_Text _score;
        [SerializeField] private TMP_Text _best;
        [SerializeField] private GameObject _newRecord;
        [SerializeField] private Button _replay;

        public event Action ReplayPressed;

        private void Awake()
        {
            if (_replay != null) _replay.onClick.AddListener(OnReplayClicked);
        }

        private void OnDestroy()
        {
            if (_replay != null) _replay.onClick.RemoveListener(OnReplayClicked);
        }

        public void ShowResult(LoseResultText text)
        {
            if (_title != null) _title.text = text.Title;
            if (_score != null) _score.text = text.Score;
            if (_best != null) _best.text = text.Best;
            if (_newRecord != null) _newRecord.SetActive(text.ShowNewRecord);
        }

        public void LockButtons()
        {
            if (_replay != null) _replay.interactable = false;
        }

        private void OnReplayClicked() => ReplayPressed?.Invoke();
    }
}
