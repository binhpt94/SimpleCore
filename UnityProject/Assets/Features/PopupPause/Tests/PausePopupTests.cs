using System;
using System.Collections.Generic;
using Game.PopupPause;
using Game.Run;
using Game.Services.Popup;
using NUnit.Framework;

namespace Game.PopupPause.Tests
{
    // ---------------------------------------------------------------- fakes of other features (written here, per rules)

    internal sealed class FakeRun : IRunFlow
    {
        public RunStep Step { get; private set; } = RunStep.Playing;
        public event Action<RunStep> StepChanged;
        public RunTeam Team => default;
#pragma warning disable CS0067
        public event Action<RunTeam> RunStarted;
        public event Action BattleStopped;
        public event Action BattleCleared;
#pragma warning restore CS0067
        public event Action BattleStarted;
        public bool IsPaused => Step == RunStep.Paused;
        public int ResumeCalls;
        public int AbandonCalls;
        public int PauseCalls;

        public void SetStep(RunStep step)
        {
            Step = step;
            StepChanged?.Invoke(step);
        }

        public void StartBattle()
        {
            SetStep(RunStep.Playing);
            BattleStarted?.Invoke();
        }

        public void Play() { }
        public void EnterBattle(RunTeam team) { }

        public void Pause()
        {
            PauseCalls++;
            if (Step == RunStep.Playing) SetStep(RunStep.Paused);
        }

        public void Resume()
        {
            ResumeCalls++;
            if (Step == RunStep.Paused) SetStep(RunStep.Playing);
        }

        public void Abandon()
        {
            AbandonCalls++;
            if (Step == RunStep.Paused) SetStep(RunStep.Home);
        }

        public void Replay() { }
        public void GoHome() { }
        public void Lose() { }
        public void CountClearedWave(int wave) { }
    }

    internal sealed class FakePopups : IPopupService, IPopupViews
    {
        public readonly FakePauseView View = new FakePauseView();
        public readonly List<PopupId> Shown = new List<PopupId>();
        public int CloseCalls;
        public bool OnTop = true;
        public bool HasView = true;
        private int _next;
        private PopupHandle _open;

        public bool IsOpen => _open.IsValid;

        public PopupHandle Show(PopupId id)
        {
            Shown.Add(id);
            if (_open.IsValid) return default;
            _open = new PopupHandle(++_next);
            return _open;
        }

        public void Close(PopupHandle handle)
        {
            CloseCalls++;
            if (handle.Equals(_open)) _open = default;
        }

        public bool TryGetView<T>(PopupHandle handle, out T view) where T : class
        {
            view = HasView && _open.IsValid && handle.Equals(_open) ? View as T : null;
            return view != null;
        }

        public bool IsOnTop(PopupHandle handle) => OnTop && _open.IsValid && handle.Equals(_open);
    }

    internal sealed class FakePauseView : IPausePopupView
    {
        public event Action ResumePressed;
        public event Action QuitPressed;
        public event Action ConfirmQuitPressed;
        public event Action CancelQuitPressed;
        public event Action BackPressed;
        public PausePopupStep? Shown;
        public bool Interactable;

        public void ShowStep(PausePopupStep step) => Shown = step;
        public void SetInteractable(bool interactable) => Interactable = interactable;

        public void TapResume() => ResumePressed?.Invoke();
        public void TapQuit() => QuitPressed?.Invoke();
        public void TapConfirmQuit() => ConfirmQuitPressed?.Invoke();
        public void TapCancelQuit() => CancelQuitPressed?.Invoke();
        public void PressBack() => BackPressed?.Invoke();
    }

    internal sealed class FakeClock : IFrameClock
    {
        public int Frame { get; set; } = 1;
    }

    internal sealed class FakeLog : IPausePopupLog
    {
        public readonly List<string> Errors = new List<string>();
        public void Error(string message) => Errors.Add(message);
    }

    // ---------------------------------------------------------------- behaviour

    public class PausePopupTests
    {
        private FakeRun _run;
        private FakePopups _popups;
        private FakeLog _log;
        private FakeClock _clock;
        private PausePopupPresenter _presenter;

        private FakePauseView View => _popups.View;

        [SetUp]
        public void SetUp()
        {
            _run = new FakeRun();
            _popups = new FakePopups();
            _log = new FakeLog();
            _clock = new FakeClock();
            _presenter = new PausePopupPresenter(_run, _popups, _popups, _log, _clock);
            _presenter.Start();
        }

        private void NextFrame() => _clock.Frame++;

