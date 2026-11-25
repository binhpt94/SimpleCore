using System;

namespace Game.SceneFlow
{
    /// <summary>The values of SceneFlowConfig, without the Unity asset around them.</summary>
    internal readonly struct SceneFlowSettings
    {
        internal SceneFlowSettings(string loadingScene, string gameScene, string loadFailedMessage,
            string featureFailedMessage, TimeSpan minLoadingScreenTime, TimeSpan slowSwitchWarningTime)
        {
            LoadingScene = loadingScene;
            GameScene = gameScene;
            LoadFailedMessage = loadFailedMessage;
            FeatureFailedMessage = featureFailedMessage;
            MinLoadingScreenTime = minLoadingScreenTime;
            SlowSwitchWarningTime = slowSwitchWarningTime;
        }

        /// <summary>The scenes of the game (design: feature-scene-flow/scene-list). A switch to any other name is refused.</summary>
        internal bool IsKnownScene(string sceneName)
        {
            return !string.IsNullOrEmpty(sceneName) && (sceneName == LoadingScene || sceneName == GameScene);
        }

        internal string LoadingScene { get; }

        internal string GameScene { get; }

        /// <summary>Shown to the player when the Game scene cannot become playable and no feature is to blame.</summary>
        internal string LoadFailedMessage { get; }

        /// <summary>Shown when one feature of the Game scene failed to load; "{0}" is replaced by its name.</summary>
        internal string FeatureFailedMessage { get; }

        /// <summary>The loading screen stays at least this long after it shows, even when loading is done sooner.</summary>
        internal TimeSpan MinLoadingScreenTime { get; }

        /// <summary>A switch still going after this long logs a performance warning; it keeps going.</summary>
        internal TimeSpan SlowSwitchWarningTime { get; }

        internal string FailedMessageFor(string feature)
        {
            if (string.IsNullOrEmpty(feature) || string.IsNullOrEmpty(FeatureFailedMessage)) return LoadFailedMessage;
            return FeatureFailedMessage.Replace("{0}", feature);
        }
    }
}
