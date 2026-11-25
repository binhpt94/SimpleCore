using System;

namespace Game.PopupPause
{
    /// <summary>
    /// The pause popup prefab as the presenter sees it: shows one step and reports what the player pressed.
    /// It decides nothing.
    /// </summary>
    internal interface IPausePopupView
    {
        /// <summary>Main step: "Tiếp tục".</summary>
        event Action ResumePressed;

        /// <summary>Main step: "Bỏ lượt".</summary>
        event Action QuitPressed;

        /// <summary>Confirm step: "Bỏ lượt".</summary>
        event Action ConfirmQuitPressed;

        /// <summary>Confirm step: "Không".</summary>
        event Action CancelQuitPressed;

        /// <summary>The device back key was pressed while the popup is shown.</summary>
        event Action BackPressed;

        /// <summary>Shows the parts of <paramref name="step"/> and hides the other step.</summary>
        void ShowStep(PausePopupStep step);

        /// <summary>False once the choice was reported: no button takes a tap any more.</summary>
        void SetInteractable(bool interactable);
    }
}