        // back-key: the press that paused the run must not resume it in the same frame
        [Test]
        public void BackKeyInTheFrameThePopupOpened_IsIgnored_TheNextFrameItContinues()
        {
            _run.Pause();               // the run paused on this frame's back key

            View.PressBack();           // the popup sees the same press
            Assert.That(_run.ResumeCalls, Is.EqualTo(0));
            Assert.That(_popups.IsOpen, Is.True);
            Assert.That(View.Shown, Is.EqualTo(PausePopupStep.Main));

            NextFrame();
            View.PressBack();
            Assert.That(_run.ResumeCalls, Is.EqualTo(1));
        }

        [Test]
        public void BackKeyInTheFrameThePopupOpened_IsIgnoredAgainAfterReopening()
        {
            _run.Pause();
            NextFrame();
            View.PressBack();           // continue
            NextFrame();

            _run.Pause();               // paused again by the back key
            View.PressBack();

            Assert.That(_run.ResumeCalls, Is.EqualTo(1));
            Assert.That(_popups.IsOpen, Is.True);
        }

        // step-main
        [Test]
        public void PausingTheBattle_OpensThePausePopupAtTheMainStep()
        {
            _run.Pause();

            Assert.That(_popups.Shown, Is.EqualTo(new[] { new PopupId("PopupPause/Main") }));
            Assert.That(_popups.IsOpen, Is.True);
            Assert.That(View.Shown, Is.EqualTo(PausePopupStep.Main));
            Assert.That(View.Interactable, Is.True);
        }

        [Test]
        public void NoPausePopup_WhileTheBattleRuns()
        {
            _run.SetStep(RunStep.Home);
            _run.SetStep(RunStep.Playing);
            _run.SetStep(RunStep.Lost);

            Assert.That(_popups.Shown, Is.Empty);
        }

        // resume-tap
        [Test]
        public void TappingContinue_ResumesTheRunAtOnceAndClosesThePopup()
        {
            _run.Pause();

            View.TapResume();

            Assert.That(_run.ResumeCalls, Is.EqualTo(1));
            Assert.That(_run.Step, Is.EqualTo(RunStep.Playing));
            Assert.That(_popups.IsOpen, Is.False);
        }

        // quit-to-confirm
        [Test]
        public void TappingQuitAtTheMainStep_SwitchesToTheConfirmStepInTheSamePopup()
        {
            _run.Pause();

            View.TapQuit();

            Assert.That(View.Shown, Is.EqualTo(PausePopupStep.ConfirmQuit));
            Assert.That(_popups.Shown.Count, Is.EqualTo(1));
            Assert.That(_popups.IsOpen, Is.True);
            Assert.That(_run.AbandonCalls, Is.EqualTo(0));
            Assert.That(_run.Step, Is.EqualTo(RunStep.Paused));
        }

        // confirm-yes
        [Test]
        public void ConfirmingQuit_AbandonsTheRunAndClosesThePopup()
        {
            _run.Pause();
            View.TapQuit();

            View.TapConfirmQuit();

            Assert.That(_run.AbandonCalls, Is.EqualTo(1));
            Assert.That(_run.ResumeCalls, Is.EqualTo(0));
            Assert.That(_run.Step, Is.EqualTo(RunStep.Home));
            Assert.That(_popups.IsOpen, Is.False);
        }

        // confirm-no
        [Test]
        public void TappingNoAtTheConfirmStep_GoesBackToTheMainStepAndStaysPaused()
        {
            _run.Pause();
            View.TapQuit();

            View.TapCancelQuit();

            Assert.That(View.Shown, Is.EqualTo(PausePopupStep.Main));
            Assert.That(_run.Step, Is.EqualTo(RunStep.Paused));
            Assert.That(_run.ResumeCalls + _run.AbandonCalls, Is.EqualTo(0));
            Assert.That(_popups.IsOpen, Is.True);
        }

        // back-key
        [Test]
        public void BackKeyAtTheMainStep_ActsLikeContinue()
        {
            _run.Pause();
            NextFrame();

            View.PressBack();

            Assert.That(_run.ResumeCalls, Is.EqualTo(1));
            Assert.That(_popups.IsOpen, Is.False);
        }

        [Test]
        public void BackKeyAtTheConfirmStep_ActsLikeNo()
        {
            _run.Pause();
            NextFrame();
            View.TapQuit();

            View.PressBack();

            Assert.That(View.Shown, Is.EqualTo(PausePopupStep.Main));
            Assert.That(_run.ResumeCalls + _run.AbandonCalls, Is.EqualTo(0));
        }

        [Test]
        public void BackKey_IsIgnoredWhenThePausePopupIsNotOnTop()
        {
            _run.Pause();
            NextFrame();
            _popups.OnTop = false;

            View.PressBack();

            Assert.That(_run.ResumeCalls, Is.EqualTo(0));
            Assert.That(_popups.IsOpen, Is.True);
        }

