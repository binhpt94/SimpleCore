using System;

namespace Game.SceneFlow
{
    /// <summary>The values of SceneFlowConfig, without the Unity asset around them.</summary>
    internal readonly struct SceneFlowSettings
    {
        internal SceneFlowSettings(string loadingScene, string gameScene, string loadFailedMessage,
            string featureFailedMessage, TimeSpan minLoadingScreenTime)
        {
            LoadingScene = loadingScene;
            GameScene = gameScene;
            LoadFailedMessage = loadFailedMessage;
            FeatureFailedMessage = featureFailedMessage;
            MinLoadingScreenTime = minLoadingScreenTime;
        }

        internal string LoadingScene { get; }

        internal string GameScene { get; }

        /// <summary>Shown to the player when the Game scene cannot become playable and no feature is to blame.</summary>
        internal string LoadFailedMessage { get; }

        /// <summary>Shown when one feature of the Game scene failed to load; "{0}" is replaced by its name.</summary>
        internal string FeatureFailedMessage { get; }

        /// <summary>The loading screen stays at least this long after it shows, even when loading is done sooner.</summary>
        internal TimeSpan MinLoadingScreenTime { get; }

        internal string FailedMessageFor(string feature)
        {
            if (string.IsNullOrEmpty(feature) || string.IsNullOrEmpty(FeatureFailedMessage)) return LoadFailedMessage;
            return FeatureFailedMessage.Replace("{0}", feature);
        }
    }
}
