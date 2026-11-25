using UnityEngine;
using UnityEngine.UI;

namespace Game.SceneFlow
{
    /// <summary>The loading screen. It only shows what it is told to show.</summary>
    internal sealed class LoadingScreenView : MonoBehaviour, ILoadingScreen
    {
        [SerializeField] private Text errorLabel;
        [Tooltip("Image of type Filled; its fill amount is the load progress.")]
        [SerializeField] private Image progressFill;

        private bool reportedMissingBar;

        Object ILoadingScreen.Root => gameObject;

        void ILoadingScreen.ShowProgress(float fraction)
        {
            if (progressFill == null)
            {
                if (!reportedMissingBar)
                {
                    reportedMissingBar = true;
                    Debug.LogError("LoadingScreenView has no progressFill assigned; the player sees no progress bar.", this);
                }

                return;
            }

            progressFill.fillAmount = Mathf.Clamp01(fraction);
        }

        void ILoadingScreen.ShowError(string message)
        {
            // The game name and the bar (stopped where it was) stay; the error line shows under them.
            if (errorLabel == null)
            {
                Debug.LogError($"LoadingScreenView has no errorLabel assigned; the player cannot see: {message}", this);
                return;
            }

            errorLabel.text = message;
            errorLabel.gameObject.SetActive(true);
        }

        void ILoadingScreen.Close()
        {
            Destroy(gameObject);
        }
    }
}
