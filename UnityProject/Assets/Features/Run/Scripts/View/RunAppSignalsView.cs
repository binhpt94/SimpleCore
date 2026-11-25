using System;
using UnityEngine;

namespace Game.Run
{
    /// <summary>
    /// Hears the device for the run: the game leaving the screen (app pause, focus lost) and the back key.
    /// Only passes the signal on; the run decides what it means. Disposing destroys the object.
    /// </summary>
    internal sealed class RunAppSignalsView : MonoBehaviour, IAppSignals
    {
        public event Action LeftApp;
        public event Action BackPressed;

        private void Update()
        {
            // Android maps the back key to Escape (old input manager, as set in the project).
            if (Input.GetKeyDown(KeyCode.Escape)) BackPressed?.Invoke();
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused) LeftApp?.Invoke();
        }

        private void OnApplicationFocus(bool focused)
        {
            if (!focused) LeftApp?.Invoke();
        }

        public void Dispose()
        {
            LeftApp = null;
            BackPressed = null;
            if (this != null) Destroy(gameObject);
        }
    }
}
