using UnityEngine;

namespace Game.SceneFlow
{
    /// <summary>Scene names and player-facing text. Read-only at runtime.</summary>
    internal sealed class SceneFlowConfig : ScriptableObject
    {
        [SerializeField] private string loadingScene;
        [SerializeField] private string gameScene;
        [SerializeField] private string loadFailedMessage;

        internal SceneFlowSettings Settings => new SceneFlowSettings(loadingScene, gameScene, loadFailedMessage);
    }
}
