using System;
using System.Collections.Generic;
using Game.Run;
using NUnit.Framework;

namespace Game.GameFlow.Tests
{
    // feature-game-flow/flow-routes/routes-values
    public class ScreenRouterTests
    {
        private FakeScreens _screens;
        private FakeTeamSelect _teamSelect;
        private FakeBuilder _builder;
        private FakeMemory _memory;
        private FakeLog _log;
        private ScreenRouter _router;

        private static readonly RunTeam TeamA = new RunTeam(new[] { "knight", "axe", "archer" });
        private static readonly RunTeam TeamB = new RunTeam(new[] { "mage", "axe", "archer" });

        [SetUp]
        public void SetUp()
        {
            _screens = new FakeScreens();
            _teamSelect = new FakeTeamSelect();
            _builder = new FakeBuilder();
            _memory = new FakeMemory();
            _log = new FakeLog();
            _router = new ScreenRouter(new RouteSettings(), _screens, _teamSelect, _builder, _memory, _log);
        }

        // ---- switch-instant ----

        [Test]
        public void SwitchingScreens_HidesOldAndShowsNewInOneStep()
        {
            _router.SceneReady();
            _router.Play();

            Assert.That(_screens.Shown, Is.EqualTo(new[] { MainScreen.Home, MainScreen.TeamSelect }));
            Assert.That(_router.Current, Is.EqualTo(MainScreen.TeamSelect));
            Assert.That(_router.IsLocked, Is.False);
        }

        [Test]
        public void EnterBattle_KeepsTeamSelectUpUntilTheBattleIsBuilt()
        {
            ToTeamSelect();

            _router.EnterBattle(TeamA);
            Assert.That(_router.Current, Is.EqualTo(MainScreen.TeamSelect));
            Assert.That(_screens.Shown, Has.No.Member(MainScreen.Battle));

            _router.BattleBuilt();
            Assert.That(_router.Current, Is.EqualTo(MainScreen.Battle));
            Assert.That(_screens.Shown[_screens.Shown.Count - 1], Is.EqualTo(MainScreen.Battle));
        }

        // ---- lock-until-shown ----

        [Test]
        public void RequestArrivingBeforeTheTargetScreenIsShown_IsDroppedWithAWarning()
        {
            _router.SceneReady();
            // A second Play while team select is being shown: without the lock it would still be taken,
            // because the current screen only becomes team select once Show returned.
            _screens.DuringShow = () => _router.Play();

            _router.Play();

            Assert.That(_screens.Shown, Is.EqualTo(new[] { MainScreen.Home, MainScreen.TeamSelect }));
            Assert.That(_router.Current, Is.EqualTo(MainScreen.TeamSelect));
            Assert.That(_log.Warnings, Has.Count.EqualTo(1));
            Assert.That(_log.Warnings[0], Does.Contain("switching screens"));
            Assert.That(_router.IsLocked, Is.False);
        }

        [Test]
        public void WhileBuilding_TheFlowIsLockedAndRequestsAreDropped()
        {
            ToTeamSelect();
            _router.EnterBattle(TeamA);

            Assert.That(_router.IsLocked, Is.True);
            _log.Clear();
            _router.EnterBattle(TeamB);
            _router.Back();
            _router.RunLeft(LeaveChoice.GoHome);

            Assert.That(_log.Warnings, Has.Count.EqualTo(3));
            Assert.That(_router.Current, Is.EqualTo(MainScreen.TeamSelect));
        }

        [Test]
        public void AfterTheTargetScreenIsShown_RequestsAreTakenAgain()
        {
            ToTeamSelect();
            _router.EnterBattle(TeamA);
            _router.BattleBuilt();

            Assert.That(_router.IsLocked, Is.False);
            _router.RunLeft(LeaveChoice.Replay);
            Assert.That(_router.Current, Is.EqualTo(MainScreen.TeamSelect));
        }

        // ---- build-limit ----

        [Test]
        public void BuildFailure_StaysOnTeamSelect_ShowsTheErrorLine_AndReopensEnterBattle()
        {
            ToTeamSelect();
            _router.EnterBattle(TeamA);
            Assert.That(_teamSelect.EnterEnabled, Is.False);

            _router.BattleBuildFailed("no prefab");

            Assert.That(_router.Current, Is.EqualTo(MainScreen.TeamSelect));
            Assert.That(_teamSelect.ErrorText, Is.EqualTo("Không vào được trận, thử lại"));
            Assert.That(_teamSelect.EnterEnabled, Is.True);
            Assert.That(_router.IsLocked, Is.False);
            Assert.That(_memory.Remembered, Is.Empty);
        }

