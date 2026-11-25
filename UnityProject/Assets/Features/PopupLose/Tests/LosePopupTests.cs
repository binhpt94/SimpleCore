using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Game.Core;
using Game.Run;
using Game.Services.Popup;
using NUnit.Framework;

namespace Game.PopupLose.Tests
{
    public sealed class LosePopupTests
    {
        private FakeFlow _flow;
        private FakeRevive _revive;
        private FakeScore _score;
        private FakePopups _popups;
        private FakeLog _log;
        private LosePopupParticipant _participant;

        [SetUp]
        public void SetUp()
        {
            _flow = new FakeFlow();
            _revive = new FakeRevive();
            _score = new FakeScore { Score = 12, Best = 15 };
            _popups = new FakePopups();
            _log = new FakeLog();
            // From PROTOTYPE on: both buttons. IDEA (no home button) has its own tests below.
            _participant = new LosePopupParticipant(_flow, _revive, _score, _popups, _popups, _log, homeButton: true);
        }

        private LosePopupParticipant IdeaParticipant() =>
            new LosePopupParticipant(_flow, _revive, _score, _popups, _popups, _log, homeButton: false);

        [Test]
        public void AtIdeaTheHomeButtonIsHiddenAndOnlyReplayShows()
        {
            var participant = IdeaParticipant();
            participant.Run();
            _flow.GoTo(RunStep.Lost);

            Assert.That(_popups.View.HomeShown, Is.False);
        }

        [Test]
        public void AtIdeaAHomeTouchIsNeverReportedAndReplayStillWorks()
        {
            var participant = IdeaParticipant();
            participant.Run();
            _flow.GoTo(RunStep.Lost);

            _popups.View.RaiseHomeIgnoringLock();
            Assert.That(_flow.GoHomeCalls, Is.EqualTo(0));
            Assert.That(_popups.View.Locked, Is.False, "a stray home touch does not lock replay");

            _popups.View.PressReplay();
            Assert.That(_flow.ReplayCalls, Is.EqualTo(1));
        }

        [Test]
        public void OpenPopupCountsOneHeldViewAndClosingOrEndingTheSessionGoesBackToZero()
        {
            var unityView = UnityEngine.ScriptableObject.CreateInstance<UnityFakeView>();
            try
            {
                _popups.ViewOverride = unityView;
                var usage = (ISessionParticipantUsage)_participant;
                Assert.That(usage.OpensViewsOnRun, Is.False, "the popup opens only when the run is lost, not at Run");
                _participant.Run();
                Assert.That(usage.HeldViews, Is.EqualTo(0));
                Assert.That(usage.RunningJobs, Is.EqualTo(0), "the lose popup runs no timer or wait");

                _flow.GoTo(RunStep.Lost);
                Assert.That(usage.HeldViews, Is.EqualTo(1));

                _flow.GoTo(RunStep.Playing);
                Assert.That(usage.HeldViews, Is.EqualTo(0), "closed when the run leaves the lost step");

                _flow.GoTo(RunStep.Lost);
                Assert.That(usage.HeldViews, Is.EqualTo(1));
                _participant.DisposeAsync().GetAwaiter().GetResult();
                Assert.That(usage.HeldViews, Is.EqualTo(0), "cleaned up when the session ends");
                Assert.That(usage.RunningJobs, Is.EqualTo(0));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(unityView);
            }
        }

        [Test]
        public void FromPrototypeTheHomeButtonShows()
        {
            _participant.Run();
            _flow.GoTo(RunStep.Lost);

            Assert.That(_popups.View.HomeShown, Is.True);
        }

        [Test]
        public void PopupOpensWhenTheRunIsLost()
        {
            _participant.Run();
            Assert.That(_popups.Shown, Is.Empty);

            _flow.GoTo(RunStep.Lost);

            Assert.That(_popups.Shown, Is.EqualTo(new[] { new PopupId("PopupLose/Main") }));
        }

        [Test]
        public void PopupOpensAtRunWhenTheRunIsAlreadyLost()
        {
            _flow.Step = RunStep.Lost;
            _participant.Run();
            Assert.That(_popups.OpenCount, Is.EqualTo(1));
        }

