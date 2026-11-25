using Game.Run;
using NUnit.Framework;

namespace Game.Hud.Tests
{
    /// <summary>feature-hud/hud-pause-button/pause-button-spec: what a tap on the pause button reports to the run.</summary>
    public sealed class HudPauseButtonTests
    {
        private FakeView _view;
        private FakeRun _run;
        private FakeCastle _castle;
        private HudPausePresenter _pause;

        [SetUp]
        public void SetUp()
        {
            _view = new FakeView();
            _run = new FakeRun();
            _castle = new FakeCastle();
            _pause = new HudPausePresenter(_view, _run, _castle);
            _pause.Start();
        }

        [Test]
        public void TapDuringTheBattleReportsPauseToTheRunOnce()
        {
            _view.PressPause();

            Assert.AreEqual(1, _run.PauseRequests);
        }

        [Test]
        public void TapWhilePausedReportsNothing()
        {
            _run.SetStep(RunStep.Paused);

            _view.PressPause();

            Assert.AreEqual(0, _run.PauseRequests);
        }

        [Test]
        public void TapAfterTheRunEndedReportsNothing()
        {
            _run.SetStep(RunStep.Lost);
            _view.PressPause();

            Assert.AreEqual(0, _run.PauseRequests);
        }

        [Test]
        public void TwoQuickTapsReportPauseOnlyOnce()
        {
            _view.PressPause();
            _view.PressPause();

            Assert.AreEqual(1, _run.PauseRequests);
            Assert.AreEqual(RunStep.Paused, _run.Step);
        }

        [Test]
        public void TapInTheOneSecondWaitBeforeTheLosePopupReportsNothing()
        {
            _castle.Destroy();
            _run.SetStep(RunStep.Ending);

            _view.PressPause();

            Assert.AreEqual(0, _run.PauseRequests);
        }

        [Test]
        public void TapInTheFrameTheCastleFallsIsDropped()
        {
            // The castle is already destroyed but the run has not moved on to Ending yet.
            _castle.Destroy();
            Assert.AreEqual(RunStep.Playing, _run.Step);

            _view.PressPause();

            Assert.AreEqual(0, _run.PauseRequests);
        }

        [Test]
        public void TapAfterResumingReportsPauseAgain()
        {
            _view.PressPause();
            _run.SetStep(RunStep.Playing);
            _view.PressPause();

            Assert.AreEqual(2, _run.PauseRequests);
        }

        [Test]
        public void StoppedButtonIgnoresTaps()
        {
            _pause.Stop();
            _view.PressPause();

            Assert.AreEqual(0, _run.PauseRequests);
            Assert.AreEqual(0, _view.PauseListeners);
        }
    }
}
