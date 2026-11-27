using System;
using System.Collections.Generic;
using Game.Core;
using Game.Home;
using Game.Run;
using Game.SceneFlow;
using Game.TeamSelect;

namespace Game.GameFlow
{
    /// <summary>
    /// Joins the Game scene's session from PROTOTYPE. At <see cref="OnRun"/> it connects the screens' presses and the
    /// run's answers to the router, starts the build clock, and waits for the scene-ready signal of SceneFlow
    /// (taken late because SceneFlow installs last) to show the start screen once.
    /// </summary>
    internal sealed class GameFlowParticipant : SessionParticipantBase
    {
        private readonly ScreenRouter _router;
        private readonly IHomeScreen _home;
        private readonly ITeamSelectScreen _teamSelect;
        private readonly IRunBattle _battle;
        private readonly Func<IGameSceneReady> _sceneReady;
        private readonly IFlowClock _clock;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private readonly IRunCheatStart _cheatStart;
#endif

        public GameFlowParticipant(ScreenRouter router, IHomeScreen home, ITeamSelectScreen teamSelect,
            IRunBattle battle, Func<IGameSceneReady> sceneReady, IFlowClock clock
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            , IRunCheatStart cheatStart = null
#endif
            )
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            _cheatStart = cheatStart;
#endif
            _router = router ?? throw new ArgumentNullException(nameof(router));
            _home = home ?? throw new ArgumentNullException(nameof(home));
            _teamSelect = teamSelect ?? throw new ArgumentNullException(nameof(teamSelect));
            _battle = battle ?? throw new ArgumentNullException(nameof(battle));
            _sceneReady = sceneReady ?? throw new ArgumentNullException(nameof(sceneReady));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        }

        protected override void OnRun()
        {
            var ready = _sceneReady() ??
                throw new InvalidOperationException("GameFlow: IGameSceneReady is missing (SceneFlow not installed).");

            Action play = _router.Play;
            _home.PlayPressed += play;
            WhenDisposed(() => _home.PlayPressed -= play);

            Action<IReadOnlyList<string>> enter = codes => _router.EnterBattle(new RunTeam(codes));
            _teamSelect.EnterBattlePressed += enter;
            WhenDisposed(() => _teamSelect.EnterBattlePressed -= enter);

            Action back = _router.Back;
            _teamSelect.BackPressed += back;
            WhenDisposed(() => _teamSelect.BackPressed -= back);

            Action built = _router.BattleBuilt;
            _battle.BattleBuilt += built;
            WhenDisposed(() => _battle.BattleBuilt -= built);

            Action<string> failed = _router.BattleBuildFailed;
            _battle.BuildFailed += failed;
            WhenDisposed(() => _battle.BuildFailed -= failed);

            Action<RunLeaveChoice> left = choice => _router.RunLeft(LeaveChoices.From(choice));
            _battle.BattleLeft += left;
            WhenDisposed(() => _battle.BattleLeft -= left);

            var job = TrackJob();
            var clock = _clock.Start(_router.Tick, _router.SetAppActive);
            WhenDisposed(() =>
            {
                clock.Dispose();
                job.Dispose();
            });

            if (ready.IsReady)
            {
                OnSceneReady();
                return;
            }

            Action onReady = OnSceneReady;
            ready.Ready += onReady;
            WhenDisposed(() => ready.Ready -= onReady);
        }

        /// <summary>The start screen, or (dev cheat state shown in battle) straight into the battle it asked for.</summary>
        private void OnSceneReady()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (_cheatStart != null && _cheatStart.BattleRequested)
            {
                _router.SceneReadyIntoBattle(_cheatStart.Team);
                return;
            }
#endif
            _router.SceneReady();
        }
    }
}