        [Test]
        public void PopupShowsTitleScoreAndBestWithoutNewRecordLine()
        {
            _participant.Run();
            _flow.GoTo(RunStep.Lost);

            var shown = _popups.View.Shown.Value;
            Assert.That(shown.Title, Is.EqualTo("Thành đã bị phá"));
            Assert.That(shown.Score, Is.EqualTo("Trụ được 12 đợt"));
            Assert.That(shown.Best, Is.EqualTo("Kỉ lục: 15 đợt"));
            Assert.That(shown.ShowNewRecord, Is.False);
        }

        [Test]
        public void NewBestShowsTheNewRecordLine()
        {
            _score.Score = 16;
            _score.Best = 16;
            _score.IsNewBest = true;
            _participant.Run();
            _flow.GoTo(RunStep.Lost);

            Assert.That(_popups.View.Shown.Value.ShowNewRecord, Is.True);
            Assert.That(_popups.View.Shown.Value.Best, Is.EqualTo("Kỉ lục: 16 đợt"));
        }

        [Test]
        public void ScoreEqualToTheOldBestShowsNoNewRecordLine()
        {
            _score.Score = 15;
            _score.Best = 15;
            _score.IsNewBest = false;
            _participant.Run();
            _flow.GoTo(RunStep.Lost);

            var shown = _popups.View.Shown.Value;
            Assert.That(shown.Score, Is.EqualTo("Trụ được 15 đợt"));
            Assert.That(shown.Best, Is.EqualTo("Kỉ lục: 15 đợt"));
            Assert.That(shown.ShowNewRecord, Is.False);
        }

        [Test]
        public void ZeroScoreReadsZeroWaves()
        {
            _score.Score = 0;
            _score.Best = 0;
            _participant.Run();
            _flow.GoTo(RunStep.Lost);

            Assert.That(_popups.View.Shown.Value.Score, Is.EqualTo("Trụ được 0 đợt"));
            Assert.That(_popups.View.Shown.Value.Best, Is.EqualTo("Kỉ lục: 0 đợt"));
            Assert.That(_popups.View.Shown.Value.ShowNewRecord, Is.False);
        }

        [Test]
        public void ScoreIsReadOnceWhenThePopupOpens()
        {
            _participant.Run();
            _flow.GoTo(RunStep.Lost);
            _score.Score = 99;
            _score.RaiseChanged();

            Assert.That(_popups.View.ShowCount, Is.EqualTo(1));
            Assert.That(_popups.View.Shown.Value.Score, Is.EqualTo("Trụ được 12 đợt"));
        }

        [Test]
        public void ReplayTouchAsksTheRunToReplayAndLocksButtons()
        {
            _participant.Run();
            _flow.GoTo(RunStep.Lost);

            _popups.View.PressReplay();

            Assert.That(_flow.ReplayCalls, Is.EqualTo(1));
            Assert.That(_popups.View.Locked, Is.True);
        }

        [Test]
        public void OnlyTheFirstReplayTouchIsReported()
        {
            _participant.Run();
            _flow.GoTo(RunStep.Lost);

            _popups.View.PressReplay();
            _popups.View.PressReplay();

            Assert.That(_flow.ReplayCalls, Is.EqualTo(1));
        }

        [Test]
        public void SecondTouchReachingThePopupBeforeTheLockIsNotReported()
        {
            _participant.Run();
            _flow.GoTo(RunStep.Lost);

            _popups.View.RaiseReplayIgnoringLock();
            _popups.View.RaiseReplayIgnoringLock();

            Assert.That(_flow.ReplayCalls, Is.EqualTo(1));
        }

        [Test]
        public void TouchesInTheSameFrameCountOnlyTheFirst()
        {
            _participant.Run();
            _flow.GoTo(RunStep.Lost);

            for (var i = 0; i < 3; i++) _popups.View.RaiseReplayIgnoringLock();

            Assert.That(_flow.ReplayCalls, Is.EqualTo(1));
            Assert.That(_popups.View.Locked, Is.True);
        }

        [Test]
        public void ReplayTouchIsIgnoredWhileAnotherPopupIsOnTop()
        {
            _participant.Run();
            _flow.GoTo(RunStep.Lost);
            _popups.OnTop = false;

            _popups.View.PressReplay();

            Assert.That(_flow.ReplayCalls, Is.EqualTo(0));
            Assert.That(_popups.View.Locked, Is.False);
        }

