using UnityEngine;
using UnityEngine.UI;

namespace Game.SceneFlow
{
    /// <summary>The loading screen. It only shows what it is told to show.</summary>
    internal sealed class LoadingScreenView : MonoBehaviour, ILoadingScreen
    {
        [SerializeField] private Text errorLabel;
        [SerializeField] private GameObject loadingIndicator;

        void ILoadingScreen.ShowError(string message)
        {
            // Nothing is loading any more; leaving the indicator up would contradict the error.
            if (loadingIndicator != null) loadingIndicator.SetActive(false);

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
