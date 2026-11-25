using System;

namespace Game.GameFlow
{
    /// <summary>Numbers of feature-game-flow/flow-routes/routes-values ("Thông số").</summary>
    internal sealed class RouteSettings
    {
        public const float DefaultSwitchSeconds = 0f;
        public const float DefaultBuildLimitSeconds = 3f;
        public const string DefaultBuildFailedText = "Không vào được trận, thử lại";
        public const int DefaultBackButtonMinSidePx = 120;

        public RouteSettings(
            float switchSeconds = DefaultSwitchSeconds,
            float buildLimitSeconds = DefaultBuildLimitSeconds,
            string buildFailedText = DefaultBuildFailedText,
            int backButtonMinSidePx = DefaultBackButtonMinSidePx,
            ScreenCorner backButtonCorner = ScreenCorner.TopLeft)
        {
            if (switchSeconds < 0f) throw new ArgumentOutOfRangeException(nameof(switchSeconds));
            if (buildLimitSeconds <= 0f) throw new ArgumentOutOfRangeException(nameof(buildLimitSeconds));
            if (string.IsNullOrEmpty(buildFailedText)) throw new ArgumentException("Build failed text is empty", nameof(buildFailedText));
            if (backButtonMinSidePx <= 0) throw new ArgumentOutOfRangeException(nameof(backButtonMinSidePx));

            SwitchSeconds = switchSeconds;
            BuildLimitSeconds = buildLimitSeconds;
            BuildFailedText = buildFailedText;
            BackButtonMinSidePx = backButtonMinSidePx;
            BackButtonCorner = backButtonCorner;
        }

        /// <summary>Time to switch between two main screens; 0 means the same frame.</summary>
        public float SwitchSeconds { get; }

        /// <summary>A battle build taking longer than this, counted only while the app is active, fails.</summary>
        public float BuildLimitSeconds { get; }

        /// <summary>Line shown on team select when the battle could not be built.</summary>
        public string BuildFailedText { get; }

        /// <summary>Smallest side of the Back button on team select, in px on the 1920x1080 reference screen.</summary>
        public int BackButtonMinSidePx { get; }

        public ScreenCorner BackButtonCorner { get; }
    }
}
