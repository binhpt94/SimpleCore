using System;
using Game.Core;
using Game.Home;
using Game.Run;
using Game.SceneFlow;
using Game.TeamSelect;
using UnityEngine;

namespace Game.GameFlow
{
    /// <summary>
    /// Wires GameFlow into the Game scene. With "Route screens" on (from PROTOTYPE) it builds the router over the
    /// start screen, team select and the run's battle, and joins the session. SceneFlow installs last, so its
    /// scene-ready signal is taken from the registry only when the session runs. With "Route screens" off (IDEA)
    /// it only checks its config and joins nothing.
    /// </summary>
    [CreateAssetMenu(menuName = "Game/GameFlow/Installer", fileName = "GameFlowInstaller")]
    public sealed class GameFlowInstaller : InstallerAsset
    {
        [SerializeField] private GameFlowConfig _config;

        public override void Install(IServiceRegistry registry)
        {
            if (registry == null) throw new ArgumentNullException(nameof(registry));
            if (_config == null) throw new InvalidOperationException("GameFlowInstaller: GameFlowConfig is missing.");

            var settings = _config.ToSettings();

            if (!_config.RouteScreens) return;

            var session = Need<ISessionScene>(registry);
            var home = Need<IHomeScreen>(registry);
            var teamSelect = Need<ITeamSelectScreen>(registry);
            var battle = Need<IRunBattle>(registry);

            var router = new ScreenRouter(
                settings,
                new MainScreensAdapter(home, teamSelect),
                new TeamSelectPanelAdapter(teamSelect),
                new BattleBuilderAdapter(battle),
                new TeamMemoryAdapter(teamSelect),
                new UnityFlowLog());

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            // A cheat state shown in battle asked the run for a battle while installing (all before GameFlow).
            registry.TryResolve<IRunCheatStart>(out var cheatStart);
            session.Join(new GameFlowParticipant(
                router, home, teamSelect, battle, () => Need<IGameSceneReady>(registry), new UnityFlowClock(),
                cheatStart));
#else
            session.Join(new GameFlowParticipant(
                router, home, teamSelect, battle, () => Need<IGameSceneReady>(registry), new UnityFlowClock()));
#endif
        }

        private static T Need<T>(IServiceRegistry registry) where T : class
        {
            if (registry.TryResolve<T>(out var service)) return service;
            throw new InvalidOperationException(
                $"GameFlowInstaller: {typeof(T).Name} is not registered; install its feature before GameFlow " +
                "(IGameSceneReady: SceneFlow must be in the Game scene's boot).");
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        /// <summary>Test hook: sets the config on an instance made in a test.</summary>
        internal void SetConfigForTest(GameFlowConfig config) => _config = config;
#endif
    }
}