        [Test]
        public void BuildTakingOverThreeSeconds_FailsAndCancelsTheBuild()
        {
            ToTeamSelect();
            _router.EnterBattle(TeamA);

            _router.Tick(1.5f);
            _router.Tick(1.5f);
            Assert.That(_router.IsBuilding, Is.True, "exactly 3 s is not over the limit");

            _router.Tick(0.1f);

            Assert.That(_builder.Cancelled, Is.EqualTo(1));
            Assert.That(_router.IsBuilding, Is.False);
            Assert.That(_router.Current, Is.EqualTo(MainScreen.TeamSelect));
            Assert.That(_teamSelect.ErrorText, Is.EqualTo("Không vào được trận, thử lại"));
            Assert.That(_teamSelect.EnterEnabled, Is.True);
        }

        [Test]
        public void BattleBuiltAfterTheLimitPassed_IsIgnored()
        {
            ToTeamSelect();
            _router.EnterBattle(TeamA);
            _router.Tick(3.5f);

            _router.BattleBuilt();

            Assert.That(_router.Current, Is.EqualTo(MainScreen.TeamSelect));
            Assert.That(_memory.Remembered, Is.Empty);
        }

        [Test]
        public void AfterAFailedBuild_EnterBattleCanBePressedAgain_AndHidesTheErrorLine()
        {
            ToTeamSelect();
            _router.EnterBattle(TeamA);
            _router.BattleBuildFailed("x");

            _router.EnterBattle(TeamA);

            Assert.That(_builder.Built, Has.Count.EqualTo(2));
            Assert.That(_teamSelect.ErrorText, Is.Null);
        }

        [Test]
        public void SuccessfulBuild_RemembersTheConfirmedTeam()
        {
            ToTeamSelect();
            _router.EnterBattle(TeamA);
            _router.BattleBuilt();

            Assert.That(_memory.Remembered, Has.Count.EqualTo(1));
            Assert.That(_memory.Remembered[0].HeroCodes, Is.EqualTo(TeamA.HeroCodes));
        }

        // ---- back-button ----

        [Test]
        public void BackOnTeamSelect_GoesToTheStartScreen_WithoutTouchingTheRememberedTeam()
        {
            ToTeamSelect();

            _router.Back();

            Assert.That(_router.Current, Is.EqualTo(MainScreen.Home));
            Assert.That(_memory.Remembered, Is.Empty);
        }

        [Test]
        public void BackButton_SitsTopLeftWithASideOfAtLeast120Px()
        {
            var settings = new RouteSettings();

            Assert.That(settings.BackButtonCorner, Is.EqualTo(ScreenCorner.TopLeft));
            Assert.That(settings.BackButtonMinSidePx, Is.EqualTo(120));
        }

        [Test]
        public void BackOutsideTeamSelect_IsIgnored()
        {
            _router.SceneReady();

            _router.Back();

            Assert.That(_router.Current, Is.EqualTo(MainScreen.Home));
            Assert.That(_screens.Shown, Has.Count.EqualTo(1));
        }

        // ---- home-first-once ----

        [Test]
        public void SceneReady_ShowsTheStartScreen()
        {
            _router.SceneReady();

            Assert.That(_screens.Shown, Is.EqualTo(new[] { MainScreen.Home }));
            Assert.That(_router.Current, Is.EqualTo(MainScreen.Home));
        }

        [Test]
        public void RepeatedSceneReady_IsIgnored()
        {
            _router.SceneReady();
            _router.Play();

            _router.SceneReady();

            Assert.That(_router.Current, Is.EqualTo(MainScreen.TeamSelect));
            Assert.That(_screens.Shown, Is.EqualTo(new[] { MainScreen.Home, MainScreen.TeamSelect }));
            Assert.That(_log.Warnings, Has.Count.EqualTo(1));
        }