        [Test]
        public void HomeTouchAsksTheRunToGoHomeAndLocksButtons()
        {
            _participant.Run();
            _flow.GoTo(RunStep.Lost);

            _popups.View.PressHome();

            Assert.That(_flow.GoHomeCalls, Is.EqualTo(1));
            Assert.That(_flow.ReplayCalls, Is.EqualTo(0));
            Assert.That(_popups.View.Locked, Is.True);
        }

        [Test]
        public void TouchOnTheOtherButtonAfterTheFirstIsNotReported()
        {
            _participant.Run();
            _flow.GoTo(RunStep.Lost);

            _popups.View.RaiseHomeIgnoringLock();
            _popups.View.RaiseReplayIgnoringLock();
            _popups.View.RaiseHomeIgnoringLock();

            Assert.That(_flow.GoHomeCalls, Is.EqualTo(1));
            Assert.That(_flow.ReplayCalls, Is.EqualTo(0));
        }

        [Test]
        public void HomeTouchIsIgnoredWhileAnotherPopupIsOnTop()
        {
            _participant.Run();
            _flow.GoTo(RunStep.Lost);
            _popups.OnTop = false;

            _popups.View.PressHome();

            Assert.That(_flow.GoHomeCalls, Is.EqualTo(0));
            Assert.That(_popups.View.Locked, Is.False);
        }

        [Test]
        public void HomeTouchDoesNotCloseThePopupUntilTheRunLeavesTheLostStep()
        {
            _participant.Run();
            _flow.GoTo(RunStep.Lost);
            _popups.View.PressHome();
            Assert.That(_popups.OpenCount, Is.EqualTo(1));

            _flow.GoTo(RunStep.Home);

            Assert.That(_popups.OpenCount, Is.EqualTo(0));
        }

        [Test]
        public void ComingBackToTheGameKeepsTheLosePopupAsItWasAndNeverPauses()
        {
            _participant.Run();
            _flow.GoTo(RunStep.Lost);
            var view = _popups.View;

            _flow.GoTo(RunStep.Lost);   // the run reports its step again after the app returns to the front

            Assert.That(_popups.Shown, Has.Count.EqualTo(1), "no second popup");
            Assert.That(_popups.OpenCount, Is.EqualTo(1));
            Assert.That(view.ShowCount, Is.EqualTo(1), "lines unchanged");
            Assert.That(_flow.PauseCalls, Is.EqualTo(0), "no pause popup");
            view.PressReplay();
            Assert.That(_flow.ReplayCalls, Is.EqualTo(1), "buttons still take the first touch");
        }

        [Test]
        public void PopupShowsTheBestTheRunHoldsWithoutReportingAnyError()
        {
            // Saving the best to the device failed: the run still holds the new best while the game is open.
            _score.Score = 20;
            _score.Best = 20;
            _score.IsNewBest = true;
            _participant.Run();
            _flow.GoTo(RunStep.Lost);

            Assert.That(_popups.View.Shown.Value.Best, Is.EqualTo("Kỉ lục: 20 đợt"));
            Assert.That(_popups.View.Shown.Value.ShowNewRecord, Is.True);
            Assert.That(_log.Errors, Is.Empty);
        }

        [Test]
        public void NewRecordLinePulsesFromOneTo115AndBackEverySecond()
        {
            Assert.That(NewRecordPulse.ScaleAt(0f), Is.EqualTo(1f).Within(1e-4));
            Assert.That(NewRecordPulse.ScaleAt(0.5f), Is.EqualTo(1.15f).Within(1e-4));
            Assert.That(NewRecordPulse.ScaleAt(1f), Is.EqualTo(1f).Within(1e-4));
            Assert.That(NewRecordPulse.ScaleAt(3.5f), Is.EqualTo(1.15f).Within(1e-4), "keeps looping");
            Assert.That(NewRecordPulse.ScaleAt(0.25f), Is.GreaterThan(1f).And.LessThan(1.15f));
            for (var t = 0f; t < 2f; t += 0.05f)
                Assert.That(NewRecordPulse.ScaleAt(t), Is.InRange(1f - 1e-4f, 1.15f + 1e-4f));
        }

        [Test]
        public void FailedReviveAdShowsTheAdNotCompletedLine()
        {
            _revive.LostAfterFailedRevive = true;
            _participant.Run();
            _flow.GoTo(RunStep.Lost);

            var shown = _popups.View.Shown.Value;
            Assert.That(shown.ShowAdFailed, Is.True);
            Assert.That(shown.ShowNewRecord, Is.False);
            Assert.That(LoseResultText.AdFailedText, Is.EqualTo("Quảng cáo chưa hoàn tất"));
        }

