using Game.Core;

namespace Game.SceneFlow
{
    /// <summary>
    /// States of the design node feature-scene-flow/cheat. Chosen on the Loading installer asset; only acted on in
    /// dev builds. The enum stays in every build so the asset's serialized field reads the same everywhere.
    /// </summary>
    internal enum SceneFlowCheatState
    {
        None = 0,

        /// <summary>The loading screen stopped because one feature failed to load.</summary>
        LoadError = 1
    }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    /// <summary>
    /// Cheat state load-error: shows the loading screen as it looks when the Game scene fails, without breaking any
    /// real asset. The bar stands at 40% and the error names Wallet; the Game scene is not loaded.
    /// </summary>
    internal sealed class LoadErrorCheat : SessionParticipantBase
    {
        /// <summary>Forced values of the state, from the design node (not balance numbers).</summary>
        internal const float ForcedProgress = 0.4f;
        internal const string ForcedFailedFeature = "Wallet";

        private readonly ILoadingScreenSource screens;
        private readonly SceneFlowSettings settings;

        internal LoadErrorCheat(ILoadingScreenSource screens, SceneFlowSettings settings)
        {
            this.screens = screens;
            this.settings = settings;
        }

        protected override void OnRun()
        {
            var screen = screens.Create();
            WhenDisposed(screen.Close);
            screen.ShowProgress(ForcedProgress);
            screen.ShowError(settings.FailedMessageFor(ForcedFailedFeature));
        }
    }
#endif
}