        [Test]
        public void Participant_ShowsTheStartScreenWhenTheSessionRuns()
        {
            // From PROTOTYPE the session's run step waits for SceneFlow's scene-ready signal, then shows the start screen.
            var home = new GameFlowParticipantTests.FakeHome();
            var ready = new GameFlowParticipantTests.FakeSceneReady();
            var participant = GameFlowParticipantTests.Make(home, new GameFlowParticipantTests.FakeTeamSelect(),
                new GameFlowParticipantTests.FakeBattle(), () => ready, new GameFlowParticipantTests.FakeClock());

            participant.Run();
            ready.Raise();

            Assert.That(home.Shows, Is.EqualTo(1));
        }

        // ---- back-key-map ----

        [Test]
        public void BackKeyOnTeamSelect_ActsLikeBack()
        {
            ToTeamSelect();

            _router.BackKey();

            Assert.That(_router.Current, Is.EqualTo(MainScreen.Home));
        }

        [Test]
        public void BackKeyOnTheStartScreen_DoesNothing()
        {
            _router.SceneReady();
            _log.Clear();

            _router.BackKey();

            Assert.That(_router.Current, Is.EqualTo(MainScreen.Home));
            Assert.That(_screens.Shown, Has.Count.EqualTo(1));
            Assert.That(_log.Warnings, Is.Empty);
        }

        [Test]
        public void BackKeyInBattle_IsLeftToTheRun()
        {
            ToBattle();

            _router.BackKey();

            Assert.That(_router.Current, Is.EqualTo(MainScreen.Battle));
        }

        // ---- edge: Enter battle twice quickly ----

        [Test]
        public void EnterBattlePressedTwiceQuickly_BuildsOnlyOneBattle()
        {
            ToTeamSelect();

            _router.EnterBattle(TeamA);
            _router.EnterBattle(TeamA);
            _router.BattleBuilt();

            Assert.That(_builder.Built, Has.Count.EqualTo(1));
            Assert.That(_router.Current, Is.EqualTo(MainScreen.Battle));
        }

        [Test]
        public void BattleBuiltInsideTheBuildCall_StillShowsTheBattle()
        {
            ToTeamSelect();
            _builder.OnBuild = () => _router.BattleBuilt();

            _router.EnterBattle(TeamA);

            Assert.That(_router.Current, Is.EqualTo(MainScreen.Battle));
            Assert.That(_router.IsLocked, Is.False);
        }

        // ---- edge: Back while building ----

        [Test]
        public void BackPressedWhileBuilding_IsIgnored()
        {
            ToTeamSelect();
            _router.EnterBattle(TeamA);

            _router.Back();
            _router.BackKey();
            _router.BattleBuilt();

            Assert.That(_router.Current, Is.EqualTo(MainScreen.Battle));
            Assert.That(_screens.Shown, Is.EqualTo(new[] { MainScreen.Home, MainScreen.TeamSelect, MainScreen.Battle }));
            Assert.That(_log.Warnings, Has.Count.EqualTo(2), "Back and the back key are both dropped with a warning");
        }

        // ---- edge: build failed then Back ----

        [Test]
        public void BuildFailedThenBack_GoesHome_AndTheRememberedTeamIsTheOneOfTheLastBattle()
        {
            ToTeamSelect();
            _router.EnterBattle(TeamA);
            _router.BattleBuilt();
            _router.RunLeft(LeaveChoice.Replay);

            _router.EnterBattle(TeamB);
            _router.BattleBuildFailed("x");
            _router.Back();

            Assert.That(_router.Current, Is.EqualTo(MainScreen.Home));
            Assert.That(_memory.Remembered, Has.Count.EqualTo(1));
            Assert.That(_memory.Remembered[0].HeroCodes, Is.EqualTo(TeamA.HeroCodes));
        }

        // ---- edge: leaving the app while building ----

        [Test]
        public void TimeInTheBackgroundDoesNotCountTowardsTheBuildLimit()
        {
            ToTeamSelect();
            _router.EnterBattle(TeamA);
            _router.Tick(2f);

            _router.SetAppActive(false);
            _router.Tick(60f);
            _router.SetAppActive(true);

            Assert.That(_router.IsBuilding, Is.True);
            Assert.That(_builder.Cancelled, Is.Zero);

            _router.Tick(1.5f);
            Assert.That(_router.IsBuilding, Is.False);
            Assert.That(_builder.Cancelled, Is.EqualTo(1));
        }

        // ---- edge: battle cleared while not on the battle screen ----