        // no-backdrop-close: the popup only goes away when the run leaves Paused, never on its own
        [Test]
        public void ThePopupNeverClosesItself_OnlyWhenTheRunLeavesPaused()
        {
            _run.Pause();
            View.TapQuit();
            View.TapCancelQuit();

            Assert.That(_popups.CloseCalls, Is.EqualTo(0));
            Assert.That(View.Shown, Is.EqualTo(PausePopupStep.Main));

            _run.SetStep(RunStep.Playing);

            Assert.That(_popups.CloseCalls, Is.EqualTo(1));
        }

        // one-report-per-choice
        [Test]
        public void AfterAChoiceIsReported_NoButtonTakesATapAnyMore()
        {
            var run = new StillPausedRun();
            var presenter = new PausePopupPresenter(run, _popups, _popups, _log, _clock);
            presenter.Start();
            run.SetStep(RunStep.Paused);

            View.TapResume();
            View.TapResume();
            View.TapQuit();
            NextFrame();
            View.PressBack();

            Assert.That(run.ResumeCalls, Is.EqualTo(1));
            Assert.That(run.AbandonCalls, Is.EqualTo(0));
            Assert.That(View.Shown, Is.EqualTo(PausePopupStep.Main));
            Assert.That(View.Interactable, Is.False);
        }

        [Test]
        public void AfterQuitIsConfirmed_NoOtherTapIsReported()
        {
            var run = new StillPausedRun();
            var presenter = new PausePopupPresenter(run, _popups, _popups, _log, _clock);
            presenter.Start();
            run.SetStep(RunStep.Paused);
            View.TapQuit();

            View.TapConfirmQuit();
            View.TapConfirmQuit();
            View.TapCancelQuit();
            NextFrame();
            View.PressBack();

            Assert.That(run.AbandonCalls, Is.EqualTo(1));
            Assert.That(run.ResumeCalls, Is.EqualTo(0));
            Assert.That(View.Interactable, Is.False);
        }

        // edge: leaving the app while the popup is open
        [Test]
        public void LeavingTheAppWhilePaused_KeepsTheOnePopupAtItsStep()
        {
            _run.Pause();
            View.TapQuit();

            _run.Pause();                       // the run ignores it: already paused
            _run.SetStep(RunStep.Paused);       // even if the step were raised again

            Assert.That(_popups.Shown.Count, Is.EqualTo(1));
            Assert.That(View.Shown, Is.EqualTo(PausePopupStep.ConfirmQuit));
            Assert.That(_presenter.Step, Is.EqualTo(PausePopupStep.ConfirmQuit));
        }

        // edge: reopen after continue
        [Test]
        public void ReopeningAfterContinue_AlwaysStartsAtTheMainStep()
        {
            _run.Pause();
            View.TapQuit();
            View.TapCancelQuit();
            View.TapResume();
            View.Shown = null;

            _run.Pause();

            Assert.That(_popups.IsOpen, Is.True);
            Assert.That(View.Shown, Is.EqualTo(PausePopupStep.Main));
            Assert.That(View.Interactable, Is.True);
        }

        [Test]
        public void ReopeningAfterTheRunLeftPausedAtTheConfirmStep_StartsAtTheMainStep()
        {
            _run.Pause();
            View.TapQuit();
            _run.SetStep(RunStep.Playing);      // run resumed by another way
            View.Shown = null;

            _run.Pause();

            Assert.That(View.Shown, Is.EqualTo(PausePopupStep.Main));
            View.TapQuit();
            Assert.That(View.Shown, Is.EqualTo(PausePopupStep.ConfirmQuit));
        }

        // edge: Continue and Quit almost together
        [Test]
        public void QuitThenContinueAlmostTogether_OnlyQuitCountsAndGoesToConfirm()
        {
            _run.Pause();

            View.TapQuit();
            View.TapResume();

            Assert.That(View.Shown, Is.EqualTo(PausePopupStep.ConfirmQuit));
            Assert.That(_run.ResumeCalls, Is.EqualTo(0));
            Assert.That(_run.Step, Is.EqualTo(RunStep.Paused));
        }

        [Test]
        public void ContinueThenQuitAlmostTogether_OnlyContinueCounts()
        {
            var run = new StillPausedRun();
            var presenter = new PausePopupPresenter(run, _popups, _popups, _log, _clock);
            presenter.Start();
            run.SetStep(RunStep.Paused);

            View.TapResume();
            View.TapQuit();

            Assert.That(run.ResumeCalls, Is.EqualTo(1));
            Assert.That(View.Shown, Is.EqualTo(PausePopupStep.Main));
        }

