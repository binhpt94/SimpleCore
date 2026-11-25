using System;
using System.Collections.Generic;
using Game.Home;
using Game.Run;
using Game.SceneFlow;
using Game.TeamSelect;
using NUnit.Framework;

namespace Game.GameFlow.Tests
{
    // feature-game-flow/flow-routes/routes-values: the router wired to the start screen, team select, the run and
    // SceneFlow's scene-ready signal (PROTOTYPE: "Ai đổi màn" = the screen flow).
    public class GameFlowParticipantTests
    {
        private static readonly string[] Codes = { "knight", "axe", "archer" };

        private FakeHome _home;
        private FakeTeamSelect _teamSelect;
        private FakeBattle _battle;
        private FakeSceneReady _ready;
        private FakeClock _clock;
        private int _readyLookups;
        private GameFlowParticipant _participant;

        [SetUp]
        public void SetUp()
        {
            _home = new FakeHome();
            _teamSelect = new FakeTeamSelect();
            _battle = new FakeBattle();
            _ready = new FakeSceneReady();
            _clock = new FakeClock();
            _readyLookups = 0;
            _participant = Make(_home, _teamSelect, _battle, () =>
            {
                _readyLookups++;
                return _ready;
            }, _clock);
        }

        internal static GameFlowParticipant Make(FakeHome home, FakeTeamSelect teamSelect, FakeBattle battle,
            Func<IGameSceneReady> ready, FakeClock clock)
        {
            var router = new ScreenRouter(new RouteSettings(), new MainScreensAdapter(home, teamSelect),
                new TeamSelectPanelAdapter(teamSelect), new BattleBuilderAdapter(battle),
                new TeamMemoryAdapter(teamSelect), new SilentLog());
            return new GameFlowParticipant(router, home, teamSelect, battle, ready, clock);
        }

        [Test]
        public void SceneReadyIsTakenOnlyWhenTheSessionRuns()
        {
            Assert.That(_readyLookups, Is.Zero);

            _participant.Run();

            Assert.That(_readyLookups, Is.EqualTo(1));
        }

        [Test]
        public void StartScreenShowsWhenSceneFlowReportsReady_NotBefore()
        {
            _participant.Run();
            Assert.That(_home.Shows, Is.Zero);

            _ready.Raise();

            Assert.That(_home.Shows, Is.EqualTo(1));
            Assert.That(_teamSelect.Shows, Is.Zero);
        }

        [Test]
        public void SceneAlreadyReadyWhenTheSessionRuns_ShowsTheStartScreenAtOnce()
        {
            _ready.IsReady = true;

            _participant.Run();

            Assert.That(_home.Shows, Is.EqualTo(1));
        }

        [Test]
        public void ReadyRaisedTwice_ShowsTheStartScreenOnce()
        {
            _participant.Run();
            _ready.Raise();
            _ready.Raise();

            Assert.That(_home.Shows, Is.EqualTo(1));
        }

        [Test]
        public void Play_HidesTheStartScreenAndShowsTeamSelect()
        {
            ToTeamSelect();

            Assert.That(_home.Hides, Is.EqualTo(1));
            Assert.That(_teamSelect.Shows, Is.EqualTo(1));
        }

        [Test]
        public void EnterBattle_AsksTheRunForABattleWithTheTeam_AndLocksTheButton()
        {
            ToTeamSelect();

            _teamSelect.PressEnter(Codes);

            Assert.That(_battle.Builds, Has.Count.EqualTo(1));
            Assert.That(_battle.Builds[0].HeroCodes, Is.EqualTo(Codes));
            Assert.That(_teamSelect.EnterEnabled, Is.False);
            Assert.That(_teamSelect.Hides, Is.Zero, "team select stays up while the battle is built");
        }

        [Test]
        public void BattleBuilt_HidesTeamSelect_AndRemembersTheTeam()
        {
            ToTeamSelect();
            _teamSelect.PressEnter(Codes);

            _battle.RaiseBuilt();

            Assert.That(_teamSelect.Hides, Is.EqualTo(1));
            Assert.That(_home.Shows, Is.EqualTo(1));
            Assert.That(_teamSelect.Remembered, Is.EqualTo(Codes));
        }

        [Test]
        public void BuildFailed_ShowsTheErrorLine_AndReopensEnterBattle_WithoutRemembering()
        {
            ToTeamSelect();
            _teamSelect.PressEnter(Codes);

            _battle.RaiseFailed("no prefab");

            Assert.That(_teamSelect.ErrorText, Is.EqualTo("Không vào được trận, thử lại"));
            Assert.That(_teamSelect.EnterEnabled, Is.True);
            Assert.That(_teamSelect.Remembered, Is.Null);
            Assert.That(_teamSelect.Hides, Is.Zero);
        }

        [Test]
        public void BuildOverTheLimit_CancelsTheRunsBuild()
        {
            ToTeamSelect();
            _teamSelect.PressEnter(Codes);

            _clock.Tick(3.1f);

            Assert.That(_battle.Cancels, Is.EqualTo(1));
            Assert.That(_teamSelect.ErrorText, Is.EqualTo("Không vào được trận, thử lại"));
        }

        [Test]
        public void TimeInTheBackground_DoesNotCountTowardsTheLimit()
        {
            ToTeamSelect();
            _teamSelect.PressEnter(Codes);

            _clock.SetActive(false);
            _clock.Tick(60f);
            _clock.SetActive(true);

            Assert.That(_battle.Cancels, Is.Zero);
        }