        [Test]
        public void FailedReviveAdWithANewBestShowsOnlyTheNewRecordLine()
        {
            _revive.LostAfterFailedRevive = true;
            _score.Score = 16;
            _score.Best = 16;
            _score.IsNewBest = true;
            _participant.Run();
            _flow.GoTo(RunStep.Lost);

            Assert.That(_popups.View.Shown.Value.ShowNewRecord, Is.True);
            Assert.That(_popups.View.Shown.Value.ShowAdFailed, Is.False);
        }

        [Test]
        public void DeclinedOrNoReviveShowsNoAdLine()
        {
            _revive.LostAfterFailedRevive = false;
            _participant.Run();
            _flow.GoTo(RunStep.Lost);

            Assert.That(_popups.View.Shown.Value.ShowAdFailed, Is.False);
        }

        [Test]
        public void ANewLossAfterAFailedAdReadsTheFlagAgain()
        {
            _revive.LostAfterFailedRevive = true;
            _participant.Run();
            _flow.GoTo(RunStep.Lost);
            _flow.GoTo(RunStep.Playing);

            _revive.LostAfterFailedRevive = false;   // the run resets it when a new run starts
            _flow.GoTo(RunStep.Lost);

            Assert.That(_popups.View.Shown.Value.ShowAdFailed, Is.False);
        }

        [Test]
        public void PopupDoesNotCloseByItself()
        {
            _participant.Run();
            _flow.GoTo(RunStep.Lost);
            _popups.View.PressReplay();

            Assert.That(_popups.OpenCount, Is.EqualTo(1));
        }

        [Test]
        public void PopupClosesWhenTheRunLeavesTheLostStep()
        {
            _participant.Run();
            _flow.GoTo(RunStep.Lost);

            _flow.GoTo(RunStep.Playing);

            Assert.That(_popups.OpenCount, Is.EqualTo(0));
        }

        [Test]
        public void ANewLossOpensAFreshPopupThatTakesATouchAgain()
        {
            _participant.Run();
            _flow.GoTo(RunStep.Lost);
            _popups.View.PressReplay();
            _flow.GoTo(RunStep.Playing);

            _flow.GoTo(RunStep.Lost);
            _popups.View.PressReplay();

            Assert.That(_flow.ReplayCalls, Is.EqualTo(2));
        }

        [Test]
        public void PopupClosesWhenTheSessionEnds()
        {
            _participant.Run();
            _flow.GoTo(RunStep.Lost);

            _participant.DisposeAsync().GetAwaiter().GetResult();

            Assert.That(_popups.OpenCount, Is.EqualTo(0));
            _flow.GoTo(RunStep.Lost);
            Assert.That(_popups.OpenCount, Is.EqualTo(0), "no longer listens to the run");
        }

        [Test]
        public void RefusedPopupLeavesNothingOpen()
        {
            _popups.Refuse = true;
            _participant.Run();
            _flow.GoTo(RunStep.Lost);

            Assert.That(_participant.IsOpen, Is.False);
        }

        [Test]
        public void PrefabWithoutLoseViewIsReportedAndClosed()
        {
            _popups.HasView = false;
            _participant.Run();
            _flow.GoTo(RunStep.Lost);

            Assert.That(_popups.OpenCount, Is.EqualTo(0));
            Assert.That(_log.Errors, Has.Count.EqualTo(1));
            Assert.That(_log.Errors[0], Does.Contain("PopupLose/Main"));
        }

        [Test]
        public void NewRecordCheatOpensThePopupWithForcedValuesAtOnce()
        {
            var cheatPopup = new LosePopupParticipant(_flow, _revive, FixedRunScore.For(LoseCheatState.NewRecord), _popups, _popups, _log, homeButton: true);
            var cheat = new LoseCheatParticipant(cheatPopup);

            cheatPopup.Run();
            cheat.Run();

            var shown = _popups.View.Shown.Value;
            Assert.That(shown.Score, Is.EqualTo("Trụ được 16 đợt"));
            Assert.That(shown.Best, Is.EqualTo("Kỉ lục: 16 đợt"));
            Assert.That(shown.ShowNewRecord, Is.True);
            Assert.That(_popups.View.HomeShown, Is.True, "cheat from PROTOTYPE: every part of the popup, home button included");
        }