        // edge: quit before any wave was cleared
        [Test]
        public void QuittingBeforeAnyWaveWasCleared_StillAsksForConfirmation()
        {
            _run.Pause();

            View.TapQuit();
            Assert.That(_run.AbandonCalls, Is.EqualTo(0));
            Assert.That(View.Shown, Is.EqualTo(PausePopupStep.ConfirmQuit));

            View.TapConfirmQuit();
            Assert.That(_run.AbandonCalls, Is.EqualTo(1));
        }

        // edge: back key pressed again and again
        [Test]
        public void BackKeyPressedRepeatedly_GoesConfirmThenMainThenContinue_OneStepPerPress()
        {
            _run.Pause();
            View.TapQuit();

            NextFrame();
            View.PressBack();
            Assert.That(View.Shown, Is.EqualTo(PausePopupStep.Main));
            Assert.That(_run.ResumeCalls, Is.EqualTo(0));

            NextFrame();
            View.PressBack();
            Assert.That(_run.ResumeCalls, Is.EqualTo(1));
            Assert.That(_popups.IsOpen, Is.False);

            NextFrame();
            View.PressBack();
            Assert.That(_run.ResumeCalls, Is.EqualTo(1));
            Assert.That(_run.AbandonCalls, Is.EqualTo(0));
        }

        // params: at most one pause popup, two steps, no countdown, one report per opening
        [Test]
        public void Params_OnePopup_TwoSteps_NoCountdown_OneReportPerOpening()
        {
            _run.Pause();
            _run.SetStep(RunStep.Paused);
            Assert.That(_popups.Shown.Count, Is.EqualTo(1), "at most one pause popup");

            View.TapQuit();
            Assert.That(View.Shown, Is.EqualTo(PausePopupStep.ConfirmQuit));
            View.TapCancelQuit();
            Assert.That(View.Shown, Is.EqualTo(PausePopupStep.Main));
            Assert.That(Enum.GetValues(typeof(PausePopupStep)).Length, Is.EqualTo(2), "two steps");

            View.TapResume();
            Assert.That(_run.Step, Is.EqualTo(RunStep.Playing), "no countdown: the run is back in the same call");
            Assert.That(_run.ResumeCalls + _run.AbandonCalls, Is.EqualTo(1), "one report");
        }

        // wiring
        [Test]
        public void StartingWhileAlreadyPaused_OpensThePopup()
        {
            var run = new FakeRun();
            run.SetStep(RunStep.Paused);
            var popups = new FakePopups();
            var presenter = new PausePopupPresenter(run, popups, popups, _log, _clock);

            presenter.Start();

            Assert.That(popups.IsOpen, Is.True);
            Assert.That(popups.View.Shown, Is.EqualTo(PausePopupStep.Main));
        }

        [Test]
        public void Stopping_ClosesThePopupAndStopsFollowingTheRun()
        {
            _run.Pause();

            _presenter.Stop();
            Assert.That(_popups.IsOpen, Is.False);

            _run.SetStep(RunStep.Playing);
            _run.Pause();
            Assert.That(_popups.Shown.Count, Is.EqualTo(1));
        }

        [Test]
        public void PrefabWithoutTheView_IsLoggedAndClosed()
        {
            _popups.HasView = false;

            _run.Pause();

            Assert.That(_log.Errors.Count, Is.EqualTo(1));
            StringAssert.Contains("PopupPause/Main", _log.Errors[0]);
            Assert.That(_popups.IsOpen, Is.False);
        }

        /// <summary>A run that takes a report but does not leave Paused at once, to see what the popup does meanwhile.</summary>
        private sealed class StillPausedRun : IRunFlow
        {
            public RunStep Step { get; private set; } = RunStep.Playing;
            public event Action<RunStep> StepChanged;
            public RunTeam Team => default;
#pragma warning disable CS0067
            public event Action<RunTeam> RunStarted;
            public event Action BattleStarted;
            public event Action BattleStopped;
            public event Action BattleCleared;
#pragma warning restore CS0067
            public bool IsPaused => Step == RunStep.Paused;
            public int ResumeCalls;
            public int AbandonCalls;

            public void SetStep(RunStep step)
            {
                Step = step;
                StepChanged?.Invoke(step);
            }

            public void Play() { }
            public void EnterBattle(RunTeam team) { }
            public void Pause() { }
            public void Resume() => ResumeCalls++;
            public void Abandon() => AbandonCalls++;
            public void Replay() { }
            public void GoHome() { }
            public void Lose() { }
            public void CountClearedWave(int wave) { }
        }
    }
}
