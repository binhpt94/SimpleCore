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
        private FakeScore _score;
        private FakePopups _popups;
        private FakeLog _log;
        private LosePopupParticipant _participant;

        [SetUp]
        public void SetUp()
        {
            _flow = new FakeFlow();
            _score = new FakeScore { Score = 12, Best = 15 };
            _popups = new FakePopups();
            _log = new FakeLog();
            _participant = new LosePopupParticipant(_flow, _score, _popups, _popups, _log);
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
            var cheatPopup = new LosePopupParticipant(_flow, FixedRunScore.For(LoseCheatState.NewRecord), _popups, _popups, _log);
            var cheat = new LoseCheatParticipant(cheatPopup);

            cheatPopup.Run();
            cheat.Run();

            var shown = _popups.View.Shown.Value;
            Assert.That(shown.Score, Is.EqualTo("Trụ được 16 đợt"));
            Assert.That(shown.Best, Is.EqualTo("Kỉ lục: 16 đợt"));
            Assert.That(shown.ShowNewRecord, Is.True);
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

            public void GoTo(RunStep step)
            {
                Step = step;
                StepChanged?.Invoke(step);
            }

            public void Play() { }
            public void EnterBattle(RunTeam team) { }
            public void Pause() { }
            public void Resume() { }
            public void Abandon() { }
            public void Replay() => ReplayCalls++;
            public void GoHome() { }
            public void Lose() { }
            public void CountClearedWave(int wave) { }
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
            public LoseResultText? Shown;
            public int ShowCount;
            public bool Locked;

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

            public void Close(PopupHandle handle) => _open.Remove(handle.Id);

            public bool TryGetView<T>(PopupHandle handle, out T view) where T : class
            {
                view = HasView && _open.Contains(handle.Id) ? View as T : null;
                return view != null;
            }

            public bool IsOnTop(PopupHandle handle) => OnTop && _open.Contains(handle.Id);
        }

        internal sealed class FakeLog : ILoseLog
        {
            public readonly List<string> Errors = new List<string>();
            public void Error(string message) => Errors.Add(message);
        }
    }
}
