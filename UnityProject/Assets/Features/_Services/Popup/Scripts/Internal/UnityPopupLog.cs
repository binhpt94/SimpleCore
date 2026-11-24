using UnityEngine;

namespace Game.Services.Popup
{
    internal sealed class UnityPopupLog : IPopupLog
    {
        public void Warning(string message) => Debug.LogWarning(message);

        public void Error(string message) => Debug.LogError(message);
    }
}
