using System;
using UnityEngine;

namespace Game.Ads
{
    /// <summary>Unity glue: raises one frame per Update with unscaled time and whether the game is on screen. No rules.</summary>
    internal sealed class AdsFrameView : MonoBehaviour, IAdsFrames
    {
        private bool _paused;
        private bool _focused = true;

        public event Action<float, bool> Frame;

        private void Awake() => _focused = Application.isFocused;

        private void Update() => Frame?.Invoke(Time.unscaledDeltaTime, !_paused && _focused);

        private void OnApplicationPause(bool paused) => _paused = paused;

        private void OnApplicationFocus(bool focused) => _focused = focused;
    }
}