        // ---------------------------------------------------------------- fakes

        internal sealed class FakeFlow : IRunFlow
        {
            public RunStep Step { get; set; } = RunStep.Home;
            public event Action<RunStep> StepChanged;
            public RunTeam Team => default;
            public event Action<RunTeam> RunStarted { add { } remove { } }
            public event Action BattleStarted { add { } remove { } }
            public event Action BattleStopped { add { } remove { } }
            public event Action BattleCleared { add { } remove { } }
            public bool IsPaused => Step == RunStep.Paused;
            public int ReplayCalls;
            public int GoHomeCalls;
            public int PauseCalls;

            public void GoTo(RunStep step)
            {
                Step = step;
                StepChanged?.Invoke(step);
            }

            public void Play() { }
            public void EnterBattle(RunTeam team) { }
            public void Pause() => PauseCalls++;
            public void Resume() { }
            public void Abandon() { }
            public void Replay() => ReplayCalls++;
            public void GoHome() => GoHomeCalls++;
            public void Lose() { }
            public void CountClearedWave(int wave) { }
        }

        internal sealed class FakeRevive : IRunRevive
        {
            public event Action ReviveStarted { add { } remove { } }
            public bool LostAfterFailedRevive { get; set; }
            public void Revive() { }
            public void DeclineRevive() { }
            public void FailRevive() { }
            public void ReviveShown() { }
        }

        internal sealed class FakeScore : IRunScore
        {
            public int Score { get; set; }
            public int Best { get; set; }
            public bool IsNewBest { get; set; }
            public event Action ScoreChanged;
            public void RaiseChanged() => ScoreChanged?.Invoke();
        }

        internal sealed class FakeView : ILoseView
        {
            public event Action ReplayPressed;
            public event Action HomePressed;
            public LoseResultText? Shown;
            public int ShowCount;
            public bool Locked;
            public bool? HomeShown;

            public void ShowHomeButton(bool shown) => HomeShown = shown;

            public void ShowResult(LoseResultText text)
            {
                Shown = text;
                ShowCount++;
            }

            public void LockButtons() => Locked = true;
            public void PressReplay()
            {
                if (!Locked) ReplayPressed?.Invoke();
            }

            /// <summary>Fires the touch even when locked, like two touches landing in the same frame.</summary>
            public void RaiseReplayIgnoringLock() => ReplayPressed?.Invoke();

            public void PressHome()
            {
                if (!Locked) HomePressed?.Invoke();
            }

            public void RaiseHomeIgnoringLock() => HomePressed?.Invoke();
        }

        internal sealed class FakePopups : IPopupService, IPopupViews
        {
            public readonly List<PopupId> Shown = new List<PopupId>();
            private readonly HashSet<int> _open = new HashSet<int>();
            private int _next;
            public bool Refuse;
            public bool HasView = true;
            public bool OnTop = true;
            public FakeView View { get; private set; }
            public int OpenCount => _open.Count;

            public PopupHandle Show(PopupId id)
            {
                if (Refuse) return default;
                Shown.Add(id);
                var handle = new PopupHandle(++_next);
                _open.Add(handle.Id);
                View = new FakeView();
                return handle;
            }

            /// <summary>When set, handed out instead of <see cref="View"/> (a real Unity object, so it can be tracked).</summary>
            public ILoseView ViewOverride;

            public void Close(PopupHandle handle) => _open.Remove(handle.Id);

            public bool TryGetView<T>(PopupHandle handle, out T view) where T : class
            {
                var source = ViewOverride ?? View;
                view = HasView && _open.Contains(handle.Id) ? source as T : null;
                return view != null;
            }

            public bool IsOnTop(PopupHandle handle) => OnTop && _open.Contains(handle.Id);
        }

        /// <summary>A view that is a Unity object, like the real LoseView, so TrackView can count it.</summary>
        internal sealed class UnityFakeView : UnityEngine.ScriptableObject, ILoseView
        {
            public event Action ReplayPressed { add { } remove { } }
            public event Action HomePressed { add { } remove { } }
            public void ShowResult(LoseResultText text) { }
            public void ShowHomeButton(bool shown) { }
            public void LockButtons() { }
        }

        internal sealed class FakeLog : ILoseLog
        {
            public readonly List<string> Errors = new List<string>();
            public void Error(string message) => Errors.Add(message);
        }
    }
}
