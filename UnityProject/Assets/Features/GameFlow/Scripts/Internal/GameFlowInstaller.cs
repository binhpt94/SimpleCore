using System;
using Game.Core;
using UnityEngine;

namespace Game.GameFlow
{
    /// <summary>
    /// Wires GameFlow into the Game scene. At IDEA (config "Route screens" off) feature-run keeps switching
    /// screens, so the installer only checks its config and joins nothing: it never shows a screen.
    /// Routing from PROTOTYPE needs contracts with Run, Home and TeamSelect that do not exist yet.
    /// </summary>
    [CreateAssetMenu(menuName = "Game/GameFlow/Installer", fileName = "GameFlowInstaller")]
    public sealed class GameFlowInstaller : InstallerAsset
    {
        [SerializeField] private GameFlowConfig _config;

        public override void Install(IServiceRegistry registry)
        {
            if (registry == null) throw new ArgumentNullException(nameof(registry));
            if (_config == null) throw new InvalidOperationException("GameFlowInstaller: GameFlowConfig is missing.");

            _config.ToSettings();

            if (!_config.RouteScreens) return;

            throw new InvalidOperationException(
                "GameFlowInstaller: 'Route screens' is on, but GameFlow cannot route yet: the contracts with Run " +
                "(hand the team, battle built or failed, leave choice after clearing), Home and TeamSelect do not exist. " +
                "Keep it off; feature-run switches screens at IDEA.");
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        /// <summary>Test hook: sets the config on an instance made in a test.</summary>
        internal void SetConfigForTest(GameFlowConfig config) => _config = config;
#endif
    }
}
