using UnityEngine;

namespace Game.PopupPause
{
    /// <summary>Where PopupPause reports wiring errors; tests replace it.</summary>
    internal interface IPausePopupLog
    {
        void Error(string message);
    }

    internal sealed class UnityPausePopupLog : IPausePopupLog
    {
        public void Error(string message) => Debug.LogError(message);
    }
}
