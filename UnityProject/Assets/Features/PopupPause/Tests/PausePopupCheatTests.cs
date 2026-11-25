#if UNITY_EDITOR || DEVELOPMENT_BUILD
using Cysharp.Threading.Tasks;
using Game.PopupPause;
using Game.Run;
using NUnit.Framework;

namespace Game.PopupPause.Tests
{
    public class PausePopupCheatTests
    {
        private FakeRun _run;
        private FakePopups _popups;
        private PopupPauseParticipant _popup;

        private PausePopupCheat Cheat(PopupPauseCheatState state, PausePopupPresenter presenter) =>
            new PausePopupCheat(_run, presenter, state);

        private PausePopupPresenter Presenter()
        {
            _run = new FakeRun();
            _run.SetStep(RunStep.Home);
            _popups = new FakePopups();
            var presenter = new PausePopupPresenter(_run, _popups, _popups, new FakeLog(), new FakeClock());
            _popup = new PopupPauseParticipant(presenter);
            return presenter;
        }

        // cheat#paused-main
        [Test]
        public void CheatPausedMain_PausesTheBattleAsSoonAsItStarts_PopupAtTheMainStep()
        {
            var presenter = Presenter();
            var cheat = Cheat(PopupPauseCheatState.PausedMain, presenter);
            _popup.Run();
            cheat.Run();

            _run.StartBattle();

            Assert.That(_run.Step, Is.EqualTo(RunStep.Paused));
            Assert.That(_run.PauseCalls, Is.EqualTo(1), "goes through the run's own Pause");
            Assert.That(_popups.IsOpen, Is.True);
            Assert.That(_popups.View.Shown, Is.EqualTo(PausePopupStep.Main));
        }

        // cheat#paused-confirm
        [Test]
        public void CheatPausedConfirm_PausesTheBattle_PopupAtTheConfirmStep()
        {
            var presenter = Presenter();
            var cheat = Cheat(PopupPauseCheatState.PausedConfirm, presenter);
            _popup.Run();
            cheat.Run();

            _run.StartBattle();

            Assert.That(_run.Step, Is.EqualTo(RunStep.Paused));
            Assert.That(_popups.Shown.Count, Is.EqualTo(1), "same popup, no second one");
            Assert.That(_popups.View.Shown, Is.EqualTo(PausePopupStep.ConfirmQuit));
            Assert.That(_run.AbandonCalls, Is.EqualTo(0));
        }

        // cheat#params
        [Test]
        public void Cheat_ActsAtOnceWhenTheBattleIsAlreadyRunning()
        {
            var presenter = Presenter();
            _run.StartBattle();
            _popup.Run();

            Cheat(PopupPauseCheatState.PausedMain, presenter).Run();

            Assert.That(_run.Step, Is.EqualTo(RunStep.Paused));
            Assert.That(_popups.View.Shown, Is.EqualTo(PausePopupStep.Main));
        }

        [Test]
        public void Cheat_ActsOncePerSession_NotOnEveryLaterBattle()
        {
            var presenter = Presenter();
            var cheat = Cheat(PopupPauseCheatState.PausedMain, presenter);
            _popup.Run();
            cheat.Run();
            _run.StartBattle();
            _popups.View.TapResume();

            _run.StartBattle();

            Assert.That(_run.PauseCalls, Is.EqualTo(1));
            Assert.That(_run.Step, Is.EqualTo(RunStep.Playing));
        }

        [Test]
        public void CheatNone_DoesNothing()
        {
            var presenter = Presenter();
            _popup.Run();
            Cheat(PopupPauseCheatState.None, presenter).Run();

            _run.StartBattle();

            Assert.That(_run.PauseCalls, Is.EqualTo(0));
            Assert.That(_popups.IsOpen, Is.False);
        }

        [Test]
        public void EndingTheSession_StopsTheCheat()
        {
            var presenter = Presenter();
            var cheat = Cheat(PopupPauseCheatState.PausedMain, presenter);
            _popup.Run();
            cheat.Run();

            cheat.DisposeAsync().Forget();
            _run.StartBattle();

            Assert.That(_run.PauseCalls, Is.EqualTo(0));
        }
    }
}
#endif