        [Test]
        public void BackOnTeamSelect_GoesToTheStartScreen()
        {
            ToTeamSelect();

            _teamSelect.PressBack();

            Assert.That(_teamSelect.Hides, Is.EqualTo(1));
            Assert.That(_home.Shows, Is.EqualTo(2));
            Assert.That(_teamSelect.Remembered, Is.Null);
        }

        [Test]
        public void LeavingTheBattle_GoesWhereTheChoiceSays()
        {
            ToBattle();
            _battle.RaiseLeft(RunLeaveChoice.PlayAgain);
            Assert.That(_teamSelect.Shows, Is.EqualTo(2));

            _teamSelect.PressEnter(Codes);
            _battle.RaiseBuilt();
            _battle.RaiseLeft(RunLeaveChoice.BackToStart);
            Assert.That(_home.Shows, Is.EqualTo(2));

            _home.PressPlay();
            _teamSelect.PressEnter(Codes);
            _battle.RaiseBuilt();
            _battle.RaiseLeft(RunLeaveChoice.Abandon);
            Assert.That(_home.Shows, Is.EqualTo(3));
        }

        [Test]
        public void EndingTheSession_StopsListeningAndStopsTheClock()
        {
            _participant.Run();
            _ = _participant.DisposeAsync();

            _ready.Raise();
            _home.PressPlay();

            Assert.That(_home.Shows, Is.Zero);
            Assert.That(_clock.Stopped, Is.True);
            Assert.That(_home.HasListeners || _teamSelect.HasListeners || _battle.HasListeners || _ready.HasListeners,
                Is.False);
        }

        [Test]
        public void MissingSceneReadySignal_FailsTheRunNamingIt()
        {
            var participant = Make(_home, _teamSelect, _battle, () => null, _clock);

            var error = Assert.Throws<InvalidOperationException>(() => participant.Run());
            Assert.That(error.Message, Does.Contain("IGameSceneReady"));
        }

        private void ToTeamSelect()
        {
            _participant.Run();
            _ready.Raise();
            _home.PressPlay();
        }

        private void ToBattle()
        {
            ToTeamSelect();
            _teamSelect.PressEnter(Codes);
            _battle.RaiseBuilt();
        }

        internal sealed class FakeHome : IHomeScreen
        {
            public int Shows;
            public int Hides;

            public event Action PlayPressed;

            public bool HasListeners => PlayPressed != null;

            public void Show() => Shows++;

            public void Hide() => Hides++;

            public void PressPlay() => PlayPressed?.Invoke();
        }

        internal sealed class FakeTeamSelect : ITeamSelectScreen
        {
            public int Shows;
            public int Hides;
            public bool EnterEnabled = true;
            public string ErrorText;
            public IReadOnlyList<string> Remembered;

            public event Action<IReadOnlyList<string>> EnterBattlePressed;
            public event Action BackPressed;

            public bool HasListeners => EnterBattlePressed != null || BackPressed != null;

            public void Show() => Shows++;

            public void Hide() => Hides++;

            public void SetEnterBattleEnabled(bool enabled) => EnterEnabled = enabled;

            public void ShowBuildError(string text) => ErrorText = text;

            public void HideBuildError() => ErrorText = null;

            public void RememberTeam(IReadOnlyList<string> heroCodes) => Remembered = heroCodes;

            public void PressEnter(IReadOnlyList<string> codes) => EnterBattlePressed?.Invoke(codes);

            public void PressBack() => BackPressed?.Invoke();
        }

        internal sealed class FakeBattle : IRunBattle
        {
            public readonly List<RunTeam> Builds = new List<RunTeam>();
            public int Cancels;

            public event Action BattleBuilt;
            public event Action<string> BuildFailed;
            public event Action<RunLeaveChoice> BattleLeft;

            public bool HasListeners => BattleBuilt != null || BuildFailed != null || BattleLeft != null;

            public void BuildBattle(RunTeam team) => Builds.Add(team);

            public void CancelBuild() => Cancels++;

            public void RaiseBuilt() => BattleBuilt?.Invoke();

            public void RaiseFailed(string reason) => BuildFailed?.Invoke(reason);

            public void RaiseLeft(RunLeaveChoice choice) => BattleLeft?.Invoke(choice);
        }

        internal sealed class FakeSceneReady : IGameSceneReady
        {
            public bool IsReady { get; set; }

            public event Action Ready;

            public bool HasListeners => Ready != null;

            public void Raise()
            {
                IsReady = true;
                Ready?.Invoke();
            }
        }

        internal sealed class FakeClock : IFlowClock
        {
            private Action<float> _tick;
            private Action<bool> _active;
            public bool Stopped;

            public IDisposable Start(Action<float> tick, Action<bool> appActive)
            {
                _tick = tick;
                _active = appActive;
                return new Handle(this);
            }

            public void Tick(float seconds)
            {
                if (!Stopped) _tick?.Invoke(seconds);
            }

            public void SetActive(bool active)
            {
                if (!Stopped) _active?.Invoke(active);
            }

            private sealed class Handle : IDisposable
            {
                private readonly FakeClock _clock;

                public Handle(FakeClock clock) => _clock = clock;

                public void Dispose() => _clock.Stopped = true;
            }
        }

        private sealed class SilentLog : IFlowLog
        {
            public void Warning(string message) { }

            public void Error(string message) { }
        }
    }
}
