using System;
using UnityEngine;

namespace Game.SceneFlow
{
    /// <summary>Scene names, player-facing text and timings. Read-only at runtime.</summary>
    internal sealed class SceneFlowConfig : ScriptableObject
    {
        [SerializeField] private string loadingScene;
        [SerializeField] private string gameScene;
        [SerializeField] private string loadFailedMessage;
        [Tooltip("Shown when one feature fails to load; {0} is replaced by the feature's name.")]
        [SerializeField] private string featureFailedMessage;
        [SerializeField, Min(0f)] private float minLoadingScreenSeconds;

        internal SceneFlowSettings Settings => new SceneFlowSettings(loadingScene, gameScene, loadFailedMessage,
            featureFailedMessage, TimeSpan.FromSeconds(minLoadingScreenSeconds));
    }
}
