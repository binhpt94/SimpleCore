namespace Game.SceneFlow
{
    /// <summary>The values of SceneFlowConfig, without the Unity asset around them.</summary>
    internal readonly struct SceneFlowSettings
    {
        internal SceneFlowSettings(string loadingScene, string gameScene, string loadFailedMessage)
        {
            LoadingScene = loadingScene;
            GameScene = gameScene;
            LoadFailedMessage = loadFailedMessage;
        }

        internal string LoadingScene { get; }

        internal string GameScene { get; }

        /// <summary>Shown to the player when the Game scene cannot become playable.</summary>
        internal string LoadFailedMessage { get; }
    }
}