        [Test]
        public void BattleClearedWhileNotOnTheBattleScreen_IsIgnoredAndLoggedAsAnError()
        {
            ToTeamSelect();

            _router.RunLeft(LeaveChoice.GoHome);

            Assert.That(_router.Current, Is.EqualTo(MainScreen.TeamSelect));
            Assert.That(_log.Errors, Has.Count.EqualTo(1));
        }

        [Test]
        public void LeaveChoices_GoToTheirScreens()
        {
            ToBattle();
            _router.RunLeft(LeaveChoice.Replay);
            Assert.That(_router.Current, Is.EqualTo(MainScreen.TeamSelect));

            _router.EnterBattle(TeamA);
            _router.BattleBuilt();
            _router.RunLeft(LeaveChoice.GoHome);
            Assert.That(_router.Current, Is.EqualTo(MainScreen.Home));
        }

        // ---- params ----

        [Test]
        public void Settings_MatchTheDesignedValues()
        {
            var settings = new RouteSettings();

            Assert.That(settings.SwitchSeconds, Is.EqualTo(0f));
            Assert.That(settings.BuildLimitSeconds, Is.EqualTo(3f));
            Assert.That(settings.BuildFailedText, Is.EqualTo("Không vào được trận, thử lại"));
            Assert.That(settings.BackButtonMinSidePx, Is.EqualTo(120));
            Assert.That(settings.BackButtonCorner, Is.EqualTo(ScreenCorner.TopLeft));
        }

        [Test]
        public void Settings_RejectValuesThatCannotWork()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new RouteSettings(buildLimitSeconds: 0f));
            Assert.Throws<ArgumentOutOfRangeException>(() => new RouteSettings(switchSeconds: -1f));
            Assert.Throws<ArgumentException>(() => new RouteSettings(buildFailedText: ""));
            Assert.Throws<ArgumentOutOfRangeException>(() => new RouteSettings(backButtonMinSidePx: 0));
        }

        [Test]
        public void ARetryAfterAFailureIsNotLimited()
        {
            ToTeamSelect();
            for (var i = 0; i < 5; i++)
            {
                _router.EnterBattle(TeamA);
                _router.BattleBuildFailed("x");
            }

            _router.EnterBattle(TeamA);
            _router.BattleBuilt();

            Assert.That(_builder.Built, Has.Count.EqualTo(6));
            Assert.That(_router.Current, Is.EqualTo(MainScreen.Battle));
        }

        // ---- helpers ----

        private void ToTeamSelect()
        {
            _router.SceneReady();
            _router.Play();
            _log.Clear();
        }

        private void ToBattle()
        {
            ToTeamSelect();
            _router.EnterBattle(TeamA);
            _router.BattleBuilt();
            _log.Clear();
        }

        private sealed class FakeScreens : IMainScreens
        {
            public readonly List<MainScreen> Shown = new List<MainScreen>();
            public Action DuringShow;

            public void Show(MainScreen screen)
            {
                Shown.Add(screen);
                DuringShow?.Invoke();
            }
        }

        private sealed class FakeTeamSelect : ITeamSelectPanel
        {
            public bool EnterEnabled = true;
            public string ErrorText;

            public void SetEnterBattleEnabled(bool enabled) => EnterEnabled = enabled;
            public void ShowBuildError(string text) => ErrorText = text;
            public void HideBuildError() => ErrorText = null;
        }

        private sealed class FakeBuilder : IBattleBuilder
        {
            public readonly List<RunTeam> Built = new List<RunTeam>();
            public int Cancelled;
            public Action OnBuild;

            public void Build(RunTeam team)
            {
                Built.Add(team);
                OnBuild?.Invoke();
            }

            public void Cancel() => Cancelled++;
        }

        private sealed class FakeMemory : ITeamMemory
        {
            public readonly List<RunTeam> Remembered = new List<RunTeam>();
            public void Remember(RunTeam team) => Remembered.Add(team);
        }

        private sealed class FakeLog : IFlowLog
        {
            public readonly List<string> Warnings = new List<string>();
            public readonly List<string> Errors = new List<string>();
            public void Warning(string message) => Warnings.Add(message);
            public void Error(string message) => Errors.Add(message);

            public void Clear()
            {
                Warnings.Clear();
                Errors.Clear();
            }
        }
    }
}
