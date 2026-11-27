using System;
using Game.Run;

namespace Game.GameFlow
{
    /// <summary>
    /// The only place that picks and shows the main screen of the Game scene (feature-game-flow/flow-routes).
    /// Every request locks the flow until the target screen is shown, or until team select is restored after a
    /// failed build; requests arriving while locked are dropped with a warning.
    /// </summary>
    internal sealed class ScreenRouter
    {
        private readonly RouteSettings _settings;
        private readonly IMainScreens _screens;
        private readonly ITeamSelectPanel _teamSelect;
        private readonly IBattleBuilder _builder;
        private readonly ITeamMemory _memory;
        private readonly IFlowLog _log;

        private bool _locked;
        private bool _homeShownOnce;
        private bool _building;
        private RunTeam _buildingTeam;
        private float _buildSeconds;
        private bool _appActive = true;
        private bool _intoBattleAtStart;

        public ScreenRouter(RouteSettings settings, IMainScreens screens, ITeamSelectPanel teamSelect,
            IBattleBuilder builder, ITeamMemory memory, IFlowLog log)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _screens = screens ?? throw new ArgumentNullException(nameof(screens));
            _teamSelect = teamSelect ?? throw new ArgumentNullException(nameof(teamSelect));
            _builder = builder ?? throw new ArgumentNullException(nameof(builder));
            _memory = memory ?? throw new ArgumentNullException(nameof(memory));
            _log = log ?? throw new ArgumentNullException(nameof(log));
        }

        public MainScreen Current { get; private set; } = MainScreen.None;

        /// <summary>True from the moment a request is taken until its target screen is shown (or the source restored).</summary>
        public bool IsLocked => _locked;

        /// <summary>True while a battle is being built for a confirmed team.</summary>
        public bool IsBuilding => _building;

        /// <summary>The Game scene reported ready: show the start screen, once per load of the scene.</summary>
        public void SceneReady()
        {
            if (_homeShownOnce)
            {
                _log.Warning("GameFlow: scene ready reported again; ignored.");
                return;
            }

            _homeShownOnce = true;
            SwitchTo(MainScreen.Home);
        }

        /// <summary>
        /// Dev cheat (a state shown in battle): the scene is ready, and instead of the start screen the battle is built
        /// at once for <paramref name="team"/>, with no start screen and no team select. Once per load, like
        /// <see cref="SceneReady"/>. A failed build falls back to the start screen.
        /// </summary>
        public void SceneReadyIntoBattle(RunTeam team)
        {
            if (_homeShownOnce)
            {
                _log.Warning("GameFlow: scene ready reported again; ignored.");
                return;
            }

            _homeShownOnce = true;
            _locked = true;
            _building = true;
            _intoBattleAtStart = true;
            _buildingTeam = team;
            _buildSeconds = 0f;
            _builder.Build(team);
        }

        /// <summary>Start screen: Play.</summary>
        public void Play()
        {
            if (!Accept("Play", MainScreen.Home)) return;
            _teamSelect.HideBuildError();
            SwitchTo(MainScreen.TeamSelect);
        }

        /// <summary>Team select: Enter battle with the confirmed team. Team select stays up until the battle is built.</summary>
        public void EnterBattle(RunTeam team)
        {
            if (!Accept("Enter battle", MainScreen.TeamSelect)) return;

            _locked = true;
            _building = true;
            _buildingTeam = team;
            _buildSeconds = 0f;
            _teamSelect.HideBuildError();
            _teamSelect.SetEnterBattleEnabled(false);
            _builder.Build(team);
        }

        /// <summary>Team select: Back. The remembered team does not change.</summary>
        public void Back()
        {
            if (!Accept("Back", MainScreen.TeamSelect)) return;
            _teamSelect.HideBuildError();
            SwitchTo(MainScreen.Home);
        }

        /// <summary>The device back key: Back on team select; nothing on the start screen; the run handles it in battle.</summary>
        public void BackKey()
        {
            if (Current == MainScreen.TeamSelect) Back();
        }

        /// <summary>The run cleared the battle and hands over the player's leave choice.</summary>
        public void RunLeft(LeaveChoice choice)
        {
            if (_locked)
            {
                _log.Warning($"GameFlow: leave choice {choice} while switching screens; ignored.");
                return;
            }

            if (Current != MainScreen.Battle)
            {
                _log.Error($"GameFlow: battle cleared ({choice}) while on {Current}, not on the battle screen; ignored.");
                return;
            }

            var target = choice == LeaveChoice.Replay ? MainScreen.TeamSelect : MainScreen.Home;
            if (target == MainScreen.TeamSelect) _teamSelect.HideBuildError();
            SwitchTo(target);
        }

        /// <summary>The builder reports the battle is ready: hide team select and show the battle in the same frame.</summary>
        public void BattleBuilt()
        {
            if (!_building)
            {
                _log.Warning("GameFlow: battle built while no build is pending; ignored.");
                return;
            }

            _building = false;
            _intoBattleAtStart = false;
            _memory.Remember(_buildingTeam);
            SwitchTo(MainScreen.Battle);
        }

        /// <summary>The builder reports it could not build the battle.</summary>
        public void BattleBuildFailed(string reason)
        {
            if (!_building)
            {
                _log.Warning("GameFlow: battle build failure while no build is pending; ignored.");
                return;
            }

            FailBuild("GameFlow: battle build failed: " + reason);
        }

        /// <summary>Advances the build time limit; time while the app is in the background does not count.</summary>
        public void Tick(float deltaSeconds)
        {
            if (!_building || !_appActive || deltaSeconds <= 0f) return;

            _buildSeconds += deltaSeconds;
            if (_buildSeconds <= _settings.BuildLimitSeconds) return;

            _builder.Cancel();
            FailBuild($"GameFlow: battle build took over {_settings.BuildLimitSeconds} s.");
        }

        /// <summary>The app went to the background (false) or came back (true).</summary>
        public void SetAppActive(bool active) => _appActive = active;

        private bool Accept(string request, MainScreen from)
        {
            if (_locked)
            {
                _log.Warning($"GameFlow: {request} while switching screens; ignored.");
                return false;
            }

            if (Current != from)
            {
                _log.Warning($"GameFlow: {request} on {Current}; ignored.");
                return false;
            }

            return true;
        }

        private void FailBuild(string message)
        {
            _building = false;
            _log.Warning(message);
            if (_intoBattleAtStart)
            {
                // No team select was shown to carry the error: open on the start screen as a normal load would.
                _intoBattleAtStart = false;
                SwitchTo(MainScreen.Home);
                return;
            }

            _teamSelect.ShowBuildError(_settings.BuildFailedText);
            _teamSelect.SetEnterBattleEnabled(true);
            _locked = false;
        }

        private void SwitchTo(MainScreen target)
        {
            _locked = true;
            _screens.Show(target);
            Current = target;
            _locked = false;
        }
    }
}
