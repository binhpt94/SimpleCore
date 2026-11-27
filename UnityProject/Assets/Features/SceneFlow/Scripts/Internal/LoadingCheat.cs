using Game.Core;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System.Collections.Generic;
#endif

namespace Game.SceneFlow
{
    /// <summary>
    /// States of the design node feature-scene-flow/cheat. Chosen on the Loading installer asset or picked in the
    /// cheat panel; only acted on in dev builds. The enum stays in every build so the asset's serialized field reads
    /// the same everywhere.
    /// </summary>
    internal enum SceneFlowCheatState
    {
        None = 0,

        /// <summary>The loading screen stopped because one feature failed to load.</summary>
        LoadError = 1
    }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    /// <summary>What the cheat list shows of this feature, from the design (feature-scene-flow and its cheat node). Fixed values, no state.</summary>
    internal static class SceneFlowCheats
    {
        /// <summary>Title of the design node feature-scene-flow.</summary>
        internal const string FeatureTitle = "Chuyển scene";

        internal const string LoadErrorKey = "SceneFlow/LoadError";

        /// <summary>In the order of the ideas of the cheat node.</summary>
        internal static readonly IReadOnlyList<CheatState> States = new[]
        {
            new CheatState(LoadErrorKey, "Màn Loading dừng vì một feature tải lỗi")
        };

        /// <summary>The state a picked key stands for: <see cref="SceneFlowCheatState.None"/> when it is not this feature's.</summary>
        internal static SceneFlowCheatState StateOf(string pickedKey)
        {
            return pickedKey == LoadErrorKey ? SceneFlowCheatState.LoadError : SceneFlowCheatState.None;
        }
    }

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
        private readonly SceneGate gate;

        internal LoadErrorCheat(ILoadingScreenSource screens, SceneFlowSettings settings, SceneGate gate)
        {
            this.screens = screens;
            this.settings = settings;
            this.gate = gate;
        }

        protected override bool OpensViewsOnRun => true;

        protected override void OnRun()
        {
            var screen = screens.Create();
            // Not the switch cover: this screen belongs to the Loading session, so it is counted as a held view.
            var held = TrackView(screen.Root);
            WhenDisposed(() =>
            {
                held.Dispose();
                screen.Close();
            });
            screen.ShowProgress(ForcedProgress);
            screen.ShowError(settings.FailedMessageFor(ForcedFailedFeature));

            // A switch into this scene (the cheat panel reloading it) ends here: the state is built.
            gate.Open();
        }
    }
#endif
}
