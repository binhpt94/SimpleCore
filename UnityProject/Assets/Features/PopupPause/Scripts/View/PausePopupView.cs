using System;
using UnityEngine;
using UnityEngine.UI;

namespace Game.PopupPause
{
    /// <summary>
    /// Root of the pause popup prefab. Shows one of the two step groups and forwards taps and the device
    /// back key. Texts live in the prefab; nothing is decided here.
    /// </summary>
    public sealed class PausePopupView : MonoBehaviour, IPausePopupView
    {
        [SerializeField] private GameObject mainStep;
        [SerializeField] private GameObject confirmStep;
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button quitButton;
        [SerializeField] private Button confirmQuitButton;
        [SerializeField] private Button cancelQuitButton;

        private event Action ResumePressed;
        private event Action QuitPressed;
        private event Action ConfirmQuitPressed;
        private event Action CancelQuitPressed;
        private event Action BackPressed;

        event Action IPausePopupView.ResumePressed { add => ResumePressed += value; remove => ResumePressed -= value; }
        event Action IPausePopupView.QuitPressed { add => QuitPressed += value; remove => QuitPressed -= value; }
        event Action IPausePopupView.ConfirmQuitPressed { add => ConfirmQuitPressed += value; remove => ConfirmQuitPressed -= value; }
        event Action IPausePopupView.CancelQuitPressed { add => CancelQuitPressed += value; remove => CancelQuitPressed -= value; }
        event Action IPausePopupView.BackPressed { add => BackPressed += value; remove => BackPressed -= value; }

        private void Awake()
        {
            resumeButton.onClick.AddListener(() => ResumePressed?.Invoke());
            quitButton.onClick.AddListener(() => QuitPressed?.Invoke());
            confirmQuitButton.onClick.AddListener(() => ConfirmQuitPressed?.Invoke());
            cancelQuitButton.onClick.AddListener(() => CancelQuitPressed?.Invoke());
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape)) BackPressed?.Invoke();
        }

        void IPausePopupView.ShowStep(PausePopupStep step)
        {
            mainStep.SetActive(step == PausePopupStep.Main);
            confirmStep.SetActive(step == PausePopupStep.ConfirmQuit);
        }

        void IPausePopupView.SetInteractable(bool interactable)
        {
            resumeButton.interactable = interactable;
            quitButton.interactable = interactable;
            confirmQuitButton.interactable = interactable;
            cancelQuitButton.interactable = interactable;
        }
    }
}
