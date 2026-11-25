using UnityEngine;

namespace Game.GameFlow
{
    /// <summary>
    /// Numbers of feature-game-flow/flow-routes/routes-values, as an asset in FeatureAssets/Private.
    /// <see cref="RouteScreens"/> follows the stage table: off at IDEA, where the run still switches screens.
    /// </summary>
    [CreateAssetMenu(menuName = "Game/GameFlow/Config", fileName = "GameFlowConfig")]
    internal sealed class GameFlowConfig : ScriptableObject
    {
        [Tooltip("Off at IDEA: feature-run switches screens. On from PROTOTYPE: GameFlow routes the main screens.")]
        [SerializeField] private bool _routeScreens;

        [Tooltip("Seconds to switch between two main screens; 0 = same frame.")]
        [SerializeField] private float _switchSeconds = RouteSettings.DefaultSwitchSeconds;

        [Tooltip("A battle build longer than this, counted only while the app is active, fails.")]
        [SerializeField] private float _buildLimitSeconds = RouteSettings.DefaultBuildLimitSeconds;

        [SerializeField] private string _buildFailedText = RouteSettings.DefaultBuildFailedText;

        [Tooltip("Smallest side of the Back button on team select, px on the 1920x1080 reference screen.")]
        [SerializeField] private int _backButtonMinSidePx = RouteSettings.DefaultBackButtonMinSidePx;

        [SerializeField] private ScreenCorner _backButtonCorner = ScreenCorner.TopLeft;

        public bool RouteScreens => _routeScreens;

        /// <summary>Throws when a value cannot work (see <see cref="RouteSettings"/>).</summary>
        public RouteSettings ToSettings() =>
            new RouteSettings(_switchSeconds, _buildLimitSeconds, _buildFailedText, _backButtonMinSidePx, _backButtonCorner);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        /// <summary>Test hook: sets the stage switch on an instance made in a test.</summary>
        internal void SetRouteScreensForTest(bool value) => _routeScreens = value;

        /// <summary>Test hook: sets the build limit on an instance made in a test.</summary>
        internal void SetBuildLimitForTest(float seconds) => _buildLimitSeconds = seconds;
#endif
    }
}
