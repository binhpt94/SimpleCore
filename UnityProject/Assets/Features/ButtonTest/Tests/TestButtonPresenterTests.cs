using System;
using Cysharp.Threading.Tasks;
using Game.Core;
using Game.Run;
using NUnit.Framework;
using UnityEngine;

namespace Game.ButtonTest.Tests
{
    // button-spec#edge-hoi-sinh-thanh-roi-choi-tiep, #edge-thua-roi-choi-lai, và vòng đời trong phiên.
    public sealed class TestButtonPresenterTests
    {
        private FakeRun _run;
        private FakeView _view;

        [SetUp]
        public void SetUp()
        {
            _run = new FakeRun();
            _view = new FakeView();
        }

        [TearDown]
        public void TearDown()
        {
            _view.DestroyRoot();
        }

        private TestButtonPresenter StartInBattle()
        {
            _run.Step = RunStep.Playing;
            var presenter = new TestButtonPresenter(_run, _view);
            presenter.Start();
            return presenter;
        }

        [Test]
        public void BattleScreenOpens_ButtonShowsAndLabelReadsDaBam0()
        {
            StartInBattle();

            Assert.IsTrue(_view.Visible);
            Assert.AreEqual("Đã bấm: 0", _view.Text);
        }

        [Test]
        public void TapOnButton_LabelChangesAtOnce()
        {
            StartInBattle();

            _view.Release(true);

            Assert.AreEqual("Đã bấm: 1", _view.Text);
        }

        [Test]
        public void EdgeHoiSinhThanhRoiChoiTiep_ReviveKeepsCount()
        {
            var presenter = StartInBattle();
            _view.Release(true);
            _view.Release(true);

            _run.GoTo(RunStep.ReviveWait);
            _run.GoTo(RunStep.ReviveOffer);
            _run.GoTo(RunStep.Reviving);
            _run.GoTo(RunStep.Playing);

            Assert.AreEqual(2, presenter.Count);
            Assert.AreEqual("Đã bấm: 2", _view.Text);
            Assert.IsTrue(_view.Visible);
        }

        [Test]
        public void EdgeThuaRoiChoiLai_ReplayResetsCountToZero()
        {
            var presenter = StartInBattle();
            _view.Release(true);
            _view.Release(true);
            _view.Release(true);

            _run.GoTo(RunStep.Ending);
            _run.GoTo(RunStep.Lost);
            Assert.AreEqual(3, presenter.Count, "Popup thua che lên: N chưa đổi.");

            _run.StartRun();
            _run.GoTo(RunStep.Playing);

            Assert.AreEqual(0, presenter.Count);
            Assert.AreEqual("Đã bấm: 0", _view.Text);
            Assert.IsTrue(_view.Visible);
        }

        [Test]
        public void LeavingBattleScreen_HidesButtonAndDropsCount()
        {
            var presenter = StartInBattle();
            _view.Release(true);

            _run.GoTo(RunStep.Home);

            Assert.IsFalse(_view.Visible);
            Assert.AreEqual(0, presenter.Count);
        }

        [Test]
        public void HomeAndTeamSelect_ButtonHidden()
        {
            _run.Step = RunStep.Home;
            new TestButtonPresenter(_run, _view).Start();
            Assert.IsFalse(_view.Visible);

            _run.GoTo(RunStep.TeamSelect);
            Assert.IsFalse(_view.Visible);
        }

        [Test]
        public void SessionRun_CreatesTrackedView_AndSessionEnd_DestroysIt()
        {
            _run.Step = RunStep.Playing;
            var factory = new FakeFactory(_view);
            var participant = new ButtonTestParticipant(_run, factory);

            participant.Run();
            Assert.AreEqual(1, factory.Created);
            Assert.AreEqual(1, ((ISessionParticipantUsage)participant).HeldViews);
            Assert.AreEqual("Đã bấm: 0", _view.Text);

            participant.DisposeAsync().Forget();

            Assert.AreEqual(1, factory.Destroyed);
            Assert.AreEqual(0, ((ISessionParticipantUsage)participant).HeldViews);
            _view.Release(true);
            Assert.AreEqual("Đã bấm: 0", _view.Text, "Sau khi phiên kết thúc, view không còn nối với số đếm.");
        }

        [Test]
        public void CheatPressed12_ShowsButtonWithDaBam12_WithoutBattleRunning()
        {
            _run.Step = RunStep.Home;
            var participant = new ButtonTestParticipant(_run, new FakeFactory(_view), ButtonTestCheat.Pressed12);

            participant.Run();

            Assert.IsTrue(_view.Visible);
            Assert.AreEqual("Đã bấm: 12", _view.Text);
        }

        [Test]
        public void CheatPressed12_TapAfterwards_CountsOnFrom12()
        {
            _run.Step = RunStep.Home;
            var participant = new ButtonTestParticipant(_run, new FakeFactory(_view), ButtonTestCheat.Pressed12);
            participant.Run();

            _view.Release(true);
            _view.Release(true);

            Assert.AreEqual("Đã bấm: 14", _view.Text);
        }

        [Test]
        public void NoCheat_SessionStartsOnHome_ButtonHiddenAtDaBam0()
        {
            _run.Step = RunStep.Home;
            var participant = new ButtonTestParticipant(_run, new FakeFactory(_view));

            participant.Run();

            Assert.IsFalse(_view.Visible);
            Assert.AreEqual("Đã bấm: 0", _view.Text);
        }

        internal sealed class FakeView : ITestButtonView
        {
            private GameObject _root = new GameObject("FakeTestButtonView");

            public UnityEngine.Object Root => _root;
            public bool Visible { get; private set; }
            public string Text { get; private set; }
            public event Action<bool> Released;

            public void ApplyLayout(TestButtonLayout layout, string buttonLabel) { }
            public void SetVisible(bool visible) => Visible = visible;
            public void SetCounterText(string text) => Text = text;
            public void Release(bool overButton) => Released?.Invoke(overButton);

            public void DestroyRoot()
            {
                if (_root != null) UnityEngine.Object.DestroyImmediate(_root);
                _root = null;
            }
        }

        private sealed class FakeFactory : ITestButtonViewFactory
        {
            private readonly FakeView _view;
            public int Created { get; private set; }
            public int Destroyed { get; private set; }

            public FakeFactory(FakeView view) => _view = view;

            public ITestButtonView Create()
            {
                Created++;
                return _view;
            }

            public void Destroy(ITestButtonView view)
            {
                Destroyed++;
                _view.DestroyRoot();
            }
        }

        internal sealed class FakeRun : IRunFlow
        {
            public RunStep Step { get; set; }
            public RunTeam Team => default;
            public bool IsPaused => false;

            public event Action<RunStep> StepChanged;
            public event Action<RunTeam> RunStarted;
            public event Action BattleStarted { add { } remove { } }
            public event Action BattleStopped { add { } remove { } }
            public event Action BattleCleared { add { } remove { } }

            public void GoTo(RunStep step)
            {
                Step = step;
                StepChanged?.Invoke(step);
            }

            public void StartRun() => RunStarted?.Invoke(default);

            public void Play() { }
            public void EnterBattle(RunTeam team) { }
            public void Pause() { }
            public void Resume() { }
            public void Abandon() { }
            public void Replay() { }
            public void GoHome() { }
            public void Lose() { }
            public void CountClearedWave(int wave) { }
        }
    }
}
